using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public class WaySegment
    {
        public long WayId { get; set; }
        public ushort StartNodeIndex { get; set; }
        public ushort EndNodeIndex { get; set; }
    }
}
