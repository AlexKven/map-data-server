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
    public interface IDatabase : IDataContext, IMapDataSchema
    {
        new ITable<MapRegion> MapRegions { get; }
        new ITable<MapNode> MapNodes { get; }
        new ITable<GeoTag> GeoTags { get; }
        new ITable<MapRelation> MapRelations { get; }
        new ITable<MapRelationMember> MapRelationMembers { get; }
        new ITable<MapWay> MapWays { get; }
        new ITable<MapHighway> MapHighways { get; }
        new ITable<WayNodeLink> WayNodeLinks { get; }
        new ITable<Trip> Trips { get; }
        new ITable<TripPoint> TripPoints { get; }
        Task Initialize();
        Task BulkInsert<T>(IEnumerable<T> values, bool orReplace = false);
    }
}
