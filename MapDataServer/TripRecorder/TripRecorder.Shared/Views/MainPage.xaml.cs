using MapDataServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TripRecorder.ViewModels;
using Autofac;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TripRecorder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private MainPageViewModel ViewModel { get; }

        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = Startup.Container.Resolve<MainPageViewModel>();
            this.DataContext = this;

            HttpClient = new HttpClient();
        }

#if WINDOWS_UWP || __WASM__
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#endif
        private HttpClient HttpClient { get; }

        private Trip _CurrentTrip;
        private Trip CurrentTrip
        {
            get => _CurrentTrip;
            set
            {
                _CurrentTrip = value;
                CurrentMessage = CurrentTrip == null ? "No trip in progress" : $"Trip ID: {CurrentTrip.Id}";
            }
        }

        private string _CurrentMessage = "No trip in progress";
        public string CurrentMessage
        {
            get => _CurrentMessage;
            set
            {
                _CurrentMessage = value;
                RaisePropertyChanged(nameof(CurrentMessage));
            }
        }

        public async Task LocationTask(CancellationToken cancellationToken)
        {
            await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 100;

            while (!cancellationToken.IsCancellationRequested)
            {
                var location = await geolocator.GetGeopositionAsync(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1));
                CurrentMessage = $"{location.Coordinate.Longitude}, {location.Coordinate.Latitude}, {location.Coordinate.Accuracy}";
                await Task.Delay(1000);
            }
            CurrentMessage = "Stopped";
        }

        private CancellationTokenSource TokenSource = null;

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //CurrentMessage = "Starting new trip...";
            //var trip = new Trip() { HovStatus = HovStatus.Sov, VehicleType = "Kia Spectra" };
            //var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:59759/trip/start");
            //request.Content = new StringContent(JsonConvert.SerializeObject(trip));
            //request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            //var response = await HttpClient.SendAsync(request);
            //var str = await response.Content.ReadAsStringAsync();
            //CurrentTrip = JsonConvert.DeserializeObject<Trip>(str);
            TokenSource?.Cancel();
            TokenSource = new CancellationTokenSource();
            await LocationTask(TokenSource.Token);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            TokenSource?.Cancel();
        }
    }
}
