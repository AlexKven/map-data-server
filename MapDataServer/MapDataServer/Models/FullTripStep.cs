using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public class FullTripStep
    {
        public FullTripStep(TripPoint currentPoint, TripPoint previousPoint)
        {
            CurrentPoint = currentPoint;
            PreviousPoint = previousPoint;
        }

        public TripPoint CurrentPoint { get; }
        public TripPoint PreviousPoint { get; }
        public bool IsDropped { get; private set; } = false;

        public void Drop() => IsDropped = true;
    }
}
