using LinqToDB;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class TripPreprocessor : ITripPreprocessor
    {
        private IDatabase Database { get; }
        public TripPreprocessor(IDatabase database)
        {
            Database = database;
        }

        public async Task<PreprocessedTrip> PreprocessTrip(long tripId)
        {
            await Database.Initializer;

            var duplicates = new List<TripPoint>();
            var points = await (from point in Database.TripPoints
                                where point.TripId == tripId
                                orderby point.Time ascending
                                select point).ToListAsync();

            DeDupe(points, duplicates);

            foreach (var point in duplicates)
            {
                await Database.DeleteAsync(point);
            }
            duplicates.Clear();

            StringBuilder builder = new StringBuilder();
            foreach (var point in points)
                builder.AppendLine($"{point.Latitude},{point.Longitude}");
            var res = builder.ToString();


            var startTailPointsCount = GetTailPointsCount(points);
            var pointsFromEnd = points.Reverse<TripPoint>();
            var endTailPointsCount = GetTailPointsCount(pointsFromEnd);
            var total = points.Count;

            var pointsToUpdate = new List<TripPoint>();

            foreach (var point in points.Take(startTailPointsCount))
            {
                point.IsTailPoint = true;
                pointsToUpdate.Add(point);
            }

            foreach (var point in pointsFromEnd.Take(endTailPointsCount))
            {
                point.IsTailPoint = true;
                pointsToUpdate.Add(point);
            }

            foreach (var point in pointsToUpdate)
            {
                await Database.InsertOrReplaceAsync(point);
            }
            pointsToUpdate.Clear();

            var result = new PreprocessedTrip();
            var usefulPoints = points.Skip(startTailPointsCount).Take(total - startTailPointsCount - endTailPointsCount);
            foreach (var pt in usefulPoints)
            {
                pt.IsTailPoint = false;
                pointsToUpdate.Add(pt);
            }

            var distance = GetTotalLength(usefulPoints);

            foreach (var point in pointsToUpdate)
            {
                await Database.InsertOrReplaceAsync(point);
            }

            DateTime actualStartTime;
            DateTime actualEndTime;
            if (usefulPoints.Any())
            {
                actualStartTime = (usefulPoints.First()).Time;
                actualEndTime = (usefulPoints.Last()).Time;
            }
            else if (points.Any())
            {
                actualStartTime = actualEndTime = points.First().Time;
            }
            else
                actualStartTime = actualEndTime = (await Database.Trips.FirstAsync(t => t.Id == tripId)).StartTime;

            var preprocessed = new PreprocessedTrip()
            {
                Id = tripId,
                ActualStartTime = actualStartTime,
                ActualEndTime = actualEndTime,
                DistanceMeters = distance
            };
            await Database.InsertOrReplaceAsync(preprocessed);
            return preprocessed;
        }

        private static bool AreEffectivelyEqual(TripPoint p1, TripPoint p2) =>
            p1?.Latitude == p2?.Latitude && p1?.Longitude == p2.Longitude;

        private static void DeDupe(List<TripPoint> points, List<TripPoint> dupes)
        {
            TripPoint lastPoint = null;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (lastPoint == null)
                    lastPoint = point;
                else
                {
                    if (AreEffectivelyEqual(lastPoint, point))
                    {
                        dupes.Add(point);
                        points.RemoveAt(i);
                        i--;
                    }
                    else
                        lastPoint = point;
                }
            }
        }

        private static int GetTailPointsCount(IEnumerable<TripPoint> points)
        {
            double distance = 0;
            TripPoint lastUsefulPoint = null;
            (double lat, double lon)? lastUsefulPointEdge = null;

            int currentPointIndex = -1;
            using (var enumerator = points.GetEnumerator())
            {
                bool notAtEnd;
                while (notAtEnd = enumerator.MoveNext() && distance < 20)
                {
                    currentPointIndex++;
                    if (lastUsefulPoint == null)
                        lastUsefulPoint = enumerator.Current;
                    else
                    {
                        var point = enumerator.Current;
                        var dist = ShortestDistanceBetweenPoints(lastUsefulPoint, point);
                        if (dist.HasValue)
                        {
                            distance += dist.Value.dist;
                            if (lastUsefulPointEdge != null)
                                distance += GetDistance(lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon,
                                    dist.Value.p1EdgeLat, dist.Value.p1EdgeLon);
                            lastUsefulPoint = point;
                            lastUsefulPointEdge = (lat: dist.Value.p2EdgeLat, lon: dist.Value.p2EdgeLon);
                        }
                    }
                }
                if (!notAtEnd)
                    return currentPointIndex;
                return currentPointIndex - 1;
            }
        }

        private static uint GetTotalLength(IEnumerable<TripPoint> points)
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
                        var dist = ShortestDistanceBetweenPoints(lastUsefulPoint, point);
                        if (dist.HasValue)
                        {
                            distance += dist.Value.dist;
                            if (lastUsefulPointEdge != null)
                                distance += GetDistance(lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon,
                                    dist.Value.p1EdgeLat, dist.Value.p1EdgeLon);
                            lastUsefulPoint = point;
                            lastUsefulPointEdge = (lat: dist.Value.p2EdgeLat, lon: dist.Value.p2EdgeLon);
                        }
                    }
                }
                return (uint)distance;
            }
        }

        private static (double dist, double p1EdgeLat, double p1EdgeLon, double p2EdgeLat, double p2EdgeLon)?
            ShortestDistanceBetweenPoints(TripPoint p1, TripPoint p2)
        {
            var distanceBetweenCenters = GetDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
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

        // Adapted from https://stackoverflow.com/a/51839058
        public static double GetDistance(double latitude, double longitude, double otherLatitude, double otherLongitude)
        {
            var d1 = latitude * (Math.PI / 180.0);
            var num1 = longitude * (Math.PI / 180.0);
            var d2 = otherLatitude * (Math.PI / 180.0);
            var num2 = otherLongitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}