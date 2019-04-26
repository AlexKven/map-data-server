using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "MapWays")]
    public class MapWay : GeoBase
    {
        [Column(Name = nameof(MinLat)), DataType(LinqToDB.DataType.Double)]
        public double? MinLat { get; set; }
        [Column(Name = nameof(MaxLat)), DataType(LinqToDB.DataType.Double)]
        public double? MaxLat { get; set; }
        [Column(Name = nameof(MinLon)), DataType(LinqToDB.DataType.Double)]
        public double? MinLon { get; set; }
        [Column(Name = nameof(MaxLon)), DataType(LinqToDB.DataType.Double)]
        public double? MaxLon { get; set; }
    }
}
