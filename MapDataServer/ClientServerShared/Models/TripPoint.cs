using System;
using System.Collections.Generic;
using System.Text;

#if __SERVER__
using LinqToDB.Mapping;
#endif

namespace MapDataServer.Models
{
#if __SERVER__
    [Table("TripPoints")]
#endif
    public class TripPoint
    {
#if __SERVER__
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
#endif
        public long Id { get; set; }

#if __SERVER__
        [Column(Name = nameof(TripId)), NotNull, DataType(LinqToDB.DataType.Int64)]
#endif
        public long TripId { get; set; }

#if __SERVER__
        [Column(Name = nameof(Longitude)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double Longitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(Latitude)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double Latitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(RangeRadius)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double RangeRadius { get; set; }

#if __SERVER__
        [Column(Name = nameof(Time)), NotNull, DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime Time { get; set; }

#if __SERVER__
        [Column(Name = nameof(IsTailPoint)), DataType(LinqToDB.DataType.Boolean)]
#endif
        public bool? IsTailPoint { get; set; }

#if __SERVER__
        [Column(Name = nameof(FromTunnelMode)), DataType(LinqToDB.DataType.Boolean)]
#endif
        public bool FromTunnelMode { get; set; } = false;
    }
}
