﻿using MapDataServer.Models;
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
using Xamarin.Essentials;
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

        private object SettingsLock = new object();
        private bool _IsInTunnelMode = false;
        public bool IsInTunnelMode
        {
            get
            {
                lock (SettingsLock)
                {
                    return _IsInTunnelMode;
                }
            }
            set
            {
                lock (SettingsLock)
                {
                    _IsInTunnelMode = value;
                }
            }
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                SendMessage("Starting new trip...");

                if (TripSettingsProvider.ResumingTripId.HasValue)
                {
                    Console.WriteLine($"Resuming trip {TripSettingsProvider.ResumingTripId}");
                    CurrentTrip = new Trip()
                    {
                        Id = TripSettingsProvider.ResumingTripId.Value,
                        HovStatus = TripSettingsProvider.HovStatus,
                        VehicleType = TripSettingsProvider.VehicleType,
                        StartTime = DateTime.UtcNow
                    };
                }
                else
                {
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


                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Preferences.Set("currentTrip:inProgress", true);
                        var (a, b) = LongHelpers.ToInts(CurrentTrip.Id);
                        Preferences.Set("currentTrip:tripIdA", a);
                        Preferences.Set("currentTrip:tripIdB", b);
                        Preferences.Set("currentTrip:vehicleType", CurrentTrip.VehicleType);
                        Preferences.Set("currentTrip:hovStatus", (int)CurrentTrip.HovStatus);
                        Preferences.Set("currentTrip:tripId", TripSettingsProvider.ObaTripId);
                        Preferences.Set("currentTrip:vehicleId", TripSettingsProvider.ObaVehicleId);
                        Console.WriteLine($"New trip: {CurrentTrip.Id}, {Preferences.Get("currentTrip:tripIdA", 0)}, {Preferences.Get("currentTrip:tripIdB", 0)}");
                    });

                    if (TripSettingsProvider.ObaTripId != null)
                    {
                        int attempt = 0;
                        while (!(await SetObaDetails()) && attempt++ < 10) ;
                    }
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

        public void ManuallyPostPoint(TripPoint point)
        {
            point.Time = DateTime.Now;
            point.TripId = CurrentTrip?.Id ?? 0;

            PendingPoints.Enqueue(point);
            PointsListService.TripPagePoints.Enqueue(point);

            SendMessage($"Point: {point.Latitude}, {point.Longitude} ({point.RangeRadius}, {DateTime.Now.ToString("HH:mm:ss")})");
        }

        private void Locator_PositionError(object sender, PositionErrorEventArgs e)
        {
        }

        private void Locator_PositionChanged(object sender, PositionEventArgs e)
        {
            if (!IsInTunnelMode)
            {
                var position = e.Position;

                var point = new TripPoint()
                {
                    Longitude = position.Longitude,
                    Latitude = position.Latitude,
                    RangeRadius = position.Accuracy
                };
                ManuallyPostPoint(point);
            }
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
            Console.WriteLine($"Ending trip {CurrentTrip?.Id}");
            if (CurrentTrip == null)
                return;
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Config["server"]}/trip/end?tripId={CurrentTrip.Id}&endTime={DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config["apikey"]);

            Device.BeginInvokeOnMainThread(() =>
            {
                Preferences.Set("currentTrip:inProgress", false);
                Preferences.Set("currentTrip:tripIdA", 0);
                Preferences.Set("currentTrip:tripIdB", 0);
                Preferences.Set("currentTrip:vehicleType", "");
                Preferences.Set("currentTrip:hovStatus", (int)CurrentTrip.HovStatus);
                Preferences.Set("currentTrip:tripId", "");
                Preferences.Set("currentTrip:vehicleId", "");
            });

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