using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "GeoTags")]
    public class GeoTag
    {
        [Column(Name = nameof(GeoId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long GeoId { get; set; }
        [Column(Name = nameof(Type)), NotNull, DataType("VARCHAR(8)")]
        public GeoType GeoType { get; set; }
        [Column(Name = nameof(Key)), PrimaryKey, NotNull, DataType("VARCHAR(64)")]
        public string Key { get; set; }
        [Column(Name = nameof(Value)), DataType("TEXT")]
        public string Value { get; set; }
    }
}
