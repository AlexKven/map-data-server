using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class TimeHelpers
    {
        public static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromEpochMilliseconds(long milliseconds) => Epoch.AddMilliseconds(milliseconds);

        public static long ToEpochMilliseconds(DateTime time) => (long)(time - Epoch).TotalMilliseconds;

        public static DateTime GetUtcStartOfDayForTimeZone(string timeZone, DateTime serviceDay)
        {
            // Thanks to https://stackoverflow.com/a/15213447/6706737
            var tzdb = DateTimeZoneProviders.Tzdb;

            var zone1 = tzdb[timeZone];

            // Gotta do weird things so DST doesn't break it
            var zonedDate = zone1.AtStrictly(new LocalDateTime(serviceDay.Year, serviceDay.Month, serviceDay.Day, 4, 0));
            zonedDate = zonedDate.Minus(Duration.FromHours(4));
            return zonedDate.ToDateTimeUtc();
        }
    }
}
