using Android.App;
using Android.Content;
using Android.OS;
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
                    var tracker = new LocationTracker(DependencyService.Get<ILocationProvider>());
                    tracker.Run(TokenSource.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    if (TokenSource.IsCancellationRequested)
                    {
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
            if (TokenSource != null)
            {
                TokenSource.Token.ThrowIfCancellationRequested();

                TokenSource.Cancel();
            }
            base.OnDestroy();
        }
    }
}