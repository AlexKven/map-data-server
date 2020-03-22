using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TripRecorder2.Models;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace TripRecorder2.Views
{
    public class BindableMap : Map
    {
        private TileLayer HeatmapPolylineTileLayer { get; }
        const int TILE_SIZE = 512;
        public BindableMap()
        {
            this.CameraIdled += BindableMap_CameraIdled;
            HeatmapPolylineTileLayer = TileLayer.FromSyncImage(GetHeatmapPolylineTileImage, TILE_SIZE);
        }

        private bool IsUpdatingCenter = false;
        private void BindableMap_CameraIdled(object sender, CameraIdledEventArgs e)
        {
            if (IsUpdatingCenter)
                return;
            try
            {
                IsUpdatingCenter = true;
                Center = e.Position.Target;
            }
            finally
            {
                IsUpdatingCenter = false;
            }
        }

        public static readonly BindableProperty CenterProperty = BindableProperty.Create(
            nameof(Center), typeof(Position), typeof(BindableMap), new Position(),
            propertyChanged: (b, o, n) => ((BindableMap)b).OnCenterChanged((Position)n));
        public Position Center
        {
            get => (Position)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        private void OnCenterChanged(Position newPosition)
        {
            if (IsUpdatingCenter)
                return;
            try
            {
                IsUpdatingCenter = true;
                MoveCamera(CameraUpdateFactory.NewPosition(newPosition));
            }
            finally
            {
                IsUpdatingCenter = false;
            }
        }

        public static readonly BindableProperty CirclesSourceProperty = BindableProperty.Create(
            nameof(CirclesSource), typeof(IEnumerable<Circle>), typeof(BindableMap), null,
            propertyChanged: (b, o, n) =>
            ((BindableMap)b).OnCirclesSourceChanged(
            (IEnumerable<Circle>)o, (IEnumerable<Circle>)n));
        public IEnumerable<Circle> CirclesSource
        {
            get => (IEnumerable<Circle>)GetValue(CirclesSourceProperty);
            set => SetValue(CirclesSourceProperty, value);
        }

        private void OnCirclesSourceChanged(
            IEnumerable<Circle> oldItemsSource, IEnumerable<Circle> newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged -= OnCirclesSourceCollectionChanged;
            }

            if (newItemsSource is INotifyCollectionChanged ncc1)
            {
                ncc1.CollectionChanged += OnCirclesSourceCollectionChanged;
            }


        }

        private void OnCirclesSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.NewItems)
                        Circles.Add(item);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    // Not tracking order
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        Circles.Remove(item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1)
                        goto case NotifyCollectionChangedAction.Reset;
                    foreach (Circle item in e.OldItems)
                        Circles.Remove(item);
                    foreach (Circle item in e.NewItems)
                        Circles.Add(item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Circles.Clear();
                    break;
            }
        }

        public static readonly BindableProperty HeatmapPolylineProperty = BindableProperty.Create(
            nameof(HeatmapPolyline), typeof(HeatmapPolyline), typeof(BindableMap),
            propertyChanged: (b, o, n) => ((BindableMap)b).OnHeatmapPolylineChanged((HeatmapPolyline)o, (HeatmapPolyline)n));
        public HeatmapPolyline HeatmapPolyline
        {
            get => (HeatmapPolyline)GetValue(HeatmapPolylineProperty);
            set => SetValue(HeatmapPolylineProperty, value);
        }
        private void OnHeatmapPolylineChanged(
            HeatmapPolyline oldPolyline, HeatmapPolyline newPolyline)
        {
            if (oldPolyline != null)
                oldPolyline.Changed += HeatmapPolyline_Changed;
            if (newPolyline != null)
                newPolyline.Changed += HeatmapPolyline_Changed;
            OnHeatmapPolylineChanged();
        }

        private void HeatmapPolyline_Changed(object sender, EventArgs e)
        {
            OnHeatmapPolylineChanged();
        }

        private CancellationTokenSource HeatmapPolylineChangedTokenSource { get; set; }
        private async void OnHeatmapPolylineChanged()
        {
            try
            {
                HeatmapPolylineChangedTokenSource?.Cancel();
                HeatmapPolylineChangedTokenSource = new CancellationTokenSource();
                var token = HeatmapPolylineChangedTokenSource.Token;
                await Task.Delay(100);
                if (token.IsCancellationRequested)
                    return;
                RefreshHeatmapPolyline();
            }
            catch (Exception) { }
        }

        private void RefreshHeatmapPolyline()
        {
            TileLayers.Remove(HeatmapPolylineTileLayer);
            TileLayers.Add(HeatmapPolylineTileLayer);
        }

        private byte[] GetHeatmapPolylineTileImage(int x, int y, int zoom)
        {
            var bitmap = new SkiaSharp.SKBitmap(TILE_SIZE, TILE_SIZE);
            var canvas = new SkiaSharp.SKCanvas(bitmap);
            canvas.Clear();

            var segments = HeatmapPolyline?.Segments;
            if (segments != null)
            {
                foreach (var segment in segments)
                {
                    var pStart = LatLonToSquareCoords(segment.StartPosition.Latitude,
                        segment.StartPosition.Longitude, x, y, zoom);
                    var pEnd = LatLonToSquareCoords(segment.EndPosition.Latitude,
                        segment.EndPosition.Longitude, x, y, zoom);
                    canvas.DrawLine(pStart.Item1, pStart.Item2, pEnd.Item1, pEnd.Item2,
                        new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red, StrokeWidth = 2 });
                }
                canvas.Flush();
            }
            canvas.Dispose();

            using (MemoryStream memStream = new MemoryStream())
            using (SkiaSharp.SKManagedWStream wstream = new SkiaSharp.SKManagedWStream(memStream))
            {
                bitmap.Encode(wstream, SkiaSharp.SKEncodedImageFormat.Png, 100);
                byte[] data = memStream.ToArray();
                bitmap.Dispose();
                return data;
            }
        }

        private static (float, float) LatLonToSquareCoords(double lat, double lon, int x, int y, int zoom)
        {
            // From https://web.archive.org/web/20190603233026/http://troybrant.net/blog/2010/01/mkmapview-and-zoom-levels-a-visual-guide/ 

            var mercatorOffset = Math.Pow(2, zoom - 1);
            var mercatorRadius = mercatorOffset / Math.PI;

            var coordX = mercatorOffset + lon * mercatorOffset / 180;
            var coordY = mercatorOffset - mercatorRadius * Math.Log((1 + Math.Sin(lat * Math.PI / 180)) /
                (1 - Math.Sin(lat * Math.PI / 180))) / 2.0;
            var fracX = coordX - x;
            var fracY = coordY - y;
            return ((float)fracX * TILE_SIZE, (float)fracY * TILE_SIZE);
        }

        private void UpdateCircles()
        {
            Circles.Clear();
            if (CirclesSource?.Any() ?? false)
            {
                foreach (var circle in CirclesSource)
                {
                    Circles.Add(circle);
                }
            }
        }
    }
}
