using MapDataServer.Models;
using Microcharts;
using Microsoft.Extensions.Configuration;
using MvvmHelpers;
using Newtonsoft.Json;
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
using Xamarin.Forms;

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

        public ICommand GoCommand { get; }

        public PastTripsPageViewModel(IConfiguration config)
        {
            Config = config;

            GoCommand = new Command(Go);

            StartDate = DateTime.Today - TimeSpan.FromDays(7);
            EndDate = DateTime.Today;
        }

        private HttpClient HttpClient { get; } = new HttpClient();

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

        public ObservableRangeCollection<TripListItem> TripListItems { get; }
            = new ObservableRangeCollection<TripListItem>();

        private int _SelectedItemIndex = 0;
        public int SelectedItemIndex
        {
            get => _SelectedItemIndex;
            set
            {
                _SelectedItemIndex = value;
                if (SelectedItemIndex == 1)
                {
                    LoadSummary(StartDate, EndDate);
                    ShowSummary = true;
                }
                else
                    ShowSummary = false;
            }
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

        private string FormatDate(DateTime dateTime)
        {
            var utc = dateTime.ToUniversalTime();
            return utc.ToString("s", CultureInfo.InvariantCulture);
        }

        private async Task<ActivitySummary> GetSummary(DateTime start, DateTime end, CancellationToken cancellationToken)
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

        private async Task<PaginatedResponse<TripSummary>> GetTripSummaries
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
                    var summary = await GetSummary(previous, current, token);
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

                Progress = 0;
                ShowProgress = true;
                int current = 0;
                int total;
                do
                {
                    var result = await GetTripSummaries(start, end, current, token);
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

        private void Go()
        {
            SelectedItemIndex = 0;
            TripListItems.Clear();
            TripListItems.Add(new TripListItem("Select an item", 0));
            TripListItems.Add(new TripListItem("Activity summary", 0));
            LoadTripsList(StartDate, EndDate);
        }
    }
}
