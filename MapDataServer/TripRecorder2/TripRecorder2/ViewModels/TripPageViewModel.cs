using MapDataServer.Models;
using Microsoft.Extensions.Configuration;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using TripRecorder2.Models;
using TripRecorder2.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TripRecorder2.ViewModels
{
    public class TripPageViewModel : BaseViewModel
    {
        public class TripPointsEventArgs : EventArgs
        {
            public IEnumerable<TripPoint> Points { get; }

            public TripPointsEventArgs(IEnumerable<TripPoint> points)
            {
                Points = points;
            }
        }

        public event EventHandler<TripPointsEventArgs> PointsUpdated;

        private bool _IsInProgress = false;
        private bool IsInProgress
        {
            get => _IsInProgress;
            set
            {
                if (value != _IsInProgress)
                {
                    StartStopButtonText = value ? "Stop Recording" : "Start Recording";
                }
                _IsInProgress = value;
                CanSetTripDetails = !value;
            }
        }

        private bool _CanResumeTrip = false;
        public bool CanResumeTrip
        {
            get => _CanResumeTrip;
            set => SetProperty(ref _CanResumeTrip, value);
        }

        private bool _CanSetTripDetails = true;
        public bool CanSetTripDetails
        {
            get => _CanSetTripDetails;
            private set => SetProperty(ref _CanSetTripDetails, value);
        }

        private bool _IsInTunnelMode = false;
        public bool IsInTunnelMode
        {
            get => _IsInTunnelMode;
            set
            {
                SetProperty(ref _IsInTunnelMode, value);
                MessagingCenter.Send(new SetTunnelModeMessage()
                {
                    IsInTunnelMode = _IsInTunnelMode
                }, nameof(SetTunnelModeMessage));
            }
        }

        public ObservableRangeCollection<string> TunnelStationLineNames { get; }
            = new ObservableRangeCollection<string>();

        public ObservableRangeCollection<string> TunnelStationNames { get; }
            = new ObservableRangeCollection<string>();

        private int _CurrentTunnelStation = 0;
        public int CurrentTunnelStation
        {
            get => _CurrentTunnelStation;
            set
            {
                SetProperty(ref _CurrentTunnelStation, value);
                UpdateNextPrevTunnelStations();

                if (_CurrentTunnelStation > 0 &&
                    _CurrentTunnelStationLine > 0)
                {
                    var station = TunnelStationLines[CurrentTunnelStationLine - 1]
                        .TunnelStations[CurrentTunnelStation - 1];
                    var point = new TripPoint()
                    {
                        FromTunnelMode = true,
                        Latitude = station.Latitude,
                        Longitude = station.Longitude,
                        RangeRadius = 5
                    };
                    MessagingCenter.Send(new PostPointMessage()
                    {
                        Point = point
                    }, nameof(PostPointMessage));
                }
            }
        }

        private int _CurrentTunnelStationLine = 0;
        public int CurrentTunnelStationLine
        {
            get => _CurrentTunnelStationLine;
            set
            {
                SetProperty(ref _CurrentTunnelStationLine, value);
                OnPropertyChanged(nameof(IsLineSelected));
                UpdateNextPrevTunnelStations();

                CurrentTunnelStation = 0;
                if (_CurrentTunnelStationLine == 0)
                {
                    TunnelStationNames.Replace("(No Line Selected)");
                }
                else
                {
                    TunnelStationNames.Replace("Select Station...");
                    TunnelStationNames.AddRange(
                        TunnelStationLines[_CurrentTunnelStationLine - 1]
                        .TunnelStations
                        .Select(ts => ts.Name));
                }
                CurrentTunnelStation = 0;
            }
        }

        private TunnelStation GetAdjacentTunnelStation(bool next, out int index)
        {
            index = -1;
            if (_CurrentTunnelStationLine > 0)
            {
                var currentLine = TunnelStationLines[CurrentTunnelStationLine - 1];
                if (_CurrentTunnelStation <= 0)
                {
                    index = next ?
                        currentLine.TunnelStations.Length - 1
                        : 0;
                    return currentLine.TunnelStations[index];
                }
                else
                {
                    index = next ?
                        CurrentTunnelStation :
                        CurrentTunnelStation - 2;
                    if (index < 0)
                        return null;
                    else if (index >= currentLine.TunnelStations.Length)
                        return null;
                    return currentLine.TunnelStations[index];
                }
            }
            else
                return null;
        }
        private TunnelStation GetAdjacentTunnelStation(bool next) =>
            GetAdjacentTunnelStation(next, out var _);

        private void UpdateNextPrevTunnelStations()
        {
            OnPropertyChanged(nameof(NextTunnelStationName));
            OnPropertyChanged(nameof(PrevTunnelStationName));
            OnPropertyChanged(nameof(ShowNextTunnelStation));
            OnPropertyChanged(nameof(ShowPrevTunnelStation));
        }

        private void GoToAdjacentTunnelStation(bool next)
        {
            GetAdjacentTunnelStation(next, out var index);
            if (index > -1)
                CurrentTunnelStation = index + 1;
        }

        public string NextTunnelStationName => GetAdjacentTunnelStation(true)?.Name;
        public string PrevTunnelStationName => GetAdjacentTunnelStation(false)?.Name;
        public bool ShowNextTunnelStation => GetAdjacentTunnelStation(true) != null;
        public bool ShowPrevTunnelStation => GetAdjacentTunnelStation(false) != null;
        public ICommand GoToNextTunnelStation { get; }
        public ICommand GoToPrevTunnelStation { get; }

        public bool IsLineSelected => CurrentTunnelStationLine > 0;

        private TripPagePointsListService PointsListService { get; }

        private TripSettingsProvider TripSettingsProvider { get; }

        private IConfiguration Config { get; }

        private TunnelStationLine[] TunnelStationLines { get; }

        public TripPageViewModel(IConfiguration config, TripPagePointsListService pointsListService, TripSettingsProvider tripSettingsProvider)
        {
            Config = config;
            PointsListService = pointsListService;
            TripSettingsProvider = tripSettingsProvider;
            Title = "Current Trip";
            ResumeTripCommand = new Command(ResumeTrip);
            StartStopCommand = new Command(StartStopRecording);
            FindTripByVehicleCommand = new Command(FindTripByVehicle);
            HandleReceivedMessages();

            TunnelStationLines = Config.GetSection("tunnelStations").Get<TunnelStationLine[]>();
            TunnelStationLineNames.Replace("Select Line...");
            TunnelStationLineNames.AddRange(
                TunnelStationLines
                .Select(tsl => tsl.LineName));

            GoToNextTunnelStation = new Command(() => GoToAdjacentTunnelStation(true));
            GoToPrevTunnelStation = new Command(() => GoToAdjacentTunnelStation(false));

            var tripIdA = Preferences.Get("currentTrip:tripIdA", 0);
            var tripIdB = Preferences.Get("currentTrip:tripIdB", 0);
            var tripId = LongHelpers.ToLong(tripIdA, tripIdB);

            CanResumeTrip = Preferences.Get("currentTrip:inProgress", false) && tripId != 0;
            
            Console.WriteLine($"Current trip: {tripId}, {tripIdA}, {tripIdB}");
        }

        public ICommand ResumeTripCommand { get; }

        public ICommand StartStopCommand { get; }

        public ICommand FindTripByVehicleCommand { get; }

        private string _StartStopButtonText = "Start Recording";
        public string StartStopButtonText
        {
            get => _StartStopButtonText;
            set => SetProperty(ref _StartStopButtonText, value);
        }

        private string _DisplayMessage = "Start a trip";
        public string DisplayMessage
        {
            get => _DisplayMessage;
            set => SetProperty(ref _DisplayMessage, value);
        }

        private bool _IsStartStopButtonEnabled = true;
        public bool IsStartStopButtonEnabled
        {
            get => _IsStartStopButtonEnabled;
            set => SetProperty(ref _IsStartStopButtonEnabled, value);
        }

        public HovStatus[] HovStatuses { get; } = new HovStatus[] {
            HovStatus.Sov,
            HovStatus.Hov2,
            HovStatus.Hov3,
            HovStatus.Motorcycle,
            HovStatus.Transit,
            HovStatus.Pedestrian,
            HovStatus.Bicycle,
            HovStatus.Streetcar,
            HovStatus.LightRail,
            HovStatus.HeavyRail};

        private HovStatus _CurrentHovStatus = HovStatus.Sov;
        public HovStatus CurrentHovStatus
        {
            get => _CurrentHovStatus;
            set => SetProperty(ref _CurrentHovStatus, value);
        }

        private string _CurrentVehicleType;
        public string CurrentVehicleType
        {
            get => _CurrentVehicleType;
            set => SetProperty(ref _CurrentVehicleType, value);
        }

        private string _CurrentTripId;
        public string CurrentTripId
        {
            get => _CurrentTripId;
            set => SetProperty(ref _CurrentTripId, value);
        }

        private string _VehicleId;
        public string VehicleId
        {
            get => _VehicleId;
            set => SetProperty(ref _VehicleId, value);
        }

        public void ResumeTrip()
        {
            CanResumeTrip = false;
            PointsListService.Reset();
            ShowPointsOnMap();
            var message = new StartLongRunningTaskMessage();

            var tripIdA = Preferences.Get("currentTrip:tripIdA", 0);
            var tripIdB = Preferences.Get("currentTrip:tripIdB", 0);
            TripSettingsProvider.ResumingTripId = LongHelpers.ToLong(tripIdA, tripIdB);
            CurrentVehicleType = Preferences.Get("currentTrip:vehicleType", "");
            CurrentHovStatus = (HovStatus)Preferences.Get("currentTrip:hovStatus", 0);
            CurrentTripId = Preferences.Get("currentTrip:tripId", "");
            VehicleId = Preferences.Get("currentTrip:vehicleId", "");

            TripSettingsProvider.HovStatus = CurrentHovStatus;
            TripSettingsProvider.VehicleType = string.IsNullOrEmpty(CurrentVehicleType) ? null : CurrentVehicleType;
            TripSettingsProvider.ObaTripId = string.IsNullOrEmpty(CurrentTripId) ? null : CurrentTripId;
            TripSettingsProvider.ObaVehicleId = string.IsNullOrEmpty(VehicleId) ? null : VehicleId;
            MessagingCenter.Send(message, "StartLongRunningTaskMessage");
            IsInProgress = true;
        }

        public void StartStopRecording()
        {
            CanResumeTrip = false;
            if (IsInProgress)
            {
                var message = new StopLongRunningTaskMessage();
                MessagingCenter.Send(message, "StopLongRunningTaskMessage");
                StartStopButtonText = "Stopping...";
                IsStartStopButtonEnabled = false;
            }
            else
            {
                PointsListService.Reset();
                ShowPointsOnMap();
                var message = new StartLongRunningTaskMessage();
                TripSettingsProvider.ResumingTripId = null;
                TripSettingsProvider.HovStatus = CurrentHovStatus;
                TripSettingsProvider.VehicleType = string.IsNullOrEmpty(CurrentVehicleType) ? null : CurrentVehicleType;
                TripSettingsProvider.ObaTripId = string.IsNullOrEmpty(CurrentTripId) ? null : CurrentTripId;
                TripSettingsProvider.ObaVehicleId = string.IsNullOrEmpty(VehicleId) ? null : VehicleId;
                MessagingCenter.Send(message, "StartLongRunningTaskMessage");
                IsInProgress = true;
            }
        }

        public async void FindTripByVehicle()
        {
            CurrentTripId = "Loading...";
            var tripId = await GetTripIdForVehicleId(VehicleId);
            CurrentTripId = tripId ?? "";
        }

        private async Task<string> GetTripIdForVehicleId(string vehicleId)
        {
            var httpClient = new HttpClient();
            var url = $"https://api.pugetsound.onebusaway.org/api/where/trip-for-vehicle/{vehicleId}.xml?key={Config["obaKey"]}";
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            try
            {
                var response = await httpClient.SendAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return null;
                var content = await response.Content.ReadAsStringAsync();
                var doc = new XmlDocument();
                doc.LoadXml(content);
                return doc.SelectSingleNode("/response/data/entry/tripId")?.InnerText;
            }
            catch (Exception)
            {
                return null;
            }
        }



        void HandleReceivedMessages()
        {
            MessagingCenter.Subscribe<TickedMessage>(this, "TickedMessage", message => {
                Device.BeginInvokeOnMainThread(() => {
                    DisplayMessage = message.Message;
                    if (DisplayMessage.StartsWith("Point:"))
                    {
                        ShowPointsOnMap();
                    }
                });
            });

            MessagingCenter.Subscribe<CancelledMessage>(this, "CancelledMessage", message => {
                Device.BeginInvokeOnMainThread(() => {
                    DisplayMessage = "Recording stopped";
                    IsStartStopButtonEnabled = true;
                    IsInProgress = false;
                });
            });
        }

        void ShowPointsOnMap()
        {
            PointsUpdated?.Invoke(this, new TripPointsEventArgs(PointsListService.TripPagePoints.ToArray()));
        }
    }
}
