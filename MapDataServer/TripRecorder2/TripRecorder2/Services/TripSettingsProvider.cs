using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2.Services
{
    public class TripSettingsProvider
    {
        public string VehicleType { get; set; }
        public HovStatus HovStatus { get; set; }
        public string ObaTripId { get; set; }
        public string ObaVehicleId { get; set; }
        public long? ResumingTripId { get; set; }
    }
}
