using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class UtilityExtensions
    {
        public static bool TryUse<T>(this T? nullable, out T value) where T : struct
        {
            if (nullable.HasValue)
            {
                value = nullable.Value;
                return true;
            }
            value = default;
            return false;
        }

        public static IEnumerable<(T, T)> PointsToLines<T>(this IEnumerable<T> points)
        {
            bool first = true;
            T last = default;
            foreach (var point in points)
            {
                if (!first)
                {
                    yield return (last, point);
                }
                first = false;
                last = point;
            }
        }

        public static long RandomLong(this Random random)
        {
            var bytes = new byte[8];
            new Random().NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}
