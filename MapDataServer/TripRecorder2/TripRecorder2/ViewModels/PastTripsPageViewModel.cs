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
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace TripRecorder2.ViewModels
{
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

        private string FormatDate(DateTime dateTime)
        {
            var utc = dateTime.ToUniversalTime();
            return utc.ToString("s", CultureInfo.InvariantCulture);
        }

        private async Task<ActivitySummary> GetSummary()
        {
            var httpClient = new HttpClient();
            var url = $"{Config["server"]}/trip/activitySummary?startTime={FormatDate(StartDate)}&endTime={FormatDate(EndDate)}";
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            try
            {
                var response = await httpClient.SendAsync(request);
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

        private async void Go()
        {
            var summary = await GetSummary();

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
                var distFraction = (double)distance / (double)totalDistance;
                var time = summary.Times[i];
                var timeFraction = time.TotalSeconds / totalTime;
                var count = summary.Counts[i];
                var countFraction = (double)count / (double)totalTrips;

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
        }
    }
}
