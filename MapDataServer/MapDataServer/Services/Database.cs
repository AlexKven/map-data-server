using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        // From https://stackoverflow.com/questions/10235507/determine-which-sql-data-types-require-value-to-be-quoted
        static string RenderField(string fieldValue, DataType dataType)
        {
            if (fieldValue == null)
                return "NULL";

            if (fieldValue.Trim() == string.Empty)
                return "''";

            if (new DataType[]
            {
                DataType.Boolean,
                DataType.Byte,
                DataType.Decimal,
                DataType.Double,
                DataType.Int16,
                DataType.Int32,
                DataType.Int64,
                DataType.SByte,
                DataType.Single,
                DataType.UInt16,
                DataType.UInt32,
                DataType.UInt64
            }.Contains(dataType))
                return fieldValue;
            return $"'{fieldValue.Replace("'", "''")}'";
        }

        public async Task BulkInsert<T>(IEnumerable<T> values)
        {
            var type = typeof(T);
            var attribute = type.GetCustomAttributes(false).Where(att => att is TableAttribute).FirstOrDefault() as TableAttribute;

            StringBuilder query = new StringBuilder();
            var propertyInfos = type.GetProperties()
                .Select(prop => (prop,
                prop.GetCustomAttributes(true).Where(att => att is ColumnAttribute).FirstOrDefault() as ColumnAttribute,
                prop.GetCustomAttributes(true).Where(att => att is DataTypeAttribute).FirstOrDefault() as DataTypeAttribute))
                .Where(prop => prop.Item2 != null && (prop.Item2.DataType != DataType.Undefined || prop.Item3.DataType != DataType.Undefined));


            query.Append($"INSERT INTO `{attribute.Name}`({string.Join(",", propertyInfos.Select(prop => $"`{prop.Item2.Name}`"))}) VALUES ");

            bool firstRow = true;
            foreach (var value in values)
            {
                if (!firstRow)
                    query.Append(",");
                query.AppendLine();
                query.Append("(");
                firstRow = false;
                bool first = true;
                foreach (var prop in propertyInfos)
                {
                    var dataType = prop.Item2.DataType;
                    if (dataType == DataType.Undefined)
                        dataType = prop.Item3.DataType.Value;

                    string rendered = RenderField(prop.prop.GetValue(value)?.ToString(), dataType);
                    query.Append(first ? rendered : $", {rendered}");
                    first = false;
                }
                query.Append(")");
            }
            query.Append(";");

            if (firstRow)
                return;
            var affected = await this.ExecuteAsync(query.ToString());
        }
    }
}
