using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using TripRecorder.Droid.Services;

namespace TripRecorder.Droid.Intents
{
    public class LocationIntentService : IntentService
    {
        private ILocationProvider LocationProvider { get; }

        public LocationIntentService(ILocationProvider locationProvider)
        {
            LocationProvider = locationProvider;
        }

        protected override void OnHandleIntent(Intent intent)
        {
            var count = intent.GetIntExtra("count", 0);

            for (int i = 1; i <= count; i++)
            {
                Notification notification = new Notification.Builder(this, "testChannel")
                    .SetSmallIcon(Resource.Drawable.notification_bg)
                    .SetContentTitle("Test Title")
                    .SetContentText("Test Context").Build();

                NotificationManagerCompat compat = NotificationManagerCompat.From(this);
                compat.Notify(703, notification);
            }
        }
    }
}