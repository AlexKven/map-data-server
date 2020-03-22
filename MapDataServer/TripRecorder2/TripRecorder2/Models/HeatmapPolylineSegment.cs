using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms.GoogleMaps;

namespace TripRecorder2.Models
{
    public struct HeatmapPolylineSegment
    {
        public HeatmapPolylineSegment(
            Position startPosition, Position endPosition,
            double startValue, double endValue)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            StartValue = startValue;
            EndValue = endValue;
        }

        public Position StartPosition { get; }
        public Position EndPosition { get; }
        public double StartValue { get; }
        public double EndValue { get; }
    }
}
