using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public struct GeoPoint : IEquatable<GeoPoint>
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public GeoPoint(double longitude, double latitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        // From https://stackoverflow.com/questions/6544286/calculate-distance-of-two-geo-points-in-km-c-sharp
        public double DistanceTo(GeoPoint other)
        {
            var lat1 = Latitude * Math.PI / 180;
            var lat2 = other.Latitude * Math.PI / 180;
            var lon1 = Longitude * Math.PI / 180;
            var lon2 = other.Longitude * Math.PI / 180;


            double r = 6371; // km
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = r * c;

            return d;
        }

        public double DistanceTo(GeoPoint? other)
        {
            if (other == null)
                return 0;
            return DistanceTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoPoint point && Equals(point);
        }

        public bool Equals(GeoPoint other)
        {
            return Latitude == other.Latitude &&
                   Longitude == other.Longitude;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Latitude, Longitude);
        }

        public static bool operator ==(GeoPoint left, GeoPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeoPoint left, GeoPoint right)
        {
            return !(left == right);
        }
    }
}
