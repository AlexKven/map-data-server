using MapDataServer.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public class FullTrip : IEnumerable<FullTripStep>
    {
        public class FullTripEnumerator : IEnumerator<FullTripStep>
        {
            public FullTripEnumerator(FullTrip trip) => Trip = trip;

            private FullTrip Trip { get; }

            private int CurrentIndex { get; set; } = -1;

            public FullTripStep Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                TripPoint previous = null;
                if (CurrentIndex == -1)
                {
                    CurrentIndex++;
                }
                else
                {
                    previous = Current.CurrentPoint;
                    if (Current.IsDropped)
                    {
                        Trip.Points.RemoveAt(CurrentIndex);
                        Trip.DroppedPoints.Add(Current.CurrentPoint);
                        previous = Current.PreviousPoint;
                    }
                    else
                        CurrentIndex++;
                }

                if (CurrentIndex < Trip.Points.Count)
                {
                    Current = new FullTripStep(Trip.Points[CurrentIndex], previous);
                    return true;
                }
                Current = null;
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        public long TripId { get; set; }

        public HovStatus HovStatus { get; set; }

        public string VehicleType { get; set; }
        
        public string BusRoute { get; set; }

        public List<TripPoint> Points { get; } = new List<TripPoint>();

        public List<TripPoint> DroppedPoints { get; } = new List<TripPoint>();

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

        public IEnumerator<FullTripStep> GetEnumerator()
        {
            return new FullTripEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
