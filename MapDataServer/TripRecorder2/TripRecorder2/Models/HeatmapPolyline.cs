using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2.Models
{
    public class HeatmapPolyline
    {
        public HeatmapPolyline()
        {
            Segments.CollectionChanged += (s, e) => OnChanged();
            GradientLevels.CollectionChanged += (s, e) => OnChanged();
        }

        public ObservableRangeCollection<HeatmapPolylineSegment> Segments { get; }
            = new ObservableRangeCollection<HeatmapPolylineSegment>();
        public ObservableRangeCollection<HeatmapPolylineGradientLevel> GradientLevels { get; }
            = new ObservableRangeCollection<HeatmapPolylineGradientLevel>();

        private void OnChanged() => Changed?.Invoke(this, new EventArgs());

        public event EventHandler Changed;
    }
}
