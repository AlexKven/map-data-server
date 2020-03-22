using MapDataServer.Helpers;
using MapDataServer.Models;
using Microcharts;
using Microsoft.Extensions.Configuration;
using MvvmHelpers;
using Newtonsoft.Json;
using Plugin.Geolocator;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TripRecorder2.Models;
using TripRecorder2.Services;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace TripRecorder2.ViewModels
{
    class TripListItem
    {
        public TripListItem(string text, long tripId)
        {
            Text = text;
            TripId = tripId;
        }

        public string Text { get; }
        public long TripId { get; }

        public override string ToString() => Text;
    }

    class PastTripsPageViewModel : BaseViewModel
    {
        private IConfiguration Config { get; }
        private ILocationProvider LocationProvider { get; }

        public ICommand GoCommand { get; }

        public PastTripsPageViewModel(IConfiguration config, ILocationProvider locationProvider)
        {
            Config = config;
            LocationProvider = locationProvider;

            GoCommand = new Command(Go);

            StartDate = DateTime.Today - TimeSpan.FromDays(7);
            EndDate = DateTime.Today;

            SetLocation();
        }

        private HttpClient HttpClient { get; } = new HttpClient();

        #region Top-Level Views
        private bool _ShowProgress;
        public bool ShowProgress
        {
            get => _ShowProgress;
            set => SetProperty(ref _ShowProgress, value);
        }

        private double _Progress;
        public double Progress
        {
            get => _Progress;
            set => SetProperty(ref _Progress, value);
        }

        private bool _ShowSummary = false;
        public bool ShowSummary
        {
            get => _ShowSummary;
            set => SetProperty(ref _ShowSummary, value);
        }

        private bool _ShowMap = false;
        public bool ShowMap
        {
            get => _ShowMap;
            set => SetProperty(ref _ShowMap, value);
        }

        private DateTime _StartDate;
        public DateTime StartDate
        {
            get => _StartDate;
            set
            {
                SetProperty(ref _StartDate, value);
                if (StartDate >= EndDate)
                    EndDate = StartDate.AddDays(1);
            }
        }

        private DateTime _EndDate;
        public DateTime EndDate
        {
            get => _EndDate;
            set
            {
                SetProperty(ref _EndDate, value);
                if (StartDate >= EndDate)
                    StartDate = EndDate.AddDays(-1);
            }
        }

        public ObservableRangeCollection<TripListItem> TripListItems { get; }
            = new ObservableRangeCollection<TripListItem>();

        private int _SelectedItemIndex = 0;
        public int SelectedItemIndex
        {
            get => _SelectedItemIndex;
            set
            {
                SetProperty(ref _SelectedItemIndex, value);
                if (SelectedItemIndex == 1)
                {
                    LoadSummary(StartDate, EndDate);
                    ShowSummary = true;
                }
                else
                    ShowSummary = false;
                if (SelectedItemIndex > 1)
                {
                    LoadTrip(TripListItems[SelectedItemIndex].TripId);
                    ShowMap = true;
                }
                else
                    ShowMap = false;
            }
        }
        #endregion

        #region Activity Summary
        private Chart _DistanceChart;
        public Chart DistanceChart
        {
            get => _DistanceChart;
            private set => SetProperty(ref _DistanceChart, value);
        }

        private Chart _TimeChart;
        public Chart TimeChart
        {
            get => _TimeChart;
            private set => SetProperty(ref _TimeChart, value);
        }

        private Chart _TripsChart;
        public Chart TripsChart
        {
            get => _TripsChart;
            private set => SetProperty(ref _TripsChart, value);
        }

        private string _InsignificantTripsLabel;
        public string InsignificantTripsLabel
        {
            get => _InsignificantTripsLabel;
            set => SetProperty(ref _InsignificantTripsLabel, value);
        }

        private void DisplaySummary(ActivitySummary summary)
        {

            var colors = new SKColor[]
            {
                // Sov
                SKColors.Red,
                // Hov2
                SKColors.Orange,
                // Hov3
                SKColors.DarkOrange,
                // Motorcycle
                SKColors.Crimson,
                // Transit
                SKColors.Green,
                // Pedestrian
                SKColors.Pink,
                // Bicycle
                SKColors.Purple,
                // Streetcar
                SKColors.Teal,
                // LightRail
                SKColors.Blue,
                // HeavyRail
                SKColors.DarkBlue
            };

            var distanceEntries = new List<Microcharts.Entry>();
            var timeEntries = new List<Microcharts.Entry>();
            var tripEntries = new List<Microcharts.Entry>();

            var totalDistance = summary.Distances.Sum(v => v);
            var totalTime = summary.Times.Sum(t => t.TotalSeconds);
            var totalTrips = summary.Counts.Sum();

            for (int i = 0; i < colors.Length; i++)
            {
                var distance = summary.Distances[i];
                var distFraction = totalDistance == 0 ? 0 : (double)distance / (double)totalDistance;
                var time = summary.Times[i];
                var timeFraction = totalTime == 0 ? 0 : time.TotalSeconds / totalTime;
                var count = summary.Counts[i];
                var countFraction = totalTrips == 0 ? 0 : (double)count / (double)totalTrips;

                distanceEntries.Add(new Microcharts.Entry(distance)
                {
                    Color = colors[i],
                    Label = ((HovStatus)i).ToString(),
                    TextColor = colors[i],
                    ValueLabel = $"{distance}m ({distFraction.ToString("P")})"
                });
                timeEntries.Add(new Microcharts.Entry((float)time.TotalSeconds)
                {
                    Color = colors[i],
                    Label = ((HovStatus)i).ToString(),
                    TextColor = colors[i],
                    ValueLabel = $"{time} ({timeFraction.ToString("P")})"
                });
                tripEntries.Add(new Microcharts.Entry(count)
                {
                    Color = colors[i],
                    Label = ((HovStatus)i).ToString(),
                    TextColor = colors[i],
                    ValueLabel = $"{count} ({countFraction.ToString("P")})"
                });
            }

            DistanceChart = new DonutChart() { Entries = distanceEntries };
            TimeChart = new DonutChart() { Entries = timeEntries };
            TripsChart = new DonutChart() { Entries = tripEntries };
            InsignificantTripsLabel = $"Excludes {summary.UnprocessedCount} unprocessed and {summary.InsignificantCount} zero-length trips.";
        }
        #endregion

        #region Trip Map
        private List<TripPoint> CurrentTripPoints { get; }
            = new List<TripPoint>();
        private List<GeometryHelpers.TripPointWithEdges> CurrentTripEdges { get; }
            = new List<GeometryHelpers.TripPointWithEdges>();

        public IEnumerable<string> MapViewItems { get; } = new string[]
        {
            "Trip points", "Approximate trip path"
        };

        private int _SelectedMapViewItemIndex = 0;
        public int SelectedMapViewItemIndex
        {
            get => _SelectedMapViewItemIndex;
            set
            {
                SetProperty(ref _SelectedMapViewItemIndex, value);
                SetTripMap();
            }
        }

        public ObservableRangeCollection<Circle> TripPoints { get; }
             = new ObservableRangeCollection<Circle>();

        public HeatmapPolyline TripPolyline { get; }
            = new HeatmapPolyline();

        private Position _MapCenter;
        public Position MapCenter
        {
            get => _MapCenter;
            set => SetProperty(ref _MapCenter, value);
        }

        private async void SetLocation()
        {
            try
            {
                if (await LocationProvider.CheckPermission())
                {
                    var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
                    MapCenter = new Position(position.Latitude, position.Longitude);
                }
            }
            catch (Exception) { }
        }

        private void SetTripMap()
        {
            TripPoints.Clear();
            TripPolyline.Segments.Clear();
            switch (SelectedMapViewItemIndex)
            {
                case 0:
                    foreach (var point in CurrentTripPoints)
                    {
                        var distanceFactor = Math.Min(1.0, 10.0 / point.RangeRadius);
                        var baseColor = Color.Red;
                        if (point.IsTailPoint.HasValue)
                        {
                            if (point.IsTailPoint.Value)
                                baseColor = Color.DarkGray;
                            else
                            {
                                if (CurrentTripEdges.Any(e => e.TripPoint == point))
                                    baseColor = Color.DarkCyan;
                                else
                                    baseColor = Color.DarkOrange;
                            }
                        }
                        TripPoints.Add(new Circle()
                        {
                            Center = new Position(point.Latitude, point.Longitude),
                            Radius = Distance.FromMeters(point.RangeRadius),
                            FillColor = baseColor.MultiplyAlpha(0.05 + 0.15 * distanceFactor),
                            StrokeColor = baseColor.MultiplyAlpha(0.2 + 0.6 * distanceFactor),
                            StrokeWidth = 1
                        });
                    }
                    break;
                case 1:
                    GeometryHelpers.TripPointWithEdges prev = null;
                    foreach (var edge in CurrentTripEdges)
                    {
                        if (prev != null)
                        {
                            if (prev.OutEdge.HasValue && edge.InEdge.HasValue)
                                TripPolyline.Segments.Add(new HeatmapPolylineSegment(
                                    new Position(prev.OutEdge.Value.lat, prev.OutEdge.Value.lon),
                                    new Position(edge.InEdge.Value.lat, edge.InEdge.Value.lon),
                                    0, 0));
                            if (edge.InEdge.HasValue && edge.OutEdge.HasValue)
                                TripPolyline.Segments.Add(new HeatmapPolylineSegment(
                                    new Position(edge.InEdge.Value.lat, edge.InEdge.Value.lon),
                                    new Position(edge.OutEdge.Value.lat, edge.OutEdge.Value.lon),
                                    0, 0));
                        }
                        prev = edge;
                    }
                    break;
            }
        }
        #endregion

        #region Downloading Data
        private async Task<ActivitySummary> DownloadSummary(DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            var url = $"{Config["server"]}/trip/activitySummary?startTime={FormatDate(start)}&endTime={FormatDate(end)}";
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            try
            {
                var response = await HttpClient.SendAsync(request, cancellationToken);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ActivitySummary>(content);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<PaginatedResponse<TripSummary>> DownloadTripSummaries
            (DateTime startTime, DateTime endTime, int start, CancellationToken cancellationToken)
        {
            var url = $"{Config["server"]}/trip/tripsForTimeRange?startTime={FormatDate(startTime)}&endTime={FormatDate(endTime)}&start={start}";
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            try
            {
                var response = await HttpClient.SendAsync(request, cancellationToken);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginatedResponse<TripSummary>>(content);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<PaginatedResponse<TripPoint>> DownloadTripPoints(long tripId, int start, CancellationToken cancellationToken)
        {
            var url = $"{Config["server"]}/trip/pointsForTrip?tripId={tripId}&start={start}";
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            try
            {
                var response = await HttpClient.SendAsync(request, cancellationToken);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaginatedResponse<TripPoint>>(content);
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region Formatting
        private string FormatDate(DateTime dateTime)
        {
            var utc = dateTime.ToUniversalTime();
            return utc.ToString("s", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Task Management
        private CancellationTokenSource RequestTokenSource { get; set; }
        private TaskCompletionSource<object> RequestTaskSource { get; set; }

        private async Task<CancellationToken?> WaitForPreviousTask()
        {
            RequestTokenSource?.Cancel();
            RequestTokenSource = new CancellationTokenSource();
            var token = RequestTokenSource.Token;
            if (RequestTaskSource != null)
                await RequestTaskSource.Task;
            if (token.IsCancellationRequested)
                return null;
            return token;
        }
        #endregion

        #region Top-Level Tasks
        private async void LoadSummary(DateTime start, DateTime end)
        {
            try
            {
                DateTime next(DateTime cur)
                {
                    if (end - cur < TimeSpan.FromDays(4))
                        return end;
                    return cur + TimeSpan.FromDays(4);
                }

                var wait = await WaitForPreviousTask();
                if (!wait.HasValue)
                    return;
                var token = wait.Value;
                RequestTaskSource = new TaskCompletionSource<object>();

                Progress = 0;
                ShowProgress = true;
                var current = start;
                var previous = start;
                ActivitySummary currentSummary = new ActivitySummary();
                DisplaySummary(currentSummary);
                while (current < end && !token.IsCancellationRequested)
                {
                    Progress = (current - start).TotalSeconds / (end - start).TotalSeconds;
                    current = next(current);
                    var summary = await DownloadSummary(previous, current, token);
                    if (summary == null)
                        return;
                    currentSummary.AddSummary(summary);
                    DisplaySummary(currentSummary);
                    previous = current;
                }
            }
            finally
            {
                ShowProgress = false;
                RequestTaskSource.SetResult(null);
            }
        }

        private async void LoadTripsList(DateTime start, DateTime end)
        {
            try
            {
                var wait = await WaitForPreviousTask();
                if (!wait.HasValue)
                    return;
                var token = wait.Value;
                RequestTaskSource = new TaskCompletionSource<object>();

                TripListItems.Clear();
                SelectedItemIndex = -1;
                TripListItems.Add(new TripListItem("Select an item", 0));
                TripListItems.Add(new TripListItem("Activity summary", 0));
                SelectedItemIndex = 0;

                Progress = 0;
                ShowProgress = true;
                int current = 0;
                int total;
                do
                {
                    var result = await DownloadTripSummaries(start, end, current, token);
                    if (result == null)
                        return;
                    total = result.Total;
                    current = result.Count + result.Start;
                    if (total == 0)
                        return;
                    Progress = (double)current / (double)total;
                    foreach (var trip in result.Items)
                    {
                        TripListItems.Add(new TripListItem(
                            $"{trip.HovStatus}: {trip.StartTime.ToLocalTime().ToString("MM/dd/yyyy")} ({trip.DistanceMeters} meters)",
                            trip.Id));
                    }
                } while (current < total);
            }
            finally
            {
                ShowProgress = false;
                RequestTaskSource.SetResult(null);
            }
        }

        private async void LoadTrip(long tripId)
        {
            try
            {
                var wait = await WaitForPreviousTask();
                if (!wait.HasValue)
                    return;
                var token = wait.Value;
                RequestTaskSource = new TaskCompletionSource<object>();

                CurrentTripPoints.Clear();
                CurrentTripEdges.Clear();
                TripPolyline.Segments.Clear();
                TripPoints.Clear();

                Progress = 0;
                ShowProgress = true;
                int current = 0;
                int total;
                do
                {
                    var result = await DownloadTripPoints(tripId, current, token);
                    if (result == null)
                        return;
                    total = result.Total;
                    current = result.Count + result.Start;
                    if (total == 0)
                        return;
                    CurrentTripPoints.AddRange(result.Items);
                    CurrentTripEdges.Clear();
                    GeometryHelpers.GetTotalLength(CurrentTripPoints
                        .Where(p => p.IsTailPoint == false), CurrentTripEdges);
                    SetTripMap();
                    Progress = (double)current / (double)total;
                } while (current < total);
            }
            finally
            {
                ShowProgress = false;
                RequestTaskSource.SetResult(null);
            }
        }
        #endregion

        private void Go()
        {
            SelectedItemIndex = 0;
            LoadTripsList(StartDate, EndDate);
        }
    }
}
