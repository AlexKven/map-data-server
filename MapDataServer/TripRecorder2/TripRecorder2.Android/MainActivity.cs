using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Forms;
using TripRecorder2.Models;
using Android.Content;
using TripRecorder2.Droid.Services;
using Android.Support.V4.App;
using Android;
using System.Threading.Tasks;
using System.Threading;
using TripRecorder2.Services;

namespace TripRecorder2.Droid
{
    [Activity(Label = "TripRecorder2", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private int PermissionRequestCode = 10;
        Barrier PermissionSync = new Barrier(1);
        private bool HasLocationPermission { get; set; }

        private bool PermissionResponded { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            MessagingCenter.Subscribe<StartLongRunningTaskMessage>(this, "StartLongRunningTaskMessage", message =>
            {
                var intent = new Intent(this, typeof(LocationTrackerService));
                StartForegroundService(intent);
            });
            MessagingCenter.Subscribe<StopLongRunningTaskMessage>(this, "StopLongRunningTaskMessage", message =>
            {
                var intent = new Intent(this, typeof(LocationTrackerService));
                StopService(intent);
            });

            SetupLocationProvider();

            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == PermissionRequestCode)
            {
                HasLocationPermission = (grantResults[0] == Permission.Granted);
                PermissionResponded = true;
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void SetPermissionSync(int participants)
        {
            if (participants > PermissionSync.ParticipantCount)
                PermissionSync.AddParticipants(participants - PermissionSync.ParticipantCount);
            if (participants < PermissionSync.ParticipantCount)
                PermissionSync.RemoveParticipants(PermissionSync.ParticipantCount - participants);
        }

        internal async Task<bool> RequestLocationPermission()
        {
            var permission = Manifest.Permission.AccessFineLocation;

            PermissionResponded = false;
            ActivityCompat.RequestPermissions(this, new string[] { permission }, PermissionRequestCode);
            while (!PermissionResponded)
                await Task.Delay(250);

            return HasLocationPermission;
        }

        private void SetupLocationProvider()
        {
            DependencyService.Register<ILocationProvider, LocationProvider>();
            var provider = (LocationProvider)DependencyService.Get<ILocationProvider>();
            provider.Activity = this;
            provider.Context = ApplicationContext;
        }
    }
}