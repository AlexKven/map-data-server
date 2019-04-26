using LinqToDB;
using MapDataServer.Helpers;
using MapDataServer.Models;
using OsmSharp.Streams;
using OsmSharp.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class MapDownloader : IMapDownloader
    {
        private IDatabase Database { get; }
        private IHttpClientFactory HttpClientFactory { get; }
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
            for (int i = 0; i < lengthLat; i+= 2)
            {
                for (int j = 0; j < lengthLon; j += 2)
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
                            var source = new XmlOsmStreamSource(stream);
                            var nodes = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Node).Cast<OsmSharp.Node>().ToDictionary(n => n.Id);
                            var ways = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Way).Cast<OsmSharp.Way>();
                            var relations = source.Where(geo => geo.Type == OsmSharp.OsmGeoType.Relation).Cast<OsmSharp.Relation>();
                            foreach (var node in nodes)
                            {
                                if (!(node.Value.Id.HasValue && node.Value.Latitude.HasValue && node.Value.Longitude.HasValue))
                                    continue;
                                var dbNode = new MapNode()
                                {
                                    Id = node.Value.Id.Value,
                                    GeneratedDate = node.Value.TimeStamp,
                                    SavedDate = DateTime.UtcNow,
                                    IsVisible = node.Value.Visible,
                                    Latitude = node.Value.Latitude.Value,
                                    Longitude = node.Value.Longitude.Value
                                };
                                await SaveTagsForGeo(node.Value);
                                await Database.InsertOrReplaceAsync(dbNode);
                            }
                            
                            foreach (var way in ways)
                            {
                                if (!way.Id.HasValue)
                                    continue;
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
                                    await Database.InsertOrReplaceAsync(new WayNodeLink() { NodeId = nodeId, WayId = way.Id.Value });
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
