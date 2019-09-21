using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
        private ConcurrentQueue<TripPoint> PendingPoints { get; } = new ConcurrentQueue<TripPoint>();
        private IConfiguration Config { get; }
        private TripPagePointsListService PointsListService { get; }
        private TripSettingsProvider TripSettingsProvider { get; }

        private Trip CurrentTrip { get; set; }

        public LocationTracker(TripPagePointsListService pointsListService, ILocationProvider locationProvider, TripSettingsProvider tripSettingsProvider, IConfiguration config)
        {
            LocationProvider = locationProvider;
            Config = config;
            HttpClient = new HttpClient();
            PointsListService = pointsListService;
            TripSettingsProvider = tripSettingsProvider;
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                SendMessage("Starting new trip...");

                var trip = new Trip()
                {
                    HovStatus = TripSettingsProvider.HovStatus,
                    VehicleType = TripSettingsProvider.VehicleType,
                    StartTime = DateTime.UtcNow
                };
                do
                {
                    CurrentTrip = await PostObject(trip, "trip/start");
                } while (CurrentTrip == null);

                if (TripSettingsProvider.ObaTripId != null)
                {
                    int attempt = 0;
                    while (!(await SetObaDetails()) && attempt++ < 10) ;
                }

                await LocationProvider.CheckPermission();
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 10;

                await locator.StartListeningAsync(TimeSpan.FromSeconds(15), 15, true,
                    new ListenerSettings()
                    {
                        ActivityType = ActivityType.AutomotiveNavigation,
                        AllowBackgroundUpdates = true,
                        DeferLocationUpdates = false,
                        ListenForSignificantChanges = false,
                        PauseLocationUpdatesAutomatically = false
                    });
                locator.PositionChanged += Locator_PositionChanged;
                locator.PositionError += Locator_PositionError;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(60000, token);
                        await SendPendingPoints();
                    }
                }
                finally
                {
                    await locator.StopListeningAsync();
                    await SendPendingPoints();
                    await EndTrip();
                }
            }, token);
        }

        private void Locator_PositionError(object sender, PositionErrorEventArgs e)
        {
        }

        private void Locator_PositionChanged(object sender, PositionEventArgs e)
        {
            var position = e.Position;

            var point = new TripPoint()
            {
                Longitude = position.Longitude,
                Latitude = position.Latitude,
                RangeRadius = position.Accuracy,
                Time = DateTime.Now,
                TripId = CurrentTrip?.Id ?? 0
            };
            PendingPoints.Enqueue(point);
            PointsListService.TripPagePoints.Enqueue(point);

            SendMessage($"Point: {position.Latitude}, {position.Longitude} ({position.Accuracy}, {DateTime.Now.ToString("HH:mm:ss")})");

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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Config["server"]}/{endpoint}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config["apikey"]);
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
                SendMessage($"Error: {ex.Message}");
                return default(T);
            }
        }

        private async Task SendPendingPoints()
        {
            List<TripPoint> points = new List<TripPoint>();
            while (PendingPoints.TryDequeue(out var point))
            {
                points.Add(point);
            }
            await PostObject(points.ToArray(), "trip/points");
        }

        private async Task EndTrip()
        {
            if (CurrentTrip == null)
                return;
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Config["server"]}/trip/end?tripId={CurrentTrip.Id}&endTime={DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config["apikey"]);

            try
            {
                await HttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                SendMessage($"Error: {ex.Message}");
            }
        }

        private async Task<bool> SetObaDetails()
        {
            if (CurrentTrip == null)
                return true;
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Config["server"]}/trip/setObaTrip?tripId={CurrentTrip.Id}&obaTripId={TripSettingsProvider.ObaTripId}&obaVehicleId={TripSettingsProvider.ObaVehicleId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config["apikey"]);

            try
            {
                await HttpClient.SendAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                SendMessage($"Error: {ex.Message}");
                return false;
            }
        }
    }
}