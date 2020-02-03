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

            var pointsFromStart = (from point in Database.TripPoints
                                   where point.TripId == tripId
                                   orderby point.Time ascending
                                   select point).ToAsyncEnumerable();
            var startTailPointsCount = await GetTailPointsCount(pointsFromStart);
            var pointsFromEnd = (from point in Database.TripPoints
                                   where point.TripId == tripId
                                   orderby point.Time descending
                                   select point).ToAsyncEnumerable();
            var endTailPointsCount = await GetTailPointsCount(pointsFromEnd);
            var total = await pointsFromStart.Count();

            var pointsToUpdate = new List<TripPoint>();

            await pointsFromStart.Take(startTailPointsCount).ForEachAsync(async point =>
            {
                point.IsTailPoint = true;
                pointsToUpdate.Add(point);
            });

            await pointsFromEnd.Take(endTailPointsCount).ForEachAsync(async point =>
            {
                point.IsTailPoint = true;
                pointsToUpdate.Add(point);
            });

            foreach (var point in pointsToUpdate)
            {
                await Database.InsertOrReplaceAsync(point);
            }

            pointsToUpdate.Clear();
            var result = new PreprocessedTrip();
            var usefulPoints = pointsFromStart.Skip(startTailPointsCount).Take(total - startTailPointsCount - endTailPointsCount);

            return new PreprocessedTrip();
        }

        private static async Task<int> GetTailPointsCount(IAsyncEnumerable<TripPoint> points)
        {
            double distance = 0;
            TripPoint lastUsefulPoint = null;
            (double lat, double lon)? lastUsefulPointEdge = null;

            int currentPointId = -1;
            var enumerator = points.GetEnumerator();
            while (await enumerator.MoveNext() && (distance < 30 || lastUsefulPoint?.Time == enumerator.Current?.Time))
            {
                currentPointId++;
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
            enumerator.Dispose();
            return currentPointId - 1;
        }

        private static (double dist, double p1EdgeLat, double p1EdgeLon, double p2EdgeLat, double p2EdgeLon)?
            ShortestDistanceBetweenPoints(TripPoint p1, TripPoint p2)
        {
            var distanceBetweenCenters = GetDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
            var distanceBetweenEdges = distanceBetweenCenters - p1.RangeRadius - p2.RangeRadius;
            if (distanceBetweenEdges < 0)
                return null;

            var p1RadiusFactor = distanceBetweenCenters / p1.RangeRadius;
            var p2RadiusFactor = distanceBetweenCenters / p2.RangeRadius;
            var p1EdgeLat = p1.Latitude + (p2.Latitude - p1.Latitude) * p1RadiusFactor;
            var p1EdgeLon = p1.Longitude + (p2.Longitude - p1.Longitude) * p1RadiusFactor;
            var p2EdgeLat = p2.Latitude + (p1.Latitude - p2.Latitude) * p1RadiusFactor;
            var p2EdgeLon = p2.Longitude + (p1.Longitude - p2.Longitude) * p1RadiusFactor;
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
