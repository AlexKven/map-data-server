using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;

namespace MapDataServer.Services
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private string ConnectionString { get; }

        public DbConnectionFactory(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));
            ConnectionString = connectionString;
        }

        public DbConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
