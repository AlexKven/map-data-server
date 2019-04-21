using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IDbConnectionFactory
    {
        DbConnection GetConnection();
    }
}
