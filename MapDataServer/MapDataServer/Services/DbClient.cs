using MapDataServer.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MapDataServer.Helpers;
using MapDataServer.Converters;

namespace MapDataServer.Services
{
    public class DbClient : IDbClient
    {
        private IDbConnectionFactory ConnectionFactory { get; }
        private DbConnection CurrentConnection { get; set; }
        private IServiceProvider ServiceProvider { get; }

        public DbClient(IDbConnectionFactory connectionFactory, IServiceProvider serviceProvider)
        {
            ConnectionFactory = connectionFactory;
            CurrentConnection = ConnectionFactory.GetConnection();
            ServiceProvider = serviceProvider;
        }

        public async Task Open()
        {
            await CurrentConnection.OpenAsync();
        }

        public async Task<bool> CreateTable<T>(string tableName, params IDbRowParameter[] parameters) where T : ITuple
        {
            var command = CurrentConnection.CreateCommand();
            StringBuilder builder = new StringBuilder();
            var types = typeof(T).GetTupleTypes();
            if (types == null)
                throw new ArgumentException("Generic parameter T is not a proper tuple type.");
            List<string> primaryKeys = new List<string>();
            int index = 0;
            builder.Append($"CREATE TABLE IF NOT EXISTS {tableName} (");
            //var commParameter = command.CreateParameter();
            //commParameter.ParameterName = "@tableName";
            //commParameter.Value = tableName;
            //command.Parameters.Add(commParameter);
            foreach (var type in types)
            {
                builder.Append("\n");
                if (index > 0)
                    builder.Append(", ");
                var dbType = ServiceProvider.GetService(typeof(IDbType<>).MakeGenericType(type)) as IDbType;
                var parameter = parameters[index];

                //commParameter = command.CreateParameter();
                //commParameter.ParameterName = $"@type{index}";
                //commParameter.Value = dbType.DbTypeName;
                //command.Parameters.Add(commParameter);
                //commParameter = command.CreateParameter();
                //commParameter.ParameterName = $"@name{index}";
                //commParameter.Value = parameter.Name;
                //command.Parameters.Add(commParameter);
                if (parameter.PrimaryKey)
                {
                    //commParameter = command.CreateParameter();
                    //commParameter.ParameterName = $"@primaryKey{index}";
                    //commParameter.Value = parameter.Name;
                    //command.Parameters.Add(commParameter);
                    primaryKeys.Add(parameter.Name);// $"@primaryKey{index}");
                }


                //builder.Append($"@name{index} @type{index}");
                builder.Append($"{parameter.Name} {dbType.DbTypeName}");
                if (parameter?.NotNull ?? dbType.NotNull)
                    builder.Append(" NOT NULL");
                if (parameter?.AutoIncrement ?? false)
                    builder.Append(" AUTO_INCREMENT");
                index++;
            }
            if (primaryKeys.Count > 0)
            {
                builder.Append($", PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }
            builder.Append(");");

            command.CommandText = builder.ToString();
            await command.ExecuteNonQueryAsync();

            return true;
        }
    }
}
