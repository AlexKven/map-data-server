using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using MvvmHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            SummaryDownloaded?.Invoke(this, summary);
        }

        public event EventHandler<ActivitySummary> SummaryDownloaded;
    }
}
