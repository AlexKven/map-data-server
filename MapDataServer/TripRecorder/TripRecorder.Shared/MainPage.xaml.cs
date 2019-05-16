using MapDataServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TripRecorder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = this;
            HttpClient = new HttpClient();
        }

#if WINDOWS_UWP || __WASM__
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#endif
        private HttpClient HttpClient { get; }

        private Trip _CurrentTrip;
        private Trip CurrentTrip
        {
            get => _CurrentTrip;
            set
            {
                _CurrentTrip = value;
                CurrentMessage = CurrentTrip == null ? "No trip in progress" : $"Trip ID: {CurrentTrip.Id}";
            }
        }

        private string _CurrentMessage = "No trip in progress";
        public string CurrentMessage
        {
            get => _CurrentMessage;
            set
            {
                _CurrentMessage = value;
                RaisePropertyChanged(nameof(CurrentMessage));
            }
        }

        private async void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentMessage = "Starting new trip...";
            var trip = new Trip() { HovStatus = HovStatus.Sov, VehicleType = "Kia Spectra" };
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:59759/trip/start");
            request.Content = new StringContent(JsonConvert.SerializeObject(trip));
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request);
            var str = await response.Content.ReadAsStringAsync();
            CurrentTrip = JsonConvert.DeserializeObject<Trip>(str);
        }
    }
}
