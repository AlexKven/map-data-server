﻿using Android.App;
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

        public override void OnCreate()
        {
            StartForeground(Constants.SERVICE_RUNNING_NOTIFICATION_ID, GetNotification());
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            TokenSource = new CancellationTokenSource();

            Task.Run(() => {
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
                        var message = new CancelledMessage();
                        Device.BeginInvokeOnMainThread(
                            () => MessagingCenter.Send(message, "CancelledMessage")
                        );
                    }
                }

            }, TokenSource.Token);

            return StartCommandResult.Sticky;
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
            builder.SetSmallIcon(Resource.Drawable.ic_stat_name);
            builder.SetContentTitle("Trip Recorder II");
            builder.SetContentText("Your trip is recording.");
            builder.SetContentIntent(BuildIntentToShowMainActivity());
            builder.SetOngoing(true);
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