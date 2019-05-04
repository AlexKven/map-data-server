using LinqToDB;
using LinqToDB.SqlQuery;
using MapDataServer.Helpers;
using MapDataServer.Models;
using OsmSharp.Streams;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class MapDownloader : IMapDownloader
    {
        private class WayNodeLinkComparer : Comparer<WayNodeLink>
        {
            public override int Compare(WayNodeLink x, WayNodeLink y) => x.WayId < y.WayId ? -1 : x.WayId > y.WayId ? 1 : 0;
        }

        private IDatabase Database { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private TimeSpan MaxAge { get; } = TimeSpan.FromDays(30);
        public MapDownloader(IDatabase database, IHttpClientFactory httpClientFactory)
        {
            Database = database;
            HttpClientFactory = httpClientFactory;
        }

        private async Task SaveTagsForGeo(OsmSharp.OsmGeo geo)
        {
            foreach (var tag in geo.Tags ?? Enumerable.Empty<Tag>())
            {
                var dbTag = new GeoTag()
                {
                    GeoId = geo.Id.Value,
                    Key = tag.Key,
                    Value = tag.Value,
                    GeoType = geo.Type.GetGeoType()
                };
                await Database.InsertOrReplaceAsync(dbTag);
            }
        }

        public async Task DownloadMapRegions(int minLon, int minLat, int lengthLon, int lengthLat)
        {
            await Database.Initializer;
            for (int i = 0; i < lengthLon; i++)
            {
                for (int j = 0; j < lengthLat; j++)
                {
                    await  Database.InsertOrReplaceAsync(new MapRegion(minLon + i, minLat + j));
                }
            }
            for (int i = 0; i < lengthLon; i+= 2)
            {
                for (int j = 0; j < lengthLat; j += 2)
                {
                    var height = Math.Min(2, lengthLat - j);
                    var width = Math.Min(2, lengthLon - i);
                    var lat0 = (minLat + j) * .01;
                    var lon0 = (minLon + i) * .01;
                    var lat1 = (minLat + j + height) * .01;
                    var lon1 = (minLon + i + width) * .01;

                    var url = $"https://api.openstreetmap.org/api/0.6/map?bbox={lon0},{lat0},{lon1},{lat1}";
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var httpClient = HttpClientFactory.CreateClient();
                    var result = await httpClient.SendAsync(request);

                    using (result)
                    {
                        using (var stream = await result.Content.ReadAsStreamAsync())
                        {
                            var dateThreshold = DateTime.UtcNow - MaxAge;
                            
                            var source = new XmlOsmStreamSource(stream);

                            var dbNodes = await Database.MapNodes.Where(mn => mn.SavedDate > dateThreshold).Select(mn => mn.Id).ToListAsync();
                            dbNodes.Sort();
                            var nodes = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Node).Cast<OsmSharp.Node>()
                                .Where(node => node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue)
                                .ToDictionary(node => node.Id);

                            var dbWays = await Database.MapWays.Where(mw => mw.SavedDate > dateThreshold).Select(mw => mw.Id).ToListAsync();
                            dbWays.Sort();
                            var ways = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Way).Cast<OsmSharp.Way>()
                                .Where(geo => geo.Id.HasValue)
                                .Where(geo => dbNodes.BinarySearch(geo.Id.Value) < 0);

                            var dbRelations = await Database.MapRelations.Where(mr => mr.SavedDate > dateThreshold).Select(mr => mr.Id).ToListAsync();
                            dbRelations.Sort();
                            var relations = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Relation).Cast<OsmSharp.Relation>()
                                .Where(geo => geo.Id.HasValue)
                                .Where(geo => dbRelations.BinarySearch(geo.Id.Value) < 0);

                            var dbWayNodeLinks = await Database.WayNodeLinks.ToListAsync();
                            dbWayNodeLinks.Sort(new WayNodeLinkComparer());

                            List<MapNode> dbNodesInsert = new List<MapNode>();
                            foreach (var node in nodes.Where(node => dbNodes.BinarySearch(node.Key.Value) < 0))
                            {
                                var dbNode = new MapNode()
                                {
                                    Id = node.Value.Id.Value,
                                    GeneratedDate = node.Value.TimeStamp,
                                    SavedDate = DateTime.UtcNow,
                                    IsVisible = node.Value.Visible,
                                    Latitude = node.Value.Latitude.Value,
                                    Longitude = node.Value.Longitude.Value
                                };
                                //await SaveTagsForGeo(node.Value);
                                dbNodesInsert.Add(dbNode);
                            }
                            await Database.BulkInsert(dbNodesInsert);

                            foreach (var way in ways)
                            {
                                var dbWay = new MapWay()
                                {
                                    Id = way.Id.Value,
                                    GeneratedDate = way.TimeStamp,
                                    SavedDate = DateTime.UtcNow,
                                    IsVisible = way.Visible
                                };
                                await SaveTagsForGeo(way);
                                bool minMaxSet = false;
                                foreach (var nodeId in way.Nodes ?? new long[0])
                                {
                                    var node = nodes[nodeId];
                                    if (!minMaxSet)
                                    {
                                        dbWay.MinLat = node.Latitude;
                                        dbWay.MaxLat = node.Latitude;
                                        dbWay.MinLon = node.Longitude;
                                        dbWay.MaxLon = node.Longitude;
                                        minMaxSet = true;
                                    }
                                    else
                                    {
                                        if (dbWay.MinLat > node.Latitude)
                                            dbWay.MinLat = node.Latitude;
                                        if (dbWay.MaxLat < node.Latitude)
                                            dbWay.MaxLat = node.Latitude;
                                        if (dbWay.MinLon > node.Longitude)
                                            dbWay.MinLon = node.Longitude;
                                        if (dbWay.MaxLon < node.Longitude)
                                            dbWay.MaxLon = node.Longitude;
                                    }
                                    var dbWNL = new WayNodeLink() { NodeId = nodeId, WayId = way.Id.Value };
                                    if (dbWayNodeLinks.BinarySearch(dbWNL, new WayNodeLinkComparer()) < 0 &&
                                        await Database.WayNodeLinks.CountAsync(v => v.NodeId == dbWNL.NodeId && v.WayId == dbWNL.WayId) == 0)
                                    {
                                        await Database.InsertAsync(dbWNL);
                                    }
                                }
                                await Database.InsertOrReplaceAsync(dbWay);
                            }

                            foreach (var relation in relations)
                            {
                                if (!relation.Id.HasValue)
                                    continue;
                                var dbRelation = new MapRelation()
                                {
                                    Id = relation.Id.Value,
                                    GeneratedDate = relation.TimeStamp,
                                    SavedDate = DateTime.UtcNow,
                                    IsVisible = relation.Visible
                                };
                                await SaveTagsForGeo(relation);
                                foreach (var member in relation.Members)
                                {
                                    await Database.InsertOrReplaceAsync(new MapRelationMember()
                                    {
                                        RelationId = relation.Id.Value,
                                        GeoId = member.Id,
                                        GeoType = member.Type.GetGeoType(),
                                        Role = member.Role
                                    });
                                }
                                await Database.InsertOrReplaceAsync(dbRelation);
                            }
                        }
                    }
                }
            }
        }
    }
}
