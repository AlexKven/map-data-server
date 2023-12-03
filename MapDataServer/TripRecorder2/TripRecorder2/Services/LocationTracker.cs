using MapDataServer.Helpers;
using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TripRecorder2.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TripRecorder2.Services
{
    public class LocationTracker
    {
        private ILocationProvider LocationProvider { get; }
        private HttpClient HttpClient { get; }
        private readonly ConcurrentQueue<TripPoint> PendingPoints = new ConcurrentQueue<TripPoint>();
        private List<TripPoint> QualityBuffer { get; } = new List<TripPoint>();
        private const int QualityBufferSize = 9;
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
                    if (!_IsInTunnelMode && value)
                    {
                        ProcessPoint(null, true);
                        BufferStarted = false;
                    }
                    _IsInTunnelMode = value;
                }
            }
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                SendMessage("Starting new trip...");

                IsInTunnelMode = TripSettingsProvider.StartInTunnelMode;
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
                        ActivityType = TripSettingsProvider.HovStatus == HovStatus.Pedestrian ? ActivityType.Fitness : ActivityType.AutomotiveNavigation,
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
                    ProcessPoint(null, true);
                    await SendPendingPoints();
                    await EndTrip();
                }
            }, token);
        }

        public void ManuallyPostPoint(TripPoint point, bool showMessage = true)
        {
            point.TripId = CurrentTrip?.Id ?? 0;

            PendingPoints.Enqueue(point);
            PointsListService.TripPagePoints.Enqueue(point);
            if (showMessage)
                SendMessage($"Point: {point.Latitude}, {point.Longitude} ({point.RangeRadius}, {DateTime.Now.ToString("HH:mm:ss")})");
        }

        private static bool IsBadPoint(TripPoint current, List<TripPoint> prev, List<TripPoint> next)
        {
            // It is a bad point if
            // angle is < 15 for first test
            // angle threshold increases by 5 for subsequent tests
            var testCount = Math.Min(prev.Count, next.Count);

            double Dist(TripPoint first, TripPoint second) => GeometryHelpers.GetDistance(first.Latitude, first.Longitude, second.Latitude, second.Longitude);
            double Time(TripPoint first, TripPoint second) => (second.Time - first.Time).TotalSeconds;

            //var totalAngle = 0.0;
            //var totalAngles = 0;
            //for (var t = 0; t < testCount; t++)
            //{
            //    var sideA = GeometryHelpers.GetDistance(next[t].Latitude, next[t].Longitude, prev[t].Latitude, prev[t].Longitude);
            //    var sideB = GeometryHelpers.GetDistance(prev[t].Latitude, prev[t].Longitude, current.Latitude, current.Longitude);
            //    var sideC = GeometryHelpers.GetDistance(next[t].Latitude, next[t].Longitude, current.Latitude, current.Longitude);
            //    var angle = GeometryHelpers.GetTriangleAngleA(sideA, sideB, sideC);
            //    if (angle.HasValue)
            //    {
            //        totalAngle++;
            //        totalAngle += angle.Value;
            //    }

            //    if (totalAngles > 0 && (totalAngle / totalAngles) * 180 / Math.PI < 15 + 5 * t)
            //        return true;
            //}
            if (prev.Any() && next.Any())
            {
                var combined = ((IEnumerable<TripPoint>)prev).Reverse().Concat(next);
                TripPoint last = null;
                var length = 0.0;
                var combinedRadius = 0.0;
                var count = 0;
                foreach (var cur in combined)
                {
                    combinedRadius += cur.RangeRadius;
                    if (last != null)
                    {
                        length += Dist(last, cur);
                        count++;
                    }
                    last = cur;
                }
                var avgSpeed = length / Time(combined.First(), combined.Last());
                var avgDist = length / count;
                var avgRadius = combinedRadius / (count + 1);

                var outDist = Dist(prev[0], current);
                var outSpeed = outDist / Time(prev[0], current);
                var inDist = Dist(current, next[0]);
                var inSpeed = inDist / Time(current, next[0]);
                if (outSpeed > 3 * avgSpeed || inSpeed > 3 * avgSpeed)
                    return true;

                var distFactor = 1 + avgRadius / current.RangeRadius;
                if (outDist > distFactor * avgDist ||  inDist > distFactor * avgDist)
                    return true;
            }
            return false;
        }

        private void ProcessPoint(TripPoint point, bool ending)
        {
            string QBS() => string.Join(", ", QualityBuffer.Select(p => string.Format("{0:0.00}", p.RangeRadius)));
            try
            {
                var log = new StringBuilder(QBS());
                var starting = !BufferStarted;
                lock (QualityBuffer)
                {
                    if (point != null)
                    {
                        point.Time = DateTime.Now;
                        QualityBuffer.Add(point);
                        while (QualityBuffer.Count > QualityBufferSize)
                        {
                            var next = QualityBuffer[0];
                            ManuallyPostPoint(next, false);
                            log.AppendLine();
                            log.Append($"Post:{next.RangeRadius:0.0}");
                            QualityBuffer.RemoveAt(0);
                        }
                        //SendMessage($"Point: {point.Latitude}, {point.Longitude} ({point.RangeRadius}, {DateTime.Now.ToString("HH:mm:ss")})");
                    }
                    log.AppendLine();
                    log.Append(QBS());

                    if (QualityBuffer.Count == 0)
                        return;

                    var minRadius = QualityBuffer.Select(p => p.RangeRadius).Min();
                    log.AppendLine();
                    log.Append($"Min:{minRadius:0.0}");

                    bool ShouldFilterPoint(int bufferIndex)
                    {
                        var filterPoint = QualityBuffer[bufferIndex];
                        if (filterPoint.RangeRadius > minRadius * 4)
                            return true;
                        if (QualityBuffer.Count > 1)
                        {
                            var radiiSum = 0.0;
                            for (int i = 0; i < QualityBuffer.Count; i++)
                            {
                                if (i != bufferIndex)
                                    radiiSum += QualityBuffer[i].RangeRadius;
                            }
                            var avgRadius = radiiSum / (QualityBuffer.Count - 1);
                            log.Append($", Avg:{avgRadius:0.0}");
                            if (filterPoint.RangeRadius > avgRadius * 1.5)
                            {
                                log.Append($", Rem:{bufferIndex}");
                                return true;
                            }

                            var prev = new List<TripPoint>();
                            var next = new List<TripPoint>();
                            for (int i = 0; i <= bufferIndex; i++)
                            {
                                var p = bufferIndex - i;
                                var n = bufferIndex + i;
                                if (p >= 0)
                                    prev.Add(QualityBuffer[p]);
                                if (n < QualityBuffer.Count)
                                    next.Add(QualityBuffer[n]);
                            }
                            if (IsBadPoint(filterPoint, prev, next))
                                return true;
                        }
                        return false;
                    }

                    var indicesToFilter = new List<int>();

                    var middleIndex = QualityBuffer.Count / 2;
                    if (QualityBuffer.Count >= QualityBufferSize)
                    {
                        var dupes = new List<(double lat, double lon, double range, List<int> indices)>();
                        for (var i = 0; i < QualityBuffer.Count; i++)
                        {
                            var curP = QualityBuffer[i];
                            var found = -1;
                            for (var j = 0; j < dupes.Count; j++)
                            {
                                if (found >= 0)
                                    continue;
                                var curD = dupes[j];
                                if (Math.Abs(curD.lat - curP.Latitude) < 0.0001 &&
                                    Math.Abs(curD.lon - curP.Longitude) < 0.0001 &&
                                    Math.Abs(curD.range - curP.RangeRadius) < 1)
                                    found = j;
                            }
                            if (found >= 0)
                                dupes[found].indices.Add(i);
                            else
                                dupes.Add((lat: curP.Latitude, lon: curP.Longitude, range: curP.RangeRadius, indices: new List<int>() { i }));
                        }
                        foreach (var dupe in dupes)
                        {
                            if (dupe.indices.Count >= 3)
                            {
                                indicesToFilter.AddRange(dupe.indices);
                            }
                        }
                        indicesToFilter.Sort();

                        for (var i = indicesToFilter.Count - 1; i >= 0; i--)
                        {
                            QualityBuffer.RemoveAt(indicesToFilter[i]);
                        }
                        indicesToFilter.Clear();
                    }
                    if (QualityBuffer.Count >= QualityBufferSize)
                    {
                        if (starting)
                        {
                            for (var i = 0; i < middleIndex; i++)
                            {
                                if (ShouldFilterPoint(i))
                                    indicesToFilter.Insert(0, i);
                            }
                            BufferStarted = true;
                        }
                        if (ShouldFilterPoint(middleIndex))
                            indicesToFilter.Insert(0, middleIndex);
                        if (ending && QualityBuffer.Count > middleIndex + 1)
                        {
                            for (var i = middleIndex + 1; i < QualityBuffer.Count; i++)
                            {
                                if (ShouldFilterPoint(i))
                                    indicesToFilter.Insert(0, i);
                            }
                        }
                    }
                    else if (ending)
                    {
                        for (var i = 0; i < QualityBuffer.Count; i++)
                        {
                            if (ShouldFilterPoint(i))
                                indicesToFilter.Insert(0, i);
                        }
                    }

                    foreach (var index in indicesToFilter)
                    {
                        QualityBuffer.RemoveAt(index);
                    }
                    indicesToFilter.Clear();

                    if (ending)
                    {
                        foreach (var p in QualityBuffer)
                        {
                            ManuallyPostPoint(p, false);
                        }
                        QualityBuffer.Clear();
                    }
                    log.AppendLine();
                    log.Append(QBS());
                    SendMessage(log.ToString());
                }
            }
            catch (Exception ex)
            {
                SendMessage(ex.ToString());
            }
        }

        private void Locator_PositionError(object sender, PositionErrorEventArgs e)
        {
        }

        bool BufferStarted = false;
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
                ProcessPoint(point, false);
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