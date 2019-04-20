using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MapDataServer.Services
{
    public class DatabaseConnection  : IDatabaseConnection
    {
        private MySqlConnection UnderlyingConnection { get; }

        public DatabaseConnection(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));
            UnderlyingConnection = new MySqlConnection(connectionString);
        }

        public Task OpenAsync()
        {
            return UnderlyingConnection.OpenAsync();
        }

        public Task CloseAsync()
        {
            return UnderlyingConnection.CloseAsync();
        }

        public void Dispose()
        {
            UnderlyingConnection.Dispose();
        }
    }
}
