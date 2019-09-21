using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "ObaTripLinks")]
    public class ObaTripLink
    {
        [Column(Name = nameof(MapTripId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long MapTripId { get; set; }

        [Column(Name = nameof(ObaTripId)), NotNull, DataType("VARCHAR(20)")]
        public string ObaTripId { get; set; }

        [Column(Name = nameof(ObaVehicleId)), DataType("VARCHAR(20)")]
        public string ObaVehicleId { get; set; }
    }
    //[Table(Name = "MapWays")]
    //public class MapWay : GeoBase
    //{
    //    [Column(Name = nameof(MinLat)), DataType(LinqToDB.DataType.Double)]
    //    public double? MinLat { get; set; }
    //    [Column(Name = nameof(MaxLat)), DataType(LinqToDB.DataType.Double)]
    //    public double? MaxLat { get; set; }
    //    [Column(Name = nameof(MinLon)), DataType(LinqToDB.DataType.Double)]
    //    public double? MinLon { get; set; }
    //    [Column(Name = nameof(MaxLon)), DataType(LinqToDB.DataType.Double)]
    //    public double? MaxLon { get; set; }
    //}
}
