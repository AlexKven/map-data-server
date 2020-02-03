using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Models
{
#if __SERVER__
    [Table("PreprocessedTrips")]
#endif
    public class PreprocessedTrip
    {
#if __SERVER__
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
#endif
        public long Id { get; set; }

#if __SERVER__
        [Column(Name = nameof(ActualStartTime)), NotNull, DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime ActualStartTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(ActualEndTime)), NotNull, DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime ActualEndTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(DistanceMeters)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint DistanceMeters { get; set; }
    }
}
