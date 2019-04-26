using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table("WayNodeLinks")]
    public class WayNodeLink
    {
        [Column(Name = nameof(NodeId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long NodeId { get; set; }

        [Column(Name = nameof(WayId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long WayId { get; set; }
    }
}
