using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public enum GeoType
    {
        [LinqToDB.Mapping.MapValue(Value = "Node")]
        Node,
        [LinqToDB.Mapping.MapValue(Value = "Way")]
        Way,
        [LinqToDB.Mapping.MapValue(Value = "Relation")]
        Relation
    }
}
