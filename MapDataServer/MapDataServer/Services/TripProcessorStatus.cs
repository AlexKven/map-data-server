using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class TripProcessorStatus : ITripProcessorStatus
    {
        private object PropertyLock { get; } = new object();
        private int _RunCount = 0;
        public int RunCount {
            get
            {
                lock (PropertyLock)
                {
                    return _RunCount;
                }
            }
            set
            {
                lock (PropertyLock)
                {
                    _RunCount = value;
                }
            }
        }
    }
}
