using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "MapRelationMembers")]
    public class MapRelationMember
    {
        [Column(Name = nameof(RelationId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long RelationId { get; set; }

        [Column(Name = nameof(GeoId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long GeoId { get; set; }

        [Column(Name = nameof(Type)), NotNull, DataType("VARCHAR(8)")]
        public GeoType GeoType { get; set; }

        [Column(Name = nameof(Role)), DataType("VARCHAR(128)")]
        public string Role { get; set; }
    }
}
