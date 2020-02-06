using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class TripProcessorService : IHostedService
    {
        private object PropertyLock { get; } = new object();
        private bool _Stopped;
        public bool Stopped
        {
            get
            {
                lock (PropertyLock)
                {
                    return _Stopped;
                }
            }
        }
        private TaskCompletionSource<bool> _StopTaskSource;
        private TaskCompletionSource<bool> StopTaskSource
        {
            get
            {
                lock (PropertyLock)
                {
                    return _StopTaskSource;
                }
            }
        }

        private IDatabase Database { get; }
        private ITripProcessorStatus TripProcessorStatus { get; }
        private ITripPreprocessor TripPreprocessor { get; }

        public TripProcessorService(IDatabase database, ITripProcessorStatus tripProcessorStatus, ITripPreprocessor tripPreprocessor)
        {
            Database = database;
            TripProcessorStatus = tripProcessorStatus;
            TripPreprocessor = tripPreprocessor;
        }

        private CancellationTokenSource TokenSource { get; set; }

        private void Stop()
        {
            lock (PropertyLock)
            {
                _Stopped = true;
                TokenSource.Cancel();
            }
        }

        private void Start()
        {
            lock (PropertyLock)
            {
                _Stopped = false;
                _StopTaskSource = new TaskCompletionSource<bool>();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (TokenSource != null && !TokenSource.IsCancellationRequested)
                TokenSource.Cancel();
            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;
            cancellationToken.Register(() => TokenSource.Cancel());

            Start();
            while (!Stopped)
            {
                TripProcessorStatus.RunCount++;
                try
                {
                    var now = DateTime.Now;
                    var trips = await Database.Trips.ToAsyncEnumerable()
                        .Where(t => t.EndTime.HasValue || (now - t.StartTime > TimeSpan.FromDays(1)))
                        .Select(t => t.Id).ToList(token);
                    var preprocessed = await Database.PreprocessedTrips.ToAsyncEnumerable().Select(t => t.Id).ToList(token);
                    foreach (var t in preprocessed)
                    {
                        trips.Remove(t);
                    }
                    preprocessed = null;
                    if (trips.Count == 0)
                        await Task.Delay(TimeSpan.FromSeconds(15));
                    else
                    {
                        for (int i = 0; i < trips.Count; i++)
                        {
                            await TripPreprocessor.PreprocessTrip(trips[i], token);
                        }
                    }

                }
                catch (Exception)
                {

                }
            }
            StopTaskSource.TrySetResult(true);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Stop();
            await StopTaskSource.Task;
        }
    }
}
