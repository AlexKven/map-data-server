using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TripRecorder.Droid.Intents;
using Windows.Devices.Geolocation;

namespace TripRecorder.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private IConfiguration Configuration { get; }
        
        private ILocationIntentService LocationIntentService { get; }

        private CancellationTokenSource TokenSource { get; set; } = null;
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
                OnPropertyChanged(nameof(CurrentMessage));
            }
        }

        public MainPageViewModel(IConfiguration configuration)
        {
            Configuration = configuration;
            LocationIntentService = null;
            HttpClient = new HttpClient();
        }

        private async Task<T> PostObject<T>(T obj, string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Configuration["server"]}/{endpoint}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Configuration["apikey"]);
            request.Content = new StringContent(JsonConvert.SerializeObject(obj));
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            try
            {
                var response = await HttpClient.SendAsync(request);
                var str = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(str);
            }
            catch (Exception ex)
            {
                return default(T);
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
                var point = new TripPoint()
                {
                    Longitude = location.Coordinate.Longitude,
                    Latitude = location.Coordinate.Latitude,
                    RangeRadius = location.Coordinate.Accuracy,
                    Time = DateTime.Now,
                    TripId = CurrentTrip.Id
                };
                await PostObject(point, "trip/point");
                await Task.Delay(10000);
            }
            CurrentMessage = "Stopped";
        }

        public async Task StartTracking()
        {
            CurrentMessage = "Starting new trip...";
            var trip = new Trip() { HovStatus = HovStatus.Sov, VehicleType = "Kia Spectra" };
            CurrentTrip = await PostObject(trip, "trip/start");
            TokenSource?.Cancel();
            TokenSource = new CancellationTokenSource();
            await LocationTask(TokenSource.Token);
        }

        public void StopTracking()
        {
            TokenSource?.Cancel();
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
