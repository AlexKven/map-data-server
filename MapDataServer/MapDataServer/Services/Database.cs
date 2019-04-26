using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class Database : LinqToDB.Data.DataConnection, IDatabase
    {
        public Database(IConfiguration config, IDataProvider dataProvider)
            : base(dataProvider, config.GetConnectionString("MySQL"))
        {
            Initializer = InitializeAsync();
        }

        public Task Initializer { get; }

        public ITable<GeoTag> GeoTags => GetTable<GeoTag>();

        public ITable<MapRegion> MapRegions => GetTable<MapRegion>();

        public ITable<MapNode> MapNodes => GetTable<MapNode>();

        public ITable<MapRelation> MapRelations => GetTable<MapRelation>();

        public ITable<MapRelationMember> MapRelationMembers => GetTable<MapRelationMember>();

        public ITable<MapWay> MapWays => GetTable<MapWay>();

        public ITable<WayNodeLink> WayNodeLinks => GetTable<WayNodeLink>();

        private async Task InitializeAsync()
        {
            var sp = DataProvider.GetSchemaProvider();
            var tableTypes = sp.GetSchema(this).Tables.Select(table => table.TableName);
            if (!tableTypes.Contains("GeoTags"))
            {
                await this.CreateTableAsync<GeoTag>();
            }
            if (!tableTypes.Contains("MapNodes"))
            {
                await this.CreateTableAsync<MapNode>();
            }
            if (!tableTypes.Contains("MapRegions"))
            {
                await this.CreateTableAsync<MapRegion>();
            }
            if (!tableTypes.Contains("MapRelations"))
            {
                await this.CreateTableAsync<MapRelation>();
            }
            if (!tableTypes.Contains("MapRelationMembers"))
            {
                await this.CreateTableAsync<MapRelationMember>();
            }
            if (!tableTypes.Contains("MapWays"))
            {
                await this.CreateTableAsync<MapWay>();
            }
            if (!tableTypes.Contains("WayNodeLinks"))
            {
                await this.CreateTableAsync<WayNodeLink>();
            }
        }
    }
}
