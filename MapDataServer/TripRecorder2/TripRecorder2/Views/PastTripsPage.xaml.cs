using Autofac;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripRecorder2.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TripRecorder2.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PastTripsPage : ContentPage
    {
        public PastTripsPage()
        {
            InitializeComponent();
            var viewModel = AppStartup.Container.Resolve<PastTripsPageViewModel>();
            BindingContext = viewModel;
        }

        //private void ViewModel_SummaryDownloaded(object sender, MapDataServer.Models.ActivitySummary e)
        //{
        //    var entries = new[]
        //       {
        //        new Microcharts.Entry(e.Distances[0])
        //        {
        //            Label = "Driving alone",
        //            Color = SKColor.Parse("#266489")
        //        },
        //        new Microcharts.Entry(e.Distances[1])
        //        {
        //        Label = "HOV 2",
        //        Color = SKColor.Parse("#68B9C0")
        //        },
        //        new Microcharts.Entry(e.Distances[4])
        //        {
        //        Label = "Bus",
        //        Color = SKColor.Parse("#90D585")
        //        }
        //    };
        //    var chart = new DonutChart()
        //    {
        //        Entries = entries
        //    };

        //    ModeChartDistance.Chart = chart;
        //}
    }
}