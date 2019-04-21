using MapDataServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public interface IDbClient
    {
        Task Open();
        Task<bool> CreateTable<T>(string tableName, params IDbRowParameter[] parameters) where T : ITuple;
    }
}
