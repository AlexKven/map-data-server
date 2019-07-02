using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public class MapStep
    {
        public long TripId { get; set; }

        public long NodeId { get; set; }

        public long? LinkId { get; set; }

        public ushort? WayIndex { get; set; }

        public short? WayStepCount { get; set; }

        public uint ItemIndex { get; set; }

        public double AvgSpeed { get; set; }

        public object SpeedDistribution { get; set; }
    }
}
