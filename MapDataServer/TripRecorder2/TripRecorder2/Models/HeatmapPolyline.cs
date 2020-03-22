using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace TripRecorder2.Models
{
    public class HeatmapPolyline
    {
        public HeatmapPolyline()
        {
            Segments.CollectionChanged += (s, e) => OnChanged();
            GradientLevels.CollectionChanged += (s, e) => OnChanged();
        }

        private double _StrokeWidth = 1;
        public double StrokeWidth
        {
            get => _StrokeWidth;
            set
            {
                _StrokeWidth = value;
                OnChanged();
            }
        }

        public ObservableRangeCollection<HeatmapPolylineSegment> Segments { get; }
            = new ObservableRangeCollection<HeatmapPolylineSegment>();
        public ObservableRangeCollection<HeatmapPolylineGradientLevel> GradientLevels { get; }
            = new ObservableRangeCollection<HeatmapPolylineGradientLevel>();

        public IEnumerable<HeatmapPolylineGradientLevel> GetRelativeGradientLevels(double start, double end)
        {
            var validLevels = new List<HeatmapPolylineGradientLevel>();
            foreach (var level in GradientLevels)
            {
                if (!validLevels.Any())
                    validLevels.Add(level);
                else
                {
                    var prev = validLevels[validLevels.Count - 1];
                    if (level.Threshold > prev.Threshold)
                        validLevels.Add(level);
                    else if (level.Threshold == prev.Threshold)
                    {
                        if (validLevels.Count - 2 >= 0)
                        {
                            var prevPrev = validLevels[validLevels.Count - 2];
                            if (prevPrev.Threshold < prev.Threshold)
                                validLevels.Add(level);
                        }
                        else
                            validLevels.Add(level);
                    }
                }
            }

            if (start > end)
            {
                var tmp = start;
                start = end;
                end = tmp;
                validLevels.Reverse();
                for (int i = 0; i < validLevels.Count; i++)
                {
                    var diff = start - validLevels[i].Threshold;
                    validLevels[i] = new HeatmapPolylineGradientLevel(
                        end + diff, validLevels[i].Color);
                }
            }

            if (end == start)
            {
                HeatmapPolylineGradientLevel? prev = null;
                for (int i = 0; i < validLevels.Count; i++)
                {
                    var current = validLevels[i];
                    if (current.Threshold == start)
                    {
                        yield return new HeatmapPolylineGradientLevel(0, current.Color);
                        yield return new HeatmapPolylineGradientLevel(1, current.Color);
                        yield break;
                    }
                    else if (current.Threshold > start)
                    {
                        if (prev.HasValue)
                        {
                            var diff = current.Threshold - prev.Value.Threshold;
                            var factor = (start - prev.Value.Threshold) / diff;
                            var color = MoveColorToward(prev.Value.Color, current.Color, factor);
                            yield return new HeatmapPolylineGradientLevel(0, color);
                            yield return new HeatmapPolylineGradientLevel(1, color);
                            yield break;
                        }
                        else
                        {
                            yield return new HeatmapPolylineGradientLevel(0, current.Color);
                            yield return new HeatmapPolylineGradientLevel(1, current.Color);
                            yield break;
                        }
                    }
                    prev = current;
                }
                if (prev.HasValue)
                {
                    yield return new HeatmapPolylineGradientLevel(0, prev.Value.Color);
                    yield return new HeatmapPolylineGradientLevel(1, prev.Value.Color);
                    yield break;
                }
            }
            else
            {
                for (int i = 0; i < validLevels.Count; i++)
                {
                    if (validLevels[i].Threshold < start)
                    {
                        if (i < validLevels.Count - 1 && validLevels[i + 1].Threshold > start)
                        {
                            var diff = validLevels[i + 1].Threshold - validLevels[i].Threshold;
                            var factor = (start - validLevels[i].Threshold) / diff;
                            yield return new HeatmapPolylineGradientLevel(0,
                                MoveColorToward(validLevels[i].Color, validLevels[i + 1].Color, factor));
                        }
                    }
                    else if (validLevels[i].Threshold > end)
                    {
                        if (i > 0 && validLevels[i - 1].Threshold < end)
                        {
                            var diff = validLevels[i].Threshold - validLevels[i - 1].Threshold;
                            var factor = (end - validLevels[i - 1].Threshold) / diff;
                            yield return new HeatmapPolylineGradientLevel(1,
                                MoveColorToward(validLevels[i - 1].Color, validLevels[i].Color, factor));
                        }
                    }
                    else
                    {
                        yield return new HeatmapPolylineGradientLevel(
                            (validLevels[i].Threshold - start) / (end - start),
                            validLevels[i].Color);
                    }
                }
            }
        }

        public static Color MoveColorToward(Color first, Color second, double percentage)
        {
            Func<double, double, double, double> moveValueToward =
                (f, s, p) => s + (f - s) * p;
            if (percentage < 0)
                percentage = 0;
            if (percentage > 1)
                percentage = 1;
            return new Color(
                moveValueToward(first.R, second.R, percentage),
                moveValueToward(first.G, second.G, percentage),
                moveValueToward(first.B, second.B, percentage),
                moveValueToward(first.A, second.A, percentage));
        }

        private void OnChanged() => Changed?.Invoke(this, new EventArgs());

        public event EventHandler Changed;
    }
}
