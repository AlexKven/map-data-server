using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MapDataServer.Utilities
{
    public class DbRow<T> : IDbRow<T> where T : ITuple
    {
        private IDbRowParameter[] Parameters { get; }

        public DbRow(T values, params IDbRowParameter[] parameters)
        {
            Parameters = parameters;
        }
    }
}
