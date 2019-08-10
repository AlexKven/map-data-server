using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripRecorder2.Models;
using TripRecorder2.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TripRecorder2.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TripPage : ContentPage
    {
        public TripPage()
        {
            InitializeComponent();
            BindingContext = new TripPageViewModel();
            HandleReceivedMessages();
        }

        void HandleReceivedMessages()
        {
            MessagingCenter.Subscribe<TickedMessage>(this, "TickedMessage", message => {
                Device.BeginInvokeOnMainThread(() => {
                    Ticker.Text = message.Message;
                });
            });

            MessagingCenter.Subscribe<CancelledMessage>(this, "CancelledMessage", message => {
                Device.BeginInvokeOnMainThread(() => {
                    Ticker.Text = "Cancelled";
                });
            });
        }

        private void StartButton_Clicked(object sender, EventArgs e)
        {
            var message = new StartLongRunningTaskMessage();
            MessagingCenter.Send(message, "StartLongRunningTaskMessage");
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            var message = new StopLongRunningTaskMessage();
            MessagingCenter.Send(message, "StopLongRunningTaskMessage");
        }
    }
}