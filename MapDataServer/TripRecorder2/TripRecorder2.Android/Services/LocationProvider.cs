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
        private Context Context { get; }
        private MainActivity Activity { get; }

        public LocationProvider(Context context, MainActivity activity)
        {
            Context = context;
            Activity = activity;
        }

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