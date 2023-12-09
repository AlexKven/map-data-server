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
using static System.Net.Mime.MediaTypeNames;

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

        private bool _areEventsRegistered = false;
        private void RegisterEvents(IGeolocator locator)
        {
            if (!_areEventsRegistered)
            {
                locator.PositionChanged += Locator_PositionChanged;
                locator.PositionError += Locator_PositionError;
            }
            _areEventsRegistered = true;
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                SendMessage("Starting new trip...");
                foreach (var logs in RemovedPointLogs)
                {
                    logs.Clear();
                }

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

                RegisterEvents(locator);

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
                SendMessage($"POST: {point.Latitude}, {point.Longitude} ({point.RangeRadius}, {DateTime.Now.ToString("HH:mm:ss")})");
        }

        private List<TripPoint>[] RemovedPointLogs = new List<TripPoint>[]
        {
            new List<TripPoint>(),
            new List<TripPoint>(),
            new List<TripPoint>(),
            new List<TripPoint>(),
            new List<TripPoint>(),
            new List<TripPoint>(),
        };

        private static double Dist(TripPoint first, TripPoint second) => GeometryHelpers.GetDistance(first.Latitude, first.Longitude, second.Latitude, second.Longitude);
        private static double Time(TripPoint first, TripPoint second) => (second.Time - first.Time).TotalSeconds;

        private bool IsBadPoint(TripPoint current, List<TripPoint> prev, List<TripPoint> next)
        {
            var testCount = Math.Min(prev.Count, next.Count);

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
                if (outSpeed > 2 * avgSpeed || inSpeed > 2 * avgSpeed)
                {
                    RemovedPointLogs[4].Add(current);
                    return true;
                }

                var distFactor = 1 + avgRadius / current.RangeRadius;
                if (outDist > distFactor * avgDist || inDist > distFactor * avgDist)
                {
                    RemovedPointLogs[5].Add(current);
                    return true;
                }
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
                            log.Append($"POST: {next.RangeRadius:0.0}");
                            QualityBuffer.RemoveAt(0);
                        }
                        //SendMessage($"Point: {point.Latitude}, {point.Longitude} ({point.RangeRadius}, {DateTime.Now.ToString("HH:mm:ss")})");
                    }
                    log.AppendLine();
                    log.Append(QBS());

                    if (QualityBuffer.Count == 0)
                        return;

                    var minRadius = QualityBuffer.Select(p => p.RangeRadius).Min();

                    bool ShouldFilterPoint(int bufferIndex)
                    {
                        var filterPoint = QualityBuffer[bufferIndex];
                        if (filterPoint.RangeRadius > minRadius * 4)
                        {
                            RemovedPointLogs[2].Add(filterPoint);
                            return true;
                        }
                        if (QualityBuffer.Count > 1)
                        {
                            var radiiSum = 0.0;
                            for (int i = 0; i < QualityBuffer.Count; i++)
                            {
                                if (i != bufferIndex)
                                    radiiSum += QualityBuffer[i].RangeRadius;
                            }
                            var avgRadius = radiiSum / (QualityBuffer.Count - 1);
                            if (filterPoint.RangeRadius > avgRadius * 1.5)
                            {
                                RemovedPointLogs[3].Add(filterPoint);
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
                            {
                                return true;
                            }
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
                            var index = indicesToFilter[i];
                            RemovedPointLogs[0].Add(QualityBuffer[index]);
                            QualityBuffer.RemoveAt(index);
                        }
                        indicesToFilter.Clear();
                    }
                    if (QualityBuffer.Count >= QualityBufferSize)
                    {
                        var overlapPadding = 2;
                        double largestOverlapping;
                        int largestOverlappingInd;
                        do
                        {
                            largestOverlapping = 0;
                            largestOverlappingInd = -1;
                            for (var i = overlapPadding; i < QualityBuffer.Count - overlapPadding; i++)
                            {
                                for (var j = i; j < QualityBuffer.Count - overlapPadding; j++)
                                {
                                    if (j == i)
                                        continue;
                                    var pointI = QualityBuffer[i];
                                    var pointJ = QualityBuffer[j];
                                    if (Dist(pointI, pointJ) < pointI.RangeRadius + pointJ.RangeRadius)
                                    {
                                        if (pointI.RangeRadius > largestOverlapping)
                                        {
                                            largestOverlapping = pointI.RangeRadius;
                                            largestOverlappingInd = i;
                                        }
                                        if (pointJ.RangeRadius > largestOverlapping)
                                        {
                                            largestOverlapping = pointJ.RangeRadius;
                                            largestOverlappingInd = j;
                                        }
                                    }
                                }
                            }
                            if (largestOverlappingInd >= 0)
                            {
                                RemovedPointLogs[1].Add(QualityBuffer[largestOverlappingInd]);
                                QualityBuffer.RemoveAt(largestOverlappingInd);
                            }

                        } while (largestOverlappingInd >= 0);
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
                            log.AppendLine();
                            log.Append($"POST: {p.RangeRadius:0.0}");
                            ManuallyPostPoint(p, false);
                        }
                        QualityBuffer.Clear();
                    }
                    log.AppendLine();
                    log.Append(QBS());
                    log.AppendLine();
                    log.Append("LOG: ");
                    log.Append(string.Join(", ", RemovedPointLogs.Select(l => l.Count)));
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