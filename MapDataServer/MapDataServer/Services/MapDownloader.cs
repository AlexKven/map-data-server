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

        public class GeoTagComparer : Comparer<GeoTag>
        {
            public override int Compare(GeoTag x, GeoTag y)
            {
                if (x.GeoId < y.GeoId)
                    return -1;
                if (x.GeoId > y.GeoId)
                    return 1;
                return string.Compare(x.Key, y.Key);
            }
        }

        private IDatabase Database { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private TimeSpan MaxAge { get; } = TimeSpan.FromDays(30);
        public MapDownloader(IDatabase database, IHttpClientFactory httpClientFactory)
        {
            Database = database;
            HttpClientFactory = httpClientFactory;
        }

        private List<GeoTag> StagedTags { get; } = new List<GeoTag>();
        private List<MapNode> StagedNodes { get; } = new List<MapNode>();
        private List<MapWay> StagedWays { get; } = new List<MapWay>();
        private List<MapRelation> StagedRelations { get; } = new List<MapRelation>();
        private List<WayNodeLink> StagedWayNodeLinks { get; } = new List<WayNodeLink>();
        private List<MapRelationMember> StagedRelationMembers { get; } = new List<MapRelationMember>();

        private async Task CommitStaged()
        {
            await Database.BulkInsert(StagedTags, false);
            StagedTags.Clear();

            await Database.BulkInsert(StagedNodes, false);
            StagedNodes.Clear();

            await Database.BulkInsert(StagedWays, false);
            StagedWays.Clear();

            await Database.BulkInsert(StagedRelations, false);
            StagedRelations.Clear();

            await Database.BulkInsert(StagedWayNodeLinks, false);
            StagedWayNodeLinks.Clear();

            await Database.BulkInsert(StagedRelationMembers, false);
            StagedRelationMembers.Clear();
        }

        private void StageTagsForGeo(OsmSharp.OsmGeo geo)
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
                StagedTags.Add(dbTag);
            }
        }

        private IEnumerable<T> ConvertOsmSource<T>(IEnumerable<OsmSharp.OsmGeo> source) where T : OsmSharp.OsmGeo
        {
            return source.Where(geo => geo.GetType() == typeof(T)).Cast<T>()
                                .Where(geo => geo.Id.HasValue);
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

                    var dateThreshold = DateTime.UtcNow - MaxAge;
                    Expression<Func<GeoBase, bool>> dateFilter = geo => geo.SavedDate > dateThreshold;

                    Expression<Func<OsmSharp.OsmGeo, bool>> hasIdFilter = osm => osm.Id.HasValue;

                    using (result)
                    {
                        using (var stream = await result.Content.ReadAsStreamAsync())
                        {
                            
                            var source = new XmlOsmStreamSource(stream);

                            var dbNodes = await Database.MapNodes.Where(dateFilter).Select(mn => mn.Id).ToListAsync();
                            dbNodes.Sort();
                            var osmNodes = ConvertOsmSource<OsmSharp.Node>(source)
                                .Where(node => node.Latitude.HasValue && node.Longitude.HasValue)
                                .ToDictionary(node => node.Id);

                            var dbWays = await Database.MapWays.Where(dateFilter).Select(mw => mw.Id).ToListAsync();
                            dbWays.Sort();
                            var osmWays = ConvertOsmSource<OsmSharp.Way>(source)
                                .Where(geo => dbNodes.BinarySearch(geo.Id.Value) < 0);

                            var dbRelations = await Database.MapRelations.Where(dateFilter).Select(mr => mr.Id).ToListAsync();
                            dbRelations.Sort();
                            var osmRelations = ConvertOsmSource<OsmSharp.Relation>(source)
                                .Where(geo => dbRelations.BinarySearch(geo.Id.Value) < 0);

                            var dbWayNodeLinks = await Database.WayNodeLinks.ToListAsync();
                            dbWayNodeLinks.Sort(new WayNodeLinkComparer());

                            foreach (var node in osmNodes.Where(node => dbNodes.BinarySearch(node.Key.Value) < 0))
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
                                StageTagsForGeo(node.Value);
                                StagedNodes.Add(dbNode);
                            }

                            await CommitStaged();

                            foreach (var way in osmWays)
                            {
                                var dbWay = new MapWay()
                                {
                                    Id = way.Id.Value,
                                    GeneratedDate = way.TimeStamp,
                                    SavedDate = DateTime.UtcNow,
                                    IsVisible = way.Visible
                                };
                                StageTagsForGeo(way);
                                bool minMaxSet = false;
                                foreach (var nodeId in way.Nodes ?? new long[0])
                                {
                                    var node = osmNodes[nodeId];
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
                                    StagedWayNodeLinks.Add(dbWNL);
                                }
                                StagedWays.Add(dbWay);
                            }

                            await CommitStaged();

                            foreach (var relation in osmRelations)
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
                                StageTagsForGeo(relation);
                                foreach (var member in relation.Members)
                                {
                                    StagedRelationMembers.Add(new MapRelationMember()
                                    {
                                        RelationId = relation.Id.Value,
                                        GeoId = member.Id,
                                        GeoType = member.Type.GetGeoType(),
                                        Role = member.Role
                                    });
                                }
                                StagedRelations.Add(dbRelation);
                            }

                            await CommitStaged();
                        }
                    }
                }
            }
        }
    }
}
