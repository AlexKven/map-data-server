using Autofac;
using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    }
}