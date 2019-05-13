using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class SqlableFunctions
    {
        public static double DistanceBetweenPoints(double lon1, double lat1, double lon2, double lat2)
            => Math.Sqrt((lon1 - lon2) * (lon1 - lon2) + (lat1 - lat2) * (lat1 - lat2));
    }
}
