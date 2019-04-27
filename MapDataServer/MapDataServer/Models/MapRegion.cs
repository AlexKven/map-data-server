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
        public MapRegion() => SavedDate = DateTime.UtcNow;

        public MapRegion(int lon, int lat)
        {
            Lat = lat;
            Lon = lon;
            SavedDate = DateTime.UtcNow;
        }

        public static long GetValue(int lon, int lat)
        {
            long result = lat;
            result = result << 32;
            result = result | (uint)lon;
            return result;
        }

        public static (int, int) GetComponents(long value)
        {
            int lon = (int)(value & uint.MaxValue);
            int lat = (int)(value >> 32);
            return (lon, lat);
        }

        public static long GetRegionContaining(double lon, double lat)
        {
            int rLat = (int)Math.Floor(lat / .01);
            int rLon = (int)Math.Floor(lon / .01);
            return GetValue(rLon, rLat);
        }

        public static long GetStartOfQuadrantContaining(double lon, double lat)
            => GetRegionContaining(lon - .005, lat - .005);

        [PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long Value { get; set; }

        public int Lat
        {
            get => GetComponents(Value).Item1;
            set => Value = GetValue(value, Lat);
        }
        public int Lon
        {
            get => GetComponents(Value).Item2;
            set => Value = GetValue(Lon, value);
        }

        [Column(Name = nameof(SavedDate)), DataType(LinqToDB.DataType.DateTime), NotNull]
        public DateTime SavedDate { get; set; }
    }
}
