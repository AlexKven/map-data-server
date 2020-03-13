using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "ObaTrips")]
    public class ObaTrip
    {
        [Column(Name = nameof(ObaTripId)), PrimaryKey, NotNull, DataType("VARCHAR(64)")]
        public string ObaTripId { get; set; }

        [Column(Name = nameof(ObaServicePeriodId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long ObaServicePeriodId { get; set; }

        [Column(Name = nameof(ObaRouteId)), NotNull, DataType("VARCHAR(32)")]
        public string ObaRouteId { get; set; }

        [Column(Name = nameof(TripShortName)), DataType("VARCHAR(64)")]
        public string TripShortName { get; set; }

        [Column(Name = nameof(TripHeadsign)), DataType("VARCHAR(64)")]
        public string TripHeadsign { get; set; }

        [Column(Name = nameof(ServiceId)), PrimaryKey, NotNull, DataType("VARCHAR(64)")]
        public string ServiceId { get; set; }

        [Column(Name = nameof(ServiceDate)), NotNull, DataType(LinqToDB.DataType.Date)]
        public DateTime ServiceDate { get; set; }

        [Column(Name = nameof(TimeZone)), DataType("VARCHAR(64)")]
        public string TimeZone { get; set; }

        [Column(Name = nameof(Shape)), DataType(LinqToDB.DataType.Text)]
        public string Shape { get; set; }

        [Column(Name = nameof(ShapeLength)), DataType(LinqToDB.DataType.Int32)]
        public int ShapeLength { get; set; }
    }
}
