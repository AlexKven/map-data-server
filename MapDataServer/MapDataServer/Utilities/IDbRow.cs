using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MapDataServer.Utilities
{
    public interface IDbRow<T> where T : ITuple
    {
    }
}
