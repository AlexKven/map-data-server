using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace MapDataServer.Models
{
    [Table(Name = "MapRegions")]
    public class MapRegion
    {
        public MapRegion() { }

        public MapRegion(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        [PrimaryKey, NotNull, DataType(LinqToDB.DataType.Double)]
        public double Lat { get; set; }
        [PrimaryKey, NotNull, DataType(LinqToDB.DataType.Double)]
        public double Lon { get; set; }
    }
}
