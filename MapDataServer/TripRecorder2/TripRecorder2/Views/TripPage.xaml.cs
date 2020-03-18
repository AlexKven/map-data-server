using Autofac;
using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripRecorder2.Models;
using TripRecorder2.Services;
using TripRecorder2.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.Xaml;

namespace TripRecorder2.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TripPage : ContentPage
    {
        public TripPage()
        {
            InitializeComponent();
            var viewModel = AppStartup.Container.Resolve<TripPageViewModel>();
            viewModel.PointsUpdated += ViewModel_PointsUpdated;
            BindingContext = viewModel;
            SetLocation();
            MainMap.TileLayers.Add(TileLayer.FromSyncImage(GetTileImage, 1024));
        }

        private async void SetLocation()
        {
            var locationProvider = AppStartup.Container.Resolve<ILocationProvider>();
            if (await locationProvider.CheckPermission())
            {
                var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
                MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Position(position.Latitude, position.Longitude), Distance.FromKilometers(2)), false);
            }
        }

        private void ViewModel_PointsUpdated(object sender, TripPageViewModel.TripPointsEventArgs e)
        {
            MainMap.Pins.Clear();
            int curIndex = 0;
            foreach (var point in e.Points)
            {
                var pin = new Pin()
                {
                    Position = new Xamarin.Forms.GoogleMaps.Position(point.Latitude, point.Longitude),
                    IsVisible = true,
                    Type = PinType.Generic,
                    Label = $"Point #{++curIndex}"
                };
                MainMap.Pins.Add(pin);
            }
        }

        private byte[] GetTileImage(int x, int y, int zoom)
        {
            var bitmap = new SkiaSharp.SKBitmap(1024, 1024);
            var canvas = new SkiaSharp.SKCanvas(bitmap);
            var p1 = LatLonToSquareCoords(47.6153079801, -122.1887000000, x, y, zoom, 1024);
            var p2 = LatLonToSquareCoords(47.6123821584, -122.199, x, y, zoom, 1024);
            canvas.Clear();
            canvas.DrawLine(p1.Item1, p1.Item2, p2.Item1, p2.Item2,
                new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red, StrokeWidth = 2 });
            canvas.Flush();
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

        public (float, float) LatLonToSquareCoords(double lat, double lon, int x, int y, int zoom, int tileSize)
        {
            // From https://web.archive.org/web/20190603233026/http://troybrant.net/blog/2010/01/mkmapview-and-zoom-levels-a-visual-guide/

            var mercatorOffset = Math.Pow(2, zoom - 1);
            var mercatorRadius = mercatorOffset / Math.PI;

            var coordX = mercatorOffset + lon * mercatorOffset / 180;
            var coordY = mercatorOffset - mercatorRadius * Math.Log((1 + Math.Sin(lat * Math.PI / 180)) /
                (1 - Math.Sin(lat * Math.PI / 180))) / 2.0;
            var fracX = coordX - x;
            var fracY = coordY - y;
            return ((float)fracX * tileSize, (float)fracY * tileSize);
        }
    }
}