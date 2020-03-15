using System;
using System.Collections.Generic;
using System.Text;

#if __SERVER__
using LinqToDB.Mapping;
#endif

namespace MapDataServer.Models
{
#if __SERVER__
    [Table("ObaTripPointLinks")]
#endif
    public class ObaTripPointLink
    {
#if __SERVER__
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
#endif
        public long Id { get; set; }

#if __SERVER__
        [Column(Name = nameof(ObaTripId)), NotNull, DataType("VARCHAR(64)")]
#endif
        public string ObaTripId { get; set; }

#if __SERVER__
        [Column(Name = nameof(MappedLongitude)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double MappedLongitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(MappedLatitude)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double MappedLatitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(Time)), NotNull, DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime Time { get; set; }

#if __SERVER__
        [Column(Name = nameof(DistanceAlongTrip)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double DistanceAlongTrip { get; set; }

#if __SERVER__
        [Column(Name = nameof(DelaySeconds)), NotNull, DataType(LinqToDB.DataType.Double)]
#endif
        public double DelaySeconds { get; set; }
    }
}