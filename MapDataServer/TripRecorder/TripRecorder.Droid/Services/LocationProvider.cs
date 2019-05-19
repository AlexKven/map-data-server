using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TripRecorder.Models;

namespace TripRecorder.Droid.Services
{
    public class LocationProvider : ILocationProvider
    {
        public Task<DeviceLocation> GetLocation()
        {
            throw new NotImplementedException();
        }

        public async Task Initialize()
        {
        }
    }
}