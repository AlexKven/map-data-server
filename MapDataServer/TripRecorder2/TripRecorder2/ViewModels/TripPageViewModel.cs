using MapDataServer.Models;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TripRecorder2.Models;
using TripRecorder2.Services;
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
            }
        }

        private TripPagePointsListService PointsListService { get; }

        public TripPageViewModel(TripPagePointsListService pointsListService)
        {
            PointsListService = pointsListService;
            Title = "Current Trip";
            StartStopCommand = new Command(StartStopRecording);
            HandleReceivedMessages();
        }

        public ICommand StartStopCommand { get; }

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

        public void StartStopRecording()
        {
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
                MessagingCenter.Send(message, "StartLongRunningTaskMessage");
                IsInProgress = true;
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
