using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class DbClient
    {
        private IDbConnectionFactory ConnectionFactory { get; }
        private IDbConnection CurrentConnection { get; set; }

        public DbClient(IDbConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
        }
    }
}
