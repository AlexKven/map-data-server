using MapDataServer.Models;
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

    public class PostPointMessage
    {
        public TripPoint Point { get; set; }
    }

    public class SetTunnelModeMessage
    {
        public bool IsInTunnelMode { get; set; }
    }
}
