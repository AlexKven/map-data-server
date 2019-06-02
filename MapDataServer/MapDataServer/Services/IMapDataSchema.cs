using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IMapDataSchema
    {
        IQueryable<MapRegion> MapRegions { get; }
        IQueryable<MapNode> MapNodes { get; }
        IQueryable<GeoTag> GeoTags { get; }
        IQueryable<MapRelation> MapRelations { get; }
        IQueryable<MapRelationMember> MapRelationMembers { get; }
        IQueryable<MapWay> MapWays { get; }
        IQueryable<MapHighway> MapHighways { get; }
        IQueryable<WayNodeLink> WayNodeLinks { get; }
        IQueryable<Trip> Trips { get; }
        IQueryable<TripPoint> TripPoints { get; }
    }
}
