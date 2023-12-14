using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Autofac;
using System;
using System.Threading;
using System.Threading.Tasks;
using TripRecorder2.Models;
using TripRecorder2.Services;
using Xamarin.Forms;

namespace TripRecorder2.Droid.Services
{
    [Service]
    public class LocationTrackerService : Service
    {
        CancellationTokenSource TokenSource = null;

        static object lockObj = new object();
        static DateTime lastPointTime;

        public override void OnCreate()
        {
            StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, GetNotification());
            MessagingCenter.Subscribe<PostPointMessage>(this, nameof(PostPointMessage), message =>
            {
                var tracker = AppStartup.Container.Resolve<LocationTracker>();
                var point = message.Point;
                point.Time = DateTime.Now;

                // gaurd to prevent multiple points from having the same time and causing problems in processing
                lock (lockObj)
                {
                    var diff = point.Time - lastPointTime;
                    if (diff < TimeSpan.FromSeconds(2))
                    {
                        point.Time += TimeSpan.FromSeconds(2) - diff;
                    }
                    lastPointTime = point.Time;
                }
                tracker.ManuallyPostPoint(point);
            });
            MessagingCenter.Subscribe<SetTunnelModeMessage>(this, nameof(SetTunnelModeMessage), message =>
            {
                var tracker = AppStartup.Container.Resolve<LocationTracker>();
                tracker.IsInTunnelMode = message.IsInTunnelMode;
            });
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Action == Constants.ACTION_END_TRIP)
            {
                StopSelf();
            }
            else
            {
                TokenSource = new CancellationTokenSource();

                Task.Run(() =>
                {
                    try
                    {
                        //INVOKE THE SHARED CODE
                        var tracker = AppStartup.Container.Resolve<LocationTracker>();
                        tracker.Run(TokenSource.Token).Wait();
                    }
                    catch (System.OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        if (TokenSource.IsCancellationRequested)
                        {
                            StopForeground(true);
                            StopTracker();
                        }
                    }

                }, TokenSource.Token);
            }

            return StartCommandResult.Sticky;
        }

        private void StopTracker()
        {
            var message = new CancelledMessage();
            Device.BeginInvokeOnMainThread(
                () => MessagingCenter.Send(message, "CancelledMessage")
            );
        }

        public override void OnDestroy()
        {
            CancelTask();
            base.OnDestroy();
        }

        public override bool StopService(Intent name)
        {
            CancelTask();
            return base.StopService(name);
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            CancelTask();
            base.OnTaskRemoved(rootIntent);
        }

        private Notification GetNotification()
        {
            NotificationChannel channel = new NotificationChannel("Channel_01", "My Channel", NotificationImportance.Max);
            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
            Notification.Builder builder = new Notification.Builder(ApplicationContext, "Channel_01");
            builder.SetSmallIcon(Resource.Drawable.ic_trip);
            builder.SetContentTitle("Trip Recorder II");
            builder.SetContentText("Your trip is recording.");
            builder.SetContentIntent(BuildIntentToShowMainActivity());
            builder.AddAction(BuildEndTripAction());
            builder.SetOngoing(true);
            return builder.Build();
        }

        private Notification.Action BuildEndTripAction()
        {
            var stopServiceIntent = new Intent(this, GetType());
            stopServiceIntent.SetAction(Constants.ACTION_END_TRIP);
            var stopServicePendingIntent = PendingIntent.GetService(this, 0, stopServiceIntent, 0);

            var builder = new Notification.Action.Builder(Android.Resource.Drawable.IcMediaPause,
                                                          GetText(Resource.String.end_trip),
                                                          stopServicePendingIntent);
            return builder.Build();
        }

        PendingIntent BuildIntentToShowMainActivity()
        {
            var notificationIntent = new Intent(this, typeof(MainActivity));
            notificationIntent.SetAction(Constants.ACTION_MAIN_ACTIVITY);
            notificationIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
            notificationIntent.PutExtra(Constants.SERVICE_STARTED_KEY, true);

            var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);
            return pendingIntent;
        }

        void CancelTask()
        {
            if (TokenSource != null && !TokenSource.IsCancellationRequested)
            {
                TokenSource.Cancel();
            }
        }
    }
}