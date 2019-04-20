using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Utilities
{
    public interface IDbRowParameter
    {
        string Name { get; }
        bool? NotNull { get; }
        bool Default { get; }
        bool AutoIncrement { get; }
        bool PrimaryKey { get; }
    }
}
