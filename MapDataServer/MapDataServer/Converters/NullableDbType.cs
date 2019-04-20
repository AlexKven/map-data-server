using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MapDataServer.Converters
{
    public class NullableDbType<T> : DbType<T?> where T : struct
    {
        public NullableDbType(IDbType<T> baseDbType)
            : base(baseDbType.DbTypeName,
                  val =>
                  {
                      if (val == null)
                          return null;
                      else
                          return baseDbType.FromString(val);
                  },
                  val =>
                  {
                      if (val.HasValue)
                          return baseDbType.ToString(val.Value);
                      return null;
                  }, null, false)
        { }
    }

    public class DependencyInjectedNullableDbType<T> : NullableDbType<T> where T : struct
    {
        public DependencyInjectedNullableDbType(IServiceProvider provider)
            : base(provider.GetService<IDbType<T>>()) { }
    }
}
