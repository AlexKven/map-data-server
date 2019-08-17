using MapDataServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2.Services
{
    public class TripPagePointsListService
    {
        private ConcurrentQueue<TripPoint> _TripPagePoints = new ConcurrentQueue<TripPoint>();
        public ConcurrentQueue<TripPoint> TripPagePoints => _TripPagePoints;

        public void Reset()
        {
            while (TripPagePoints.TryDequeue(out _)) ;
        }
    }
}
