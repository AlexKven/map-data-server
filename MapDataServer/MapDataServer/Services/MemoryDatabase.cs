using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using MapDataServer.Helpers;
using MapDataServer.Models;

namespace MapDataServer.Services
{
    public class MemoryDatabase : IMapDataSchema
    {
        public MemoryDatabase() { }

        public MemoryDatabase(IMapDataSchema forward)
        {
            MapRegions = forward.MapRegions;
            MapNodes = forward.MapNodes;
            GeoTags = forward.GeoTags;
            MapRelations = forward.MapRelations;
            MapRelationMembers = forward.MapRelationMembers;
            MapWays = forward.MapWays;
            MapHighways = forward.MapHighways;
            WayNodeLinks = forward.WayNodeLinks;
            Trips = forward.Trips;
            TripPoints = forward.TripPoints;
        }

        public async Task SetFromRegion(IMapDataSchema database, GeoPoint swCorner, GeoPoint neCorner)
        {
            var nodes = await database.MapNodes.WithinBoundingBox(swCorner, neCorner).ToAsyncEnumerable().ToDictionary(node => node.Id);
            var links = await database.WayNodeLinks.Where(link => nodes.ContainsKey(link.NodeId)).ToAsyncEnumerable().ToList();
            var wayIds = links.Where(link => link.Highway == false).Select(link => link.WayId).Distinct().ToArray();
            var highwayIds = links.Where(link => link.Highway == true).Select(link => link.WayId).Distinct().ToArray();
            var ways = await (from way in database.MapWays where wayIds.Contains(way.Id) select way).ToAsyncEnumerable().ToList();
            var highways = await (from highway in database.MapHighways where highwayIds.Contains(highway.Id) select highway).ToAsyncEnumerable().ToList();

            MapNodes = nodes.Values.AsQueryable();
            MapWays = ways.AsQueryable();
            MapHighways = highways.AsQueryable();
            WayNodeLinks = links.AsQueryable();
        }

        public IQueryable<MapRegion> MapRegions { get; protected set; }

        public IQueryable<MapNode> MapNodes { get; protected set; }

        public IQueryable<GeoTag> GeoTags { get; protected set; }

        public IQueryable<MapRelation> MapRelations { get; protected set; }

        public IQueryable<MapRelationMember> MapRelationMembers { get; protected set; }

        public IQueryable<MapWay> MapWays { get; protected set; }

        public IQueryable<MapHighway> MapHighways { get; protected set; }

        public IQueryable<WayNodeLink> WayNodeLinks { get; protected set; }

        public IQueryable<Trip> Trips { get; protected set; }

        public IQueryable<TripPoint> TripPoints { get; protected set; }
    }
}
