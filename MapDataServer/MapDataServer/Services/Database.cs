using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public ITable<MapHighway> MapHighways => GetTable<MapHighway>();

        public ITable<WayNodeLink> WayNodeLinks => GetTable<WayNodeLink>();

        public ITable<Trip> Trips => GetTable<Trip>();

        public ITable<PreprocessedTrip> PreprocessedTrips => GetTable<PreprocessedTrip>();

        public ITable<TripPoint> TripPoints => GetTable<TripPoint>();

        public ITable<ObaTripLink> ObaTripLinks => GetTable<ObaTripLink>();

        public ITable<ObaTrip> ObaTrips => GetTable<ObaTrip>();

        public ITable<ObaServicePeriod> ObaServicePeriods => GetTable<ObaServicePeriod>();

        public ITable<ObaRoute> ObaRoutes => GetTable<ObaRoute>();

        public ITable<ObaTripStopLink> ObaTripStopLinks => GetTable<ObaTripStopLink>();

        private async Task InitializeAsync()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sp = DataProvider.GetSchemaProvider();
            var tableTypes = sp.GetSchema(this).Tables.Select(table => table.TableName);
            if (!tableTypes.Contains("GeoTags"))
            {
                await this.CreateTableAsync<GeoTag>();
            }
            if (!tableTypes.Contains("MapNodes"))
            {
                await this.CreateTableAsync<MapNode>();
                await this.ExecuteAsync("ALTER TABLE MapNodes ADD INDEX LonLat(Longitude, Latitude)");
                await this.ExecuteAsync("ALTER TABLE MapNodes ADD INDEX LatLon(Latitude, Longitude);");
                await this.ExecuteAsync("ALTER TABLE MapNodes ADD INDEX SavedDate(SavedDate);");
                await this.ExecuteAsync("ALTER TABLE MapNodes ADD INDEX Region(Region);");
            }
            if (!tableTypes.Contains("MapRegions"))
            {
                await this.CreateTableAsync<MapRegion>();
            }
            if (!tableTypes.Contains("MapRelations"))
            {
                await this.CreateTableAsync<MapRelation>();
                await this.ExecuteAsync("ALTER TABLE MapRelations ADD INDEX SavedDate(SavedDate);");
            }
            if (!tableTypes.Contains("MapRelationMembers"))
            {
                await this.CreateTableAsync<MapRelationMember>();
                await this.ExecuteAsync("ALTER TABLE MapRelationMembers ADD INDEX GeoId(GeoId);");
                await this.ExecuteAsync("ALTER TABLE MapRelationMembers ADD INDEX `Type`(`Type`);");
            }
            if (!tableTypes.Contains("MapWays"))
            {
                await this.CreateTableAsync<MapWay>();
                await this.ExecuteAsync("ALTER TABLE MapWays ADD INDEX SavedDate(SavedDate);");
                await this.ExecuteAsync("ALTER TABLE MapWays ADD INDEX MinLonLat(MinLon, MinLat);");
                await this.ExecuteAsync("ALTER TABLE MapWays ADD INDEX MinLatLon(MinLat, MinLon);");
                await this.ExecuteAsync("ALTER TABLE MapWays ADD INDEX MaxLonLat(MaxLon, MaxLat);");
                await this.ExecuteAsync("ALTER TABLE MapWays ADD INDEX MaxLatLon(MaxLat, MaxLon);");
            }
            if (!tableTypes.Contains("MapHighways"))
            {
                await this.CreateTableAsync<MapHighway>();
                await this.ExecuteAsync("ALTER TABLE MapHighways ADD INDEX SavedDate(SavedDate);");
                await this.ExecuteAsync("ALTER TABLE MapHighways ADD INDEX MinLonLat(MinLon, MinLat);");
                await this.ExecuteAsync("ALTER TABLE MapHighways ADD INDEX MinLatLon(MinLat, MinLon);");
                await this.ExecuteAsync("ALTER TABLE MapHighways ADD INDEX MaxLonLat(MaxLon, MaxLat);");
                await this.ExecuteAsync("ALTER TABLE MapHighways ADD INDEX MaxLatLon(MaxLat, MaxLon);");
            }
            if (!tableTypes.Contains("WayNodeLinks"))
            {
                await this.CreateTableAsync<WayNodeLink>();
                await this.ExecuteAsync("ALTER TABLE WayNodeLinks ADD INDEX WayId(WayId);");
            }
            if (!tableTypes.Contains("Trips"))
            {
                await this.CreateTableAsync<Trip>();
                await this.ExecuteAsync("ALTER TABLE Trips ADD INDEX StartTime(StartTime);");
                await this.ExecuteAsync("ALTER TABLE Trips ADD INDEX EndTime(EndTime);");
            }
            if (!tableTypes.Contains("PreprocessedTrips"))
            {
                await this.CreateTableAsync<PreprocessedTrip>();
                await this.ExecuteAsync("ALTER TABLE PreprocessedTrips ADD INDEX ActualStartTime(ActualStartTime);");
                await this.ExecuteAsync("ALTER TABLE PreprocessedTrips ADD INDEX ActualEndTime(ActualEndTime);");
                await this.ExecuteAsync("ALTER TABLE PreprocessedTrips ADD INDEX DistanceMeters(DistanceMeters);");
            }
            if (!tableTypes.Contains("TripPoints"))
            {
                await this.CreateTableAsync<TripPoint>();
                await this.ExecuteAsync("ALTER TABLE TripPoints ADD INDEX Longitude(Longitude);");
                await this.ExecuteAsync("ALTER TABLE TripPoints ADD INDEX Latitude(Latitude);");
                await this.ExecuteAsync("ALTER TABLE TripPoints ADD INDEX Time(Time);");
                await this.ExecuteAsync("ALTER TABLE TripPoints ADD INDEX TripId(TripId);");
            }
            if (!tableTypes.Contains("ObaTripLinks"))
            {
                await this.CreateTableAsync<ObaTripLink>();
            }
            if (!tableTypes.Contains("ObaServicePeriods"))
            {
                await this.CreateTableAsync<ObaServicePeriod>();
            }
            if (!tableTypes.Contains("ObaTrips"))
            {
                await this.CreateTableAsync<ObaTrip>();
                await this.ExecuteAsync("ALTER TABLE ObaTrips ADD INDEX ObaRouteId(ObaRouteId);");
            }
            if (!tableTypes.Contains("ObaRoutes"))
            {
                await this.CreateTableAsync<ObaRoute>();
            }
            if (!tableTypes.Contains("ObaTripStopLinks"))
            {
                await this.CreateTableAsync<ObaTripStopLink>();
                await this.ExecuteAsync("ALTER TABLE ObaTripStopLinks ADD INDEX StopSequence(StopSequence);");
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

        private async Task<bool> BulkInserting<T>(IEnumerator<T> enumerator, bool orReplace,
            TableAttribute tableAttribute, IEnumerable<(PropertyInfo prop, ColumnAttribute, DataTypeAttribute)> propertyInfos)
        {
            StringBuilder query = new StringBuilder();
            query.Append($"{(orReplace ? "REPLACE" : "INSERT IGNORE") } INTO `{tableAttribute.Name}`({string.Join(",", propertyInfos.Select(prop => $"`{prop.Item2.Name}`"))}) VALUES ");

            int maxQueryLength = 1000000;
            bool firstRow = true;
            bool continues = false;
            while (query.Length < maxQueryLength && (continues = enumerator.MoveNext()))
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
                        dataType = prop.Item3.DataType ?? DataType.Undefined;

                    string rendered = RenderField(prop.prop.GetValue(enumerator.Current)?.ToString(), dataType);
                    query.Append(first ? rendered : $", {rendered}");
                    first = false;
                }
                query.Append(")");
            }
            query.Append(";");

            if (firstRow)
                return true;
            var affected = await this.ExecuteAsync(query.ToString());
            return !continues;
        }

        public async Task BulkInsert<T>(IEnumerable<T> values, bool orReplace = false)
        {
            var type = typeof(T);
            var tableAttribute = type.GetCustomAttributes(false).Where(att => att is TableAttribute).FirstOrDefault() as TableAttribute;

            var propertyInfos = type.GetProperties()
                .Select(prop => (prop,
                prop.GetCustomAttributes(true).Where(att => att is ColumnAttribute).FirstOrDefault() as ColumnAttribute,
                prop.GetCustomAttributes(true).Where(att => att is DataTypeAttribute).FirstOrDefault() as DataTypeAttribute))
                .Where(prop => prop.Item2 != null);

            var enumerator = values.GetEnumerator();
            while (!await BulkInserting(enumerator, orReplace, tableAttribute, propertyInfos)) ;
        }
    }
}
