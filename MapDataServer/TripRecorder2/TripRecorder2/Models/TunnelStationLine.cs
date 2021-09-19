using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2.Models
{
    public class TunnelStationLine
    {
        public string LineName { get; set; }
        public TunnelStation[] TunnelStations { get; set; }
    }
}
