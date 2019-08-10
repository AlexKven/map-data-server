using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2.Models
{
    public class StartLongRunningTaskMessage { }

    public class StopLongRunningTaskMessage { }

    public class TickedMessage
    {
        public string Message { get; set; }
    }

    public class CancelledMessage
    {
        public string Message { get; set; }
    }
}
