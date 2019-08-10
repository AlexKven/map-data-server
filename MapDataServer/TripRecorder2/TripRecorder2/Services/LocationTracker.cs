using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TripRecorder2.Models;
using Xamarin.Forms;

namespace TripRecorder2.Services
{
    public class LocationTracker
    {
        ILocationProvider LocationProvider { get; }

        public LocationTracker(ILocationProvider locationProvider)
        {
            LocationProvider = locationProvider;
        }

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                await LocationProvider.CheckPermission();
                var locator = CrossGeolocator.Current;

                for (long i = 0; i < long.MaxValue; i++)
                {
                    token.ThrowIfCancellationRequested();

                    SendMessage($"Getting location...");

                    locator.DesiredAccuracy = 100;
                    var position = await locator.GetPositionAsync(token: token);

                    SendMessage($"{position.Latitude}, {position.Longitude}");

                    await Task.Delay(15000);
                }
            }, token);
        }

        private void SendMessage(string message)
        {
            var tickedMessage = new TickedMessage
            {
                Message = message
            };

            Device.BeginInvokeOnMainThread(() =>
            {
                MessagingCenter.Send<TickedMessage>(tickedMessage, "TickedMessage");
            });
        }
    }
}
