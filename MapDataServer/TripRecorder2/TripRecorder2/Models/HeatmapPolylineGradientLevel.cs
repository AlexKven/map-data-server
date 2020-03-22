using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TripRecorder2.Models
{
    public struct HeatmapPolylineGradientLevel
    {
        public HeatmapPolylineGradientLevel(
            double threshold, Color color)
        {
            Threshold = threshold;
            Color = color;
        }

        public double Threshold { get; }
        public Color Color { get; }
    }
}
