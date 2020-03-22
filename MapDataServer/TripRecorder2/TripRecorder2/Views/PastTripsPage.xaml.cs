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
    }
}