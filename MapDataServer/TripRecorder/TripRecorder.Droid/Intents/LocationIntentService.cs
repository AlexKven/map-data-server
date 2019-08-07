using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TripRecorder.Droid.Services;
using Windows.Devices.Geolocation;

namespace TripRecorder.Droid.Intents
{
    public class LocationIntentService : Service
    {
        private ILocationProvider LocationProvider { get; }

        private Trip CurrentTrip { get; set; }

        private HttpClient HttpClient { get; } = new HttpClient();

        private IConfiguration Configuration { get; }

        private CancellationTokenSource TokenSource { get; set; }

        public LocationIntentService(ILocationProvider locationProvider)
        {
            LocationProvider = locationProvider;
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
                //CurrentMessage = $"{location.Coordinate.Longitude}, {location.Coordinate.Latitude}, {location.Coordinate.Accuracy}";
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
            //CurrentMessage = "Stopped";
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            TokenSource = new CancellationTokenSource();

            Task.Run(() => {
                try
                {
                    //INVOKE THE SHARED CODE
                    LocationTask(TokenSource.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    if (TokenSource.IsCancellationRequested)
                    {
                        var message = new CancelledMessage();
                        
                        Device.BeginInvokeOnMainThread(
                            () => MessagingCenter.Send(message, "CancelledMessage")
                        );
                    }
                }

            }, _cts.Token);

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Token.ThrowIfCancellationRequested();

                _cts.Cancel();
            }
            base.OnDestroy();
        }
    }
}