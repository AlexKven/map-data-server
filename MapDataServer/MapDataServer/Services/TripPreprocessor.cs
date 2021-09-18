using LinqToDB;
using MapDataServer.Helpers;
using MapDataServer.Models;
using MapDataServer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class TripPreprocessor : ITripPreprocessor
    {
        private IDatabase Database { get; }
        private ObaRepository ObaRepository { get; }
        public TripPreprocessor(IDatabase database, ObaRepository obaRepository)
        {
            Database = database;
            ObaRepository = obaRepository;
        }

        public async Task<PreprocessedTrip> PreprocessTrip(long tripId, CancellationToken cancellationToken)
        {
            await Database.Initializer;

            var duplicates = new List<TripPoint>();
            var points = await (from point in Database.TripPoints
                                where point.TripId == tripId
                                orderby point.Time ascending
                                select point).ToListAsync(cancellationToken);
            var trip = await (from t in Database.Trips
                              where t.Id == tripId
                              select t).FirstOrDefaultAsync();

            DeDupe(points, duplicates);

            foreach (var point in duplicates)
            {
                await Database.DeleteAsync(point, token: cancellationToken);
            }
            duplicates.Clear();

            StringBuilder builder = new StringBuilder();
            foreach (var point in points)
                builder.AppendLine($"{point.Latitude},{point.Longitude}");
            var res = builder.ToString();

            var startThreshold = 20;
            var endThreshold = 20;
            if (trip != null)
            {
                switch (trip.HovStatus)
                {
                    case HovStatus.Bicycle:
                        startThreshold = 5;
                        endThreshold = 10;
                        break;
                    case HovStatus.HeavyRail:
                        startThreshold = 20;
                        endThreshold = 25;
                        break;
                    case HovStatus.Hov2:
                        startThreshold = 5;
                        endThreshold = 15;
                        break;
                    case HovStatus.Hov3:
                        startThreshold = 5;
                        endThreshold = 15;
                        break;
                    case HovStatus.Sov:
                        startThreshold = 5;
                        endThreshold = 15;
                        break;
                    case HovStatus.LightRail:
                        startThreshold = 5;
                        endThreshold = 20;
                        break;
                    case HovStatus.Motorcycle:
                        startThreshold = 5;
                        endThreshold = 10;
                        break;
                    case HovStatus.Pedestrian:
                        startThreshold = 2;
                        endThreshold = 5;
                        break;
                    case HovStatus.Streetcar:
                        startThreshold = 5;
                        endThreshold = 20;
                        break;
                    case HovStatus.Transit:
                        startThreshold = 5;
                        endThreshold = 20;
                        break;
                }
            }

            var startTailPointsCount = GetTailPointsCount(points, startThreshold);
            var pointsFromEnd = points.Reverse<TripPoint>();
            var endTailPointsCount = GetTailPointsCount(pointsFromEnd, endThreshold);
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
                await Database.InsertOrReplaceAsync(point, token: cancellationToken);
            }
            pointsToUpdate.Clear();

            var result = new PreprocessedTrip();
            var usefulPoints = points.Skip(startTailPointsCount).Take(total - startTailPointsCount - endTailPointsCount).ToList();
            for (int i = 0; i < usefulPoints.Count; i++)
            {
                TripPoint prev = null;
                TripPoint next = null;
                var pt = usefulPoints[i];
                if (i > 0)
                    prev = usefulPoints[i - 1];
                if (i < usefulPoints.Count - 1)
                    next = usefulPoints[i + 1];

                var isBad = false;
                if (prev != null && next != null)
                    isBad = IsBadPoint(pt, prev, next);
                if (isBad)
                {
                    usefulPoints.RemoveAt(i);
                    i--;
                }
                pt.IsTailPoint = isBad;
                pointsToUpdate.Add(pt);
            }

            var distance = GeometryHelpers.GetTotalLength(usefulPoints);

            foreach (var point in pointsToUpdate)
            {
                await Database.InsertOrReplaceAsync(point, token: cancellationToken);
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
                actualStartTime = actualEndTime = (await Database.Trips.FirstAsync(t => t.Id == tripId, cancellationToken)).StartTime;


            var obaTripLink = await (from link in Database.ObaTripLinks
                                     where link.MapTripId == tripId
                                     select link).FirstOrDefaultAsync();

            if (obaTripLink != null && usefulPoints.Count > 0)
            {
                var calculator = new DelayCalculator(ObaRepository, obaTripLink.ObaTripId, actualStartTime);
                if (await calculator.Initialize())
                {
                    foreach (var point in usefulPoints)
                    {
                        var obaPoint = calculator.CreateObaTripPointLink(point);
                        if (obaPoint != null)
                            await Database.InsertOrReplaceAsync(obaPoint);
                    }
                }
            }

            var preprocessed = new PreprocessedTrip()
            {
                Id = tripId,
                ActualStartTime = actualStartTime,
                ActualEndTime = actualEndTime,
                DistanceMeters = distance
            };
            if (usefulPoints.Count > 0)
            {
                var startLat = usefulPoints[0].Latitude;
                var startLon = usefulPoints[0].Longitude;
                var endLat = usefulPoints[usefulPoints.Count - 1].Latitude;
                var endLon = usefulPoints[usefulPoints.Count - 1].Longitude;
                preprocessed.StartLatitude = startLat;
                preprocessed.StartLongitude = startLon;
                preprocessed.StartRegion = MapRegion.GetRegionContaining(startLat, startLon);
                preprocessed.EndLatitude = endLat;
                preprocessed.EndLongitude = endLon;
                preprocessed.EndRegion = MapRegion.GetRegionContaining(endLat, endLon);
            }
            await Database.InsertOrReplaceAsync(preprocessed, token: cancellationToken);

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

        private static int GetTailPointsCount(IEnumerable<TripPoint> points, double distThreshold)
        {
            double distance = 0;
            TripPoint lastUsefulPoint = null;
            (double lat, double lon)? lastUsefulPointEdge = null;

            int currentPointIndex = -1;
            using (var enumerator = points.GetEnumerator())
            {
                bool notAtEnd;
                while (notAtEnd = enumerator.MoveNext() && distance < distThreshold)
                {
                    currentPointIndex++;
                    if (lastUsefulPoint == null)
                        lastUsefulPoint = enumerator.Current;
                    else
                    {
                        var point = enumerator.Current;

                        // Tunnel mode points are exact
                        if (point.FromTunnelMode)
                            return currentPointIndex;
                        var dist = GeometryHelpers.ShortestDistanceBetweenTripPoints(lastUsefulPoint, point);
                        if (dist.HasValue)
                        {
                            distance += dist.Value.dist;
                            if (lastUsefulPointEdge != null)
                                distance += GeometryHelpers.GetDistance(lastUsefulPointEdge.Value.lat, lastUsefulPointEdge.Value.lon,
                                    dist.Value.p1EdgeLat, dist.Value.p1EdgeLon);

                            // For if the first point is a bad point
                            if (currentPointIndex == 1 && distance > 1500)
                                distance = 0;

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

        private static bool IsBadPoint(TripPoint current, TripPoint prev, TripPoint next)
        {
            // It is a bad point if
            // angle is < 90, distance > 2km, and speed > 80 m/s 
            // angle is < 45, distance > 1km, and speed > 40 m/s

            var sideA = GeometryHelpers.GetDistance(next.Latitude, next.Longitude, prev.Latitude, prev.Longitude);
            var sideB = GeometryHelpers.GetDistance(prev.Latitude, prev.Longitude, current.Latitude, current.Longitude);
            var sideC = GeometryHelpers.GetDistance(next.Latitude, next.Longitude, current.Latitude, current.Longitude);

            var angle = GeometryHelpers.GetTriangleAngleA(sideA, sideB, sideC);
            if (!angle.HasValue)
                return false;

            var totalDistance = sideB + sideC;
            var totalTime = next.Time - prev.Time;
            var speed = totalDistance / totalTime.TotalSeconds;

            if (angle.Value < Math.PI / 2 && totalDistance > 2000 && speed > 80)
                return true;
            if (angle.Value < Math.PI / 4 && totalDistance > 1000 && speed > 40)
                return true;
            return false;
        }
    }
}