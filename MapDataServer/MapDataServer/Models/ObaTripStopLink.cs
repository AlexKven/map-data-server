using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "ObaTripStopLinks")]
    public class ObaTripStopLink
    {
        [Column(Name = nameof(ObaTripId)), PrimaryKey, NotNull, DataType("VARCHAR(64)")]
        public string ObaTripId { get; set; }

        [Column(Name = nameof(ObaServicePeriodId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long ObaServicePeriodId { get; set; }

        [Column(Name = nameof(StopSequence)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.UInt16)]
        public ushort StopSequence { get; set; }

        [Column(Name = nameof(ArrivalTime)), NotNull, DataType(LinqToDB.DataType.Int32)]
        public int ArrivalTime { get; set; }

        [Column(Name = nameof(DepartureTime)), NotNull, DataType(LinqToDB.DataType.Int32)]
        public int DepartureTime { get; set; }

        [Column(Name = nameof(ObaStopId)), NotNull, DataType("VARCHAR(32)")]
        public string ObaStopId { get; set; }

        [Column(Name = nameof(DistanceAlongTrip)), NotNull, DataType(LinqToDB.DataType.Double)]
        public double DistanceAlongTrip { get; set; }
    }
}
