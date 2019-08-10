using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using TripRecorder2.Services;

namespace TripRecorder2.Droid.Services
{
    public class LocationProvider : ILocationProvider
    {
        internal Context Context { private get; set; }
        internal MainActivity Activity { private get; set; }

        public async Task<bool> CheckPermission()
        {
            var permission = Manifest.Permission.AccessFineLocation;

            var check = ActivityCompat.CheckSelfPermission(Context, permission);
            if (check == Android.Content.PM.Permission.Granted)
                return true;

            return await Activity.RequestLocationPermission();
        }
    }
}