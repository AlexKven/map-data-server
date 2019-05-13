using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public struct GeoPoint
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public GeoPoint(double longitude, double latitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double DistanceTo(GeoPoint other) => Math.Sqrt((this.Longitude - other.Longitude) * (this.Longitude - other.Longitude) + (this.Latitude - other.Latitude) * (this.Latitude - other.Latitude));

        public static double Distance(GeoPoint p1, GeoPoint p2) => p1.DistanceTo(p2);
    }
}
