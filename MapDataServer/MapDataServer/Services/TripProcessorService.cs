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

        private ITripProcessorStatus TripProcessorStatus { get; }

        public TripProcessorService(ITripProcessorStatus tripProcessorStatus)
        {
            TripProcessorStatus = tripProcessorStatus;
        }

        private TaskCompletionSource<bool> Stop()
        {
            lock (PropertyLock)
            {
                _Stopped = true;
                _StopTaskSource = new TaskCompletionSource<bool>();
                return _StopTaskSource;
            }
        }

        private void Start()
        {
            lock (PropertyLock)
            {
                _Stopped = false;
                _StopTaskSource?.SetResult(true);
                _StopTaskSource = null;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => Stop());
            Start();
            while (!Stopped)
            {
                TripProcessorStatus.RunCount++;
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Stop().Task;
        }
    }
}
