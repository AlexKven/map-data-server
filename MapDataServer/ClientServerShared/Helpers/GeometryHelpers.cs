using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Helpers
{
    public static partial class GeometryHelpers
    {
        public class TripPointWithEdges
        {
            public TripPointWithEdges(
                TripPoint tripPoint,
                (double, double)? inEdge,
                (double, double)? outEdge)
            {
                TripPoint = tripPoint;
                InEdge = inEdge;
                OutEdge = outEdge;
            }

            public TripPoint TripPoint { get; }
            public (double lat, double lon)? InEdge { get; }
            public (double lat, double lon)? OutEdge { get; }
        }

        // Adapted from https://stackoverflow.com/a/51839058
        public static double GetDistance(double latitude, double longitude, double otherLatitude, double otherLongitude)
        {
            if (latitude == otherLatitude && longitude == otherLongitude)
                return 0;

            var latA = latitude * Math.PI / 180.0;
            var lonA = longitude * Math.PI / 180.0;
            var latB = otherLatitude * Math.PI / 180.0;
            var lonB = otherLongitude * Math.PI / 180.0;

            double R = 6371000;
            return Math.Acos(Math.Sin(latA) * Math.Sin(latB) + Math.Cos(latA) * Math.Cos(latB) * Math.Cos(lonB - lonA)) * R;
        }

        private static double DistanceBetweenPointsRadians(double latA, double lonA, double latB, double lonB)
        {
            double R = 6371000;
            return Math.Acos(Math.Sin(latA) * Math.Sin(latB) + Math.Cos(latA) * Math.Cos(latB) * Math.Cos(lonB - lonA)) * R;
        }

        private static double BearingBetweenPointsRadians(double latA, double lonA, double latB, double lonB)
        {
            // BEAR Finds the bearing from one lat / lon point to another.
            return Math.Atan2(Math.Sin(lonB - lonA) * Math.Cos(latB), Math.Cos(latA) * Math.Sin(latB) - Math.Sin(latA) * Math.Cos(latB) * Math.Cos(lonB - lonA));
        }

        public static double DistanceToLine(
            double latP, double lonP,
            double latS, double lonS,
            double latE, double lonE,
            out double relativeClosestPointOnLine)
        {
            // https://stackoverflow.com/a/54665914/6706737
            var lat1 = latS * Math.PI / 180.0;
            var lon1 = lonS * Math.PI / 180.0;
            var lat2 = latE * Math.PI / 180.0;
            var lon2 = lonE * Math.PI / 180.0;
            var lat3 = latP * Math.PI / 180.0;
            var lon3 = lonP * Math.PI / 180.0;

            // Earth's radius in meters
            double R = 6371000;

            // Prerequisites for the formulas
            double bear12 = BearingBetweenPointsRadians(lat1, lon1, lat2, lon2);
            double bear13 = BearingBetweenPointsRadians(lat1, lon1, lat3, lon3);
            double dis13 = DistanceBetweenPointsRadians(lat1, lon1, lat3, lon3);

            // Is relative bearing obtuse?
            if (Math.Abs(bear13 - bear12) > (Math.PI / 2))
            {
                relativeClosestPointOnLine = 0;
                return dis13;
            }

            // Find the cross-track distance.
            double dxt = Math.Asin(Math.Sin(dis13 / R) * Math.Sin(bear13 - bear12)) * R;

            // Is p4 beyond the arc?
            double dis12 = DistanceBetweenPointsRadians(lat1, lon1, lat2, lon2);
            double dis14 = Math.Acos(Math.Cos(dis13 / R) / Math.Cos(dxt / R)) * R;
            if (dis14 > dis12)
            {
                relativeClosestPointOnLine = 1;
                return DistanceBetweenPointsRadians(lat2, lon2, lat3, lon3);
            }
            relativeClosestPointOnLine = dis14 / dis12;
            return Math.Abs(dxt);
        }

        public static IEnumerable<(double latitude, double longitude)> DecodePoints(string encodedPolyline)
        {
            int index = 0;
            int latitude = 0;
            int longitude = 0;

            int length = encodedPolyline.Length;

            while (index < length)
            {
                latitude += DecodePoint(encodedPolyline, index, out index);
                longitude += DecodePoint(encodedPolyline, index, out index);

                yield return (
                    latitude * 1e-5,
                    longitude * 1e-5);
            }
        }

        private static int DecodePoint(string encoded, int startindex, out int finishindex)
        {
            int b;
            int shift = 0;
            int result = 0;

            //magic google algorithm, see http://code.google.com/apis/maps/documentation/polylinealgorithm.html #credit
            do
            {
                b = Convert.ToInt32(encoded[startindex++]) - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            //if negative flip
            int dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));

            //set output index
            finishindex = startindex;

            return dlat;
        }

        public static double GetDistance((double latitude, double longitude) first,
            (double latitude, double longitude) second) => GetDistance(
                first.latitude, first.longitude, second.latitude, second.longitude);

        public static double DistanceToLine((double latitude, double longitude) point,
            (double latitude, double longitude) start, (double latitude, double longitude) end,
            out double relativeClosestPointOnLine) => DistanceToLine(point.latitude, point.longitude,
                start.latitude, start.longitude, end.latitude, end.longitude, out relativeClosestPointOnLine);

        public static uint GetTotalLength(IEnumerable<TripPoint> points,
            List<TripPointWithEdges> outUsefulPointEdges = null)
        {
            double distance = 0;
            TripPoint lastUsefulPoint = null;
            (double lat, double lon)? lastUsefulPointEdge = null;

            int currentPointIndex = -1;
            using (var enumerator = points.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    currentPointIndex++;
                    if (lastUsefulPoint == null)
                        lastUsefulPoint = enumerator.Current;
                    else
                    {
                        var point = enumerator.Current;
                        var dist = ShortestDistanceBetweenTripPoints(lastUsefulPoint, point);
                        if (dist.HasValue)
                        {
                            distance += dist.Value.dist;
                            if (lastUsefulPointEdge != null)
                                distance += GetDistance(lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon,
                                    dist.Value.p1EdgeLat, dist.Value.p1EdgeLon);

                            if (outUsefulPointEdges != null)
                            {
                                if (lastUsefulPointEdge == null)
                                    outUsefulPointEdges.Add(new TripPointWithEdges(lastUsefulPoint, null, (dist.Value.p1EdgeLat, dist.Value.p1EdgeLon)));
                                else
                                    outUsefulPointEdges.Add(new TripPointWithEdges(lastUsefulPoint, (lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon),
                                        (dist.Value.p1EdgeLat, dist.Value.p1EdgeLon)));
                            }

                            lastUsefulPoint = point;
                            lastUsefulPointEdge = (lat: dist.Value.p2EdgeLat, lon: dist.Value.p2EdgeLon);
                        }
                    }
                }
                if (lastUsefulPoint != null)
                {
                    if (outUsefulPointEdges != null)
                    {
                        if (lastUsefulPointEdge == null)
                            outUsefulPointEdges.Add(new TripPointWithEdges(lastUsefulPoint, null, null));
                        else
                            outUsefulPointEdges.Add(new TripPointWithEdges(lastUsefulPoint,
                                (lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon), null));
                    }
                }
                return (uint)distance;
            }
        }

        public static (double dist, double p1EdgeLat, double p1EdgeLon, double p2EdgeLat, double p2EdgeLon)?
            ShortestDistanceBetweenTripPoints(TripPoint p1, TripPoint p2)
        {
            var distanceBetweenCenters = GeometryHelpers.GetDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
            var distanceBetweenEdges = distanceBetweenCenters - p1.RangeRadius - p2.RangeRadius;
            if (distanceBetweenEdges < 0)
                return null;

            var p1RadiusFactor = p1.RangeRadius / distanceBetweenCenters;
            var p2RadiusFactor = p2.RangeRadius / distanceBetweenCenters;
            var p1EdgeLat = p1.Latitude + (p2.Latitude - p1.Latitude) * p1RadiusFactor;
            var p1EdgeLon = p1.Longitude + (p2.Longitude - p1.Longitude) * p1RadiusFactor;
            var p2EdgeLat = p2.Latitude + (p1.Latitude - p2.Latitude) * p2RadiusFactor;
            var p2EdgeLon = p2.Longitude + (p1.Longitude - p2.Longitude) * p2RadiusFactor;
            return (distanceBetweenEdges, p1EdgeLat, p1EdgeLon, p2EdgeLat, p2EdgeLon);
        }
    }
}
