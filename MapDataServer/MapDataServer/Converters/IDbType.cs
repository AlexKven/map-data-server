using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Converters
{
    public interface IDbType<T>
    {
        T FromString(string value);
        string ToString(T value);
        string DbTypeName { get; }
        bool NotNull { get; }
        T NullValue { get; }
    }
}
