using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;

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

        public IDbConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
