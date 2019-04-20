using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Converters
{
    public class DbType<T> : IDbType<T>
    {
        private Func<string, T> FromStringFunc { get; }
        private Func<T, string> ToStringFunc { get; }

        public string DbTypeName { get; }

        public bool NotNull { get; }

        public T NullValue { get; }

        private static bool CanBeNull(Type type) =>
            !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

        public DbType(string dbTypeName)
            : this(dbTypeName, val =>
            {
                var result = Convert.ChangeType(val, typeof(T));
                if (result == null)
                    return default(T);
                return (T)result;
            }, val => val.ToString(), default(T), CanBeNull(typeof(T))) { }

        public DbType(string dbTypeName, Func<string, T> fromStringFunc)
            : this(dbTypeName, fromStringFunc, val => val.ToString(), default(T), CanBeNull(typeof(T))) { }

        public DbType(string dbTypeName, Func<string, T> fromStringFunc, Func<T, string> toStringFunc)
            : this(dbTypeName, fromStringFunc, toStringFunc, default(T), CanBeNull(typeof(T))) { }

        public DbType(string dbTypeName, Func<string, T> fromStringFunc, Func<T, string> toStringFunc, T nullValue)
            : this(dbTypeName, fromStringFunc, toStringFunc, default(T), !CanBeNull(typeof(T))) { }

        public DbType(string dbTypeName, Func<string, T> fromStringFunc, Func<T, string> toStringFunc, T nullValue, bool notNull)
        {
            FromStringFunc = fromStringFunc;
            ToStringFunc = toStringFunc;
            DbTypeName = dbTypeName;
            NotNull = notNull;
            NullValue = nullValue;
        }

        public T FromString(string value)
        {
            if (value == null)
                return NullValue;
            return FromStringFunc(value);
        }

        public string ToString(T value)
        {
            return ToStringFunc(value);
        }
    }
}
