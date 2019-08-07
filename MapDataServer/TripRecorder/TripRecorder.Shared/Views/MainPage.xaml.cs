using MapDataServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TripRecorder.ViewModels;
using Autofac;
using Android.Content;
using TripRecorder.Droid.Intents;
using Android.App;
using TripRecorder.Droid;
using Android.Support.V4.App;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TripRecorder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel ViewModel { get; }

        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = Startup.Container.Resolve<MainPageViewModel>();
            this.DataContext = ViewModel;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.StartTracking();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StopTracking();
        }
    }
}
