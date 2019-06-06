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
        private double _Latitude = 0;
        private double _Longitude = 0;

        [Column(Name = nameof(Latitude)), DataType(LinqToDB.DataType.Double), NotNull]
        public double Latitude
        {
            get => _Latitude;
            set
            {
                _Latitude = value;
                Region = MapRegion.GetRegionContaining(Longitude, Latitude);
            }
        }

        [Column(Name = nameof(Longitude)), DataType(LinqToDB.DataType.Double), NotNull]
        public double Longitude
        {
            get => _Longitude;
            set
            {
                _Longitude = value;
                Region = MapRegion.GetRegionContaining(Longitude, Latitude);
            }
        }

        [Column(Name = nameof(Region)), DataType(LinqToDB.DataType.Int64), NotNull]
        public long Region { get; set; }

        public string PointFormatted => $"{Latitude}, {Longitude}";
    }
}
