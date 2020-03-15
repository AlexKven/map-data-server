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

#if __SERVER__
        [Column(Name = nameof(StartLongitude)), DataType(LinqToDB.DataType.Double)]
#endif
        public double? StartLongitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(StartLatitude)), DataType(LinqToDB.DataType.Double)]
#endif
        public double? StartLatitude { get; set; }


#if __SERVER__
        [Column(Name = nameof(StartRegion)), DataType(LinqToDB.DataType.Int64)]
#endif
        public long? StartRegion { get; set; }

#if __SERVER__
        [Column(Name = nameof(EndLongitude)), DataType(LinqToDB.DataType.Double)]
#endif
        public double? EndLongitude { get; set; }

#if __SERVER__
        [Column(Name = nameof(EndLatitude)), DataType(LinqToDB.DataType.Double)]
#endif
        public double? EndLatitude { get; set; }


#if __SERVER__
        [Column(Name = nameof(EndRegion)), DataType(LinqToDB.DataType.Int64)]
#endif
        public long? EndRegion { get; set; }
    }
}
