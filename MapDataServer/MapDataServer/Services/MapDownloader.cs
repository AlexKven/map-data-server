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
        private List<MapHighway> StagedHighways { get; } = new List<MapHighway>();
        private List<MapRelation> StagedRelations { get; } = new List<MapRelation>();
        private List<WayNodeLink> StagedWayNodeLinks { get; } = new List<WayNodeLink>();
        private List<MapRelationMember> StagedRelationMembers { get; } = new List<MapRelationMember>();

        private bool TryConvertSpeedToKmh(string speed, out float kmh)
        {
            if (float.TryParse(speed.Trim(), out kmh))
                return true;
            if (speed.Contains("km/h"))
            {
                speed = speed.Replace("km/h", "");
                if (float.TryParse(speed.Trim(), out kmh))
                    return true;
            }
            else if (speed.Contains("kph"))
            {
                speed = speed.Replace("kph", "");
                if (float.TryParse(speed.Trim(), out kmh))
                    return true;
            }
            else if (speed.Contains("kmph"))
            {
                speed = speed.Replace("kmph", "");
                if (float.TryParse(speed.Trim(), out kmh))
                    return true;
            }
            else if (speed.Contains("mph"))
            {
                speed = speed.Replace("mph", "");
                if (float.TryParse(speed.Trim(), out kmh))
                {
                    kmh *= 1.6093f;
                    return true;
                }
            }
            else if (speed.Contains("knots"))
            {
                speed = speed.Replace("knots", "");
                if (float.TryParse(speed.Trim(), out kmh))
                {
                    kmh *= 0.869f;
                    return true;
                }
            }
            return false;
        }

        private async Task CommitStaged()
        {
            await Database.BulkInsert(StagedTags, false);
            StagedTags.Clear();

            await Database.BulkInsert(StagedNodes, false);
            StagedNodes.Clear();

            await Database.BulkInsert(StagedWays, false);
            StagedWays.Clear();

            await Database.BulkInsert(StagedHighways, false);
            StagedHighways.Clear();

            await Database.BulkInsert(StagedRelations, false);
            StagedRelations.Clear();

            await Database.BulkInsert(StagedWayNodeLinks, false);
            StagedWayNodeLinks.Clear();

            await Database.BulkInsert(StagedRelationMembers, false);
            StagedRelationMembers.Clear();
        }

        private void StageTagsForGeo(OsmSharp.OsmGeo geo, bool highway = false)
        {
            foreach (var tag in geo.Tags ?? Enumerable.Empty<Tag>())
            {
                var dbTag = new GeoTag()
                {
                    GeoId = geo.Id.Value,
                    Key = tag.Key,
                    Value = tag.Value,
                    GeoType = geo.Type.GetGeoType(highway)
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
            await Database.Initialize();
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
                                MapWay dbWay;
                                bool highway = way.Tags?.ContainsKey("highway") ?? false;
                                if (highway)
                                {
                                    dbWay = new MapHighway();
                                    var dbHighway = (MapHighway)dbWay;
                                    if (way.Tags.TryGetValue("highway", out var value))
                                    {
                                        dbHighway.HighwayType = value;
                                        way.Tags.RemoveKey("highway");
                                    }
                                    if (way.Tags.TryGetValue("sidewalk", out value))
                                    {
                                        dbHighway.Sidewalk = value;
                                        way.Tags.RemoveKey("sidewalk");
                                    }
                                    if (way.Tags.TryGetValue("cycleway", out value))
                                    {
                                        dbHighway.CyclewayType = value;
                                        way.Tags.RemoveKey("cycleway");
                                    }
                                    if (way.Tags.TryGetValue("busway", out value) && value == "lane")
                                    {
                                        dbHighway.BusWay = true;
                                    }
                                    if (way.Tags.TryGetValue("abutters", out value))
                                    {
                                        dbHighway.Abutters = value;
                                        way.Tags.RemoveKey("abutters");
                                    }
                                    if (way.Tags.TryGetValue("bicycle_road", out value) && value == "yes")
                                    {
                                        dbHighway.BicycleRoad = true;
                                    }
                                    if (way.Tags.TryGetValue("incline", out value))
                                    {
                                        dbHighway.Incline = value;
                                        way.Tags.RemoveKey("incline");
                                    }
                                    if (way.Tags.TryGetValue("junction", out value))
                                    {
                                        dbHighway.Junction = value;
                                        way.Tags.RemoveKey("junction");
                                    }
                                    if (way.Tags.TryGetValue("lanes", out value) && byte.TryParse(value, out var num))
                                    {
                                        dbHighway.Lanes = num;
                                        way.Tags.RemoveKey("lanes");
                                    }
                                    if (way.Tags.TryGetValue("motorroad", out value))
                                    {
                                        dbHighway.MotorRoad = value;
                                        way.Tags.RemoveKey("motorroad");
                                    }
                                    if (way.Tags.TryGetValue("parking:condition", out value))
                                    {
                                        dbHighway.ParkingCondition = value;
                                        way.Tags.RemoveKey("parking:condition");
                                    }
                                    if (way.Tags.TryGetValue("parking:lane", out value))
                                    {
                                        dbHighway.ParkingLane = value;
                                        way.Tags.RemoveKey("parking:lane");
                                    }
                                    if (way.Tags.TryGetValue("service", out value))
                                    {
                                        dbHighway.Service = value;
                                        way.Tags.RemoveKey("service");
                                    }
                                    if (way.Tags.TryGetValue("surface", out value))
                                    {
                                        dbHighway.Surface = value;
                                        way.Tags.RemoveKey("surface");
                                    }
                                    if (way.Tags.TryGetValue("maxwidth", out value))
                                    {
                                        dbHighway.MaxWidth = value;
                                        way.Tags.RemoveKey("maxwidth");
                                    }
                                    if (way.Tags.TryGetValue("maxheight", out value))
                                    {
                                        dbHighway.MaxHeight = value;
                                        way.Tags.RemoveKey("maxheight");
                                    }
                                    if (way.Tags.TryGetValue("maxweight", out value))
                                    {
                                        dbHighway.MaxWeight = value;
                                        way.Tags.RemoveKey("maxweight");
                                    }
                                    if (way.Tags.TryGetValue("maxspeed", out value) && TryConvertSpeedToKmh(value, out var speed))
                                    {
                                        dbHighway.MaxSpeed = speed;
                                        way.Tags.RemoveKey("maxspeed");
                                    }
                                    if (way.Tags.TryGetValue("oneway", out value))
                                    {
                                        dbHighway.OneWay = value;
                                        way.Tags.RemoveKey("oneway");
                                    }

                                    if (way.Tags.TryGetValue("turn:lanes", out value))
                                    {
                                        dbHighway.TurnLanes = value;
                                        way.Tags.RemoveKey("turn:lanes");
                                    }
                                    if (way.Tags.TryGetValue("destination:lanes", out value))
                                    {
                                        dbHighway.DestinationLanes = value;
                                        way.Tags.RemoveKey("destination:lanes");
                                    }
                                    if (way.Tags.TryGetValue("width:lanes", out value))
                                    {
                                        dbHighway.WidthLanes = value;
                                        way.Tags.RemoveKey("width:lanes");
                                    }
                                    if (way.Tags.TryGetValue("hov:lanes", out value))
                                    {
                                        dbHighway.HovLanes = value;
                                        way.Tags.RemoveKey("hov:lanes");
                                    }

                                    if (way.Tags.TryGetValue("turn:lanes:forward", out value))
                                    {
                                        dbHighway.TurnLanesForward = value;
                                        way.Tags.RemoveKey("turn:lanes:forward");
                                    }
                                    if (way.Tags.TryGetValue("destination:lanes:forward", out value))
                                    {
                                        dbHighway.DestinationLanesForward = value;
                                        way.Tags.RemoveKey("destination:lanes:forward");
                                    }
                                    if (way.Tags.TryGetValue("width:lanes:forward", out value))
                                    {
                                        dbHighway.WidthLanesForward = value;
                                        way.Tags.RemoveKey("width:lanes:forward");
                                    }
                                    if (way.Tags.TryGetValue("hov:lanes:forward", out value))
                                    {
                                        dbHighway.HovLanesForward = value;
                                        way.Tags.RemoveKey("hov:lanes:forward");
                                    }

                                    if (way.Tags.TryGetValue("turn:lanes:backward", out value))
                                    {
                                        dbHighway.TurnLanesBackward = value;
                                        way.Tags.RemoveKey("turn:lanes:backward");
                                    }
                                    if (way.Tags.TryGetValue("destination:lanes:backward", out value))
                                    {
                                        dbHighway.DestinationLanesBackward = value;
                                        way.Tags.RemoveKey("destination:lanes:backward");
                                    }
                                    if (way.Tags.TryGetValue("width:lanes:backward", out value))
                                    {
                                        dbHighway.WidthLanesBackward = value;
                                        way.Tags.RemoveKey("width:lanes:backward");
                                    }
                                    if (way.Tags.TryGetValue("hov:lanes:backward", out value))
                                    {
                                        dbHighway.HovLanesBackward = value;
                                        way.Tags.RemoveKey("hov:lanes:backward");
                                    }
                                    StageTagsForGeo(way, true);
                                }
                                else
                                {
                                    dbWay = new MapWay();
                                    StageTagsForGeo(way);
                                }


                                dbWay.Id = way.Id.Value;
                                dbWay.GeneratedDate = way.TimeStamp;
                                dbWay.SavedDate = DateTime.UtcNow;
                                dbWay.IsVisible = way.Visible;

                                bool minMaxSet = false;
                                ushort wnlIndex = 0;
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
                                    var dbWNL = new WayNodeLink() { NodeId = nodeId, WayId = way.Id.Value, ItemIndex = wnlIndex++, Highway = highway };
                                    StagedWayNodeLinks.Add(dbWNL);
                                }

                                if (highway)
                                {
                                    StagedHighways.Add((MapHighway)dbWay);
                                }
                                else
                                {
                                    StagedWays.Add(dbWay);
                                }
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
