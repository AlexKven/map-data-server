using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Plugin.Geolocator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TripRecorder2.Models;
using Xamarin.Forms;

namespace TripRecorder2.Services
{
    public class LocationTracker
    {
        private ILocationProvider LocationProvider { get; }
        private HttpClient HttpClient { get; }

        private string server = "https://mapdataserver.azurewebsites.net";
        private string apiKey = "HARM TO ONGOING MATTER";

        private Trip CurrentTrip { get; set; }

        public LocationTracker(ILocationProvider locationProvider)
        {
            LocationProvider = locationProvider;
            HttpClient = new HttpClient();
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                SendMessage("Starting new trip...");
                await LocationProvider.CheckPermission();
                var locator = CrossGeolocator.Current;

                var trip = new Trip() { HovStatus = HovStatus.Sov, VehicleType = "Kia Spectra" };
                CurrentTrip = await PostObject(trip, "trip/start");

                for (long i = 0; i < long.MaxValue; i++)
                {
                    token.ThrowIfCancellationRequested();

                    SendMessage($"Getting location...");

                    locator.DesiredAccuracy = 100;
                    var position = await locator.GetPositionAsync(token: token);

                    SendMessage($"{position.Latitude}, {position.Longitude} ({position.Accuracy})");

                    var point = new TripPoint()
                    {
                        Longitude = position.Longitude,
                        Latitude = position.Latitude,
                        RangeRadius = position.Accuracy,
                        Time = DateTime.Now,
                        TripId = CurrentTrip.Id
                    };
                    await PostObject(point, "trip/point");

                    await Task.Delay(20000, token);
                }
            }, token);
        }

        private void SendMessage(string message)
        {
            var tickedMessage = new TickedMessage
            {
                Message = message
            };

            Device.BeginInvokeOnMainThread(() =>
            {
                MessagingCenter.Send<TickedMessage>(tickedMessage, "TickedMessage");
            });
        }

        private async Task<T> PostObject<T>(T obj, string endpoint)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{server}/{endpoint}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
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
    }
}
