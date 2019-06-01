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
using MapDataServer.Models;

namespace MapDataServer.Services
{
    public class MemoryDatabase : IDatabase
    {
        public ITable<MapRegion> MapRegions => throw new NotImplementedException();

        public ITable<MapNode> MapNodes => throw new NotImplementedException();

        public ITable<GeoTag> GeoTags => throw new NotImplementedException();

        public ITable<MapRelation> MapRelations => throw new NotImplementedException();

        public ITable<MapRelationMember> MapRelationMembers => throw new NotImplementedException();

        public ITable<MapWay> MapWays => throw new NotImplementedException();

        public ITable<MapHighway> MapHighways => throw new NotImplementedException();

        public ITable<WayNodeLink> WayNodeLinks => throw new NotImplementedException();

        public ITable<Trip> Trips => throw new NotImplementedException();

        public ITable<TripPoint> TripPoints => throw new NotImplementedException();

        public string ContextID => throw new NotImplementedException();

        public Func<ISqlBuilder> CreateSqlProvider => throw new NotImplementedException();

        public Func<ISqlOptimizer> GetSqlOptimizer => throw new NotImplementedException();

        public SqlProviderFlags SqlProviderFlags => throw new NotImplementedException();

        public Type DataReaderType => throw new NotImplementedException();

        public MappingSchema MappingSchema => throw new NotImplementedException();

        public bool InlineParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<string> QueryHints => throw new NotImplementedException();

        public List<string> NextQueryHints => throw new NotImplementedException();

        public bool CloseAfterUse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler OnClosing;

        public Task BulkInsert<T>(IEnumerable<T> values, bool orReplace = false)
        {
            throw new NotImplementedException();
        }

        public IDataContext Clone(bool forNestedQuery)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IQueryRunner GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
        {
            throw new NotImplementedException();
        }

        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        public bool? IsDBNullAllowed(IDataReader reader, int idx)
        {
            throw new NotImplementedException();
        }
    }
}
