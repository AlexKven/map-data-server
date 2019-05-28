using LinqToDB;
using LinqToDB.Data;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IDatabase : IDataContext
    {
        ITable<MapRegion> MapRegions { get; }
        ITable<MapNode> MapNodes { get; }
        ITable<GeoTag> GeoTags { get; }
        ITable<MapRelation> MapRelations { get; }
        ITable<MapRelationMember> MapRelationMembers { get; }
        ITable<MapWay> MapWays { get; }
        ITable<MapHighway> MapHighways { get; }
        ITable<WayNodeLink> WayNodeLinks { get; }
        ITable<Trip> Trips { get; }
        ITable<TripPoint> TripPoints { get; }
        Task Initializer { get; }
        Task BulkInsert<T>(IEnumerable<T> values, bool orReplace = false);
        Task<FullTrip> GetFullTrip(long tripId);
    }
}
