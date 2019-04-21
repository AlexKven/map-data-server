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
        object Default { get; }
        bool AutoIncrement { get; }
        bool PrimaryKey { get; }

        IDbRowParameter MakeNotNull();
        IDbRowParameter BlockNotNull();
        IDbRowParameter MakeAutoIncrement();
        IDbRowParameter MakePrimaryKey();
        IDbRowParameter SetDefault(object _default);
    }
}
