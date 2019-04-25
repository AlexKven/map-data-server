using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "MapNodes")]
    public class MapNode : GeoBase
    {
        [Column(Name = nameof(Latitude)), DataType(LinqToDB.DataType.Double)]
        public double Latitude { get; set; }

        [Column(Name = nameof(Longitude)), DataType(LinqToDB.DataType.Double)]
        public double Longitude { get; set; }
    }
}
