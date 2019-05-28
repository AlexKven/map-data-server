using MapDataServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public class FullTrip
    {
        public long TripId { get; set; }

        public HovStatus HovStatus { get; set; }

        public string VehicleType { get; set; }
        
        public string BusRoute { get; set; }

        public List<TripPoint> Points { get; } = new List<TripPoint>();

        public DateTime? StartTime => Points.FirstOrDefault()?.Time;

        public DateTime? EndTime => Points.LastOrDefault()?.Time;

        public TimeSpan? Duration => EndTime - StartTime;

        public double Length
        {
            get
            {
                double result = 0;
                GeoPoint? previous = null;
                foreach (var point in Points)
                {
                    result += point.GetPoint().DistanceTo(previous);
                    previous = point.GetPoint();
                }
                return result;
            }
        }
    }
}
