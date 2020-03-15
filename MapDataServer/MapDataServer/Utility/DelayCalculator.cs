using MapDataServer.Helpers;
using MapDataServer.Models;
using MapDataServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Utility
{
    public class DelayCalculator
    {
        private ObaRepository ObaRepository { get; }
        private string TripId { get; }
        private DateTime Now { get; }
        
        public DelayCalculator(
            ObaRepository obaRepository,
            string tripId,
            DateTime now)
        {
            ObaRepository = obaRepository;
            TripId = tripId;
            Now = now;
        }

        private ObaTripStopLink[] Stops { get; set; }
        private (double latitude, double longitude)[] TripShape { get; set; }
        private string TimeZone { get; set; }
        ObaTrip ObaTrip { get; set; }

        public async Task<bool> Initialize()
        {
            ObaTrip = await ObaRepository.GetTrip(TripId, Now);
            if (ObaTrip == null)
                return false;
            TripShape = GeometryHelpers.DecodePoints(ObaTrip.Shape).ToArray();
            if (TripShape == null)
                return false;
            await ObaRepository.GetRoute(ObaTrip.ObaRouteId, Now);
            Stops = (await ObaRepository.GetStopsForTrip(TripId, Now)).ToArray();
            TimeZone = ObaTrip.TimeZone;
            return true;
        }

        private double DistanceAlongShape(double lat, double lon,
            out double closestLat, out double closestLon)
        {
            var lines = TripShape.PointsToLines();
            double closestLength = double.PositiveInfinity;
            int closestIndex = -1;
            int currentIndex = 0;
            double nextPartial = 0;
            foreach (var line in lines)
            {
                var length = GeometryHelpers.DistanceToLine(
                    (latitude: lat, longitude: lon),
                    line.Item1, line.Item2,
                    out var partial);
                if (length < closestLength)
                {
                    closestIndex = currentIndex;
                    nextPartial = partial;
                    closestLength = length;
                }
                currentIndex++;
            }

            var distance = lines.Take(closestIndex).Sum(p =>
                GeometryHelpers.GetDistance(p.Item1, p.Item2));
            var next = lines.ElementAt(closestIndex);
            distance += GeometryHelpers.GetDistance(next.Item1, next.Item2) * nextPartial;

            closestLat = next.Item1.latitude + nextPartial * (next.Item2.latitude - next.Item1.latitude);
            closestLon = next.Item1.longitude + nextPartial * (next.Item2.longitude - next.Item1.longitude);
            return distance;
        }

        public ObaTripPointLink CreateObaTripPointLink(TripPoint point)
        {
            (double latitude, double longitude) mappedPoint;
            var distanceAlongTrip = DistanceAlongShape(point.Latitude, point.Longitude,
                out mappedPoint.latitude, out mappedPoint.longitude);

            ObaTripStopLink prevStop = null;
            ObaTripStopLink nextStop = null;
            foreach (var stop in Stops)
            {
                prevStop = nextStop;
                nextStop = stop;
                if (stop.DistanceAlongTrip > distanceAlongTrip)
                    break;
            }
            if (prevStop == null || nextStop == null || nextStop.DistanceAlongTrip < distanceAlongTrip)
                return null;

            var distanceFromPrev = distanceAlongTrip - prevStop.DistanceAlongTrip;
            var percentToNextStop = distanceFromPrev / (nextStop.DistanceAlongTrip - prevStop.DistanceAlongTrip);
            var onTimeArrivalSeconds = prevStop.DepartureTime + percentToNextStop * (nextStop.ArrivalTime - prevStop.DepartureTime);

            var localTime = TimeHelpers.GetLocalTimeForUtcTime(TimeZone, point.Time);
            var onTimeArrival = TimeHelpers.GetUtcStartOfDayForTimeZone(
                TimeZone, localTime).AddSeconds(onTimeArrivalSeconds);

            var delaySeconds = (point.Time - onTimeArrival).TotalSeconds;

            return new ObaTripPointLink()
            {
                DelaySeconds = delaySeconds,
                DistanceAlongTrip = distanceAlongTrip,
                Id = point.Id,
                MappedLatitude = mappedPoint.latitude,
                MappedLongitude = mappedPoint.longitude,
                ObaTripId = ObaTrip.ObaTripId,
                Time = point.Time
            };
        }
    }
}
