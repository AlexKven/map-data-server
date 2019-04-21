using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Converters
{
    public interface IDbType<T> : IDbType
    {
        T FromString(string value);
        string ToString(T value);
        T NullValue { get; }
    }

    public interface IDbType
    {
        bool NotNull { get; }
        string DbTypeName { get; }
    }
}
