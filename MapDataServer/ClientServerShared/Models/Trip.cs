using System;
using System.Collections.Generic;
using System.Text;

#if __SERVER__
using LinqToDB.Mapping;
#endif

namespace MapDataServer.Models
{
#if __SERVER__
    [Table("Trips")]
#endif
    public class Trip
    {
#if __SERVER__
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
#endif
        public long Id { get; set; }

#if __SERVER__
        [Column(Name = nameof(HovStatus)), NotNull, DataType("VARCHAR(10)")]
#endif
        public HovStatus HovStatus { get; set; }

#if __SERVER__
        [Column(Name = nameof(VehicleType)), DataType("VARCHAR(32)")]
#endif
        public string VehicleType { get; set; }

#if __SERVER__
        [Column(Name = nameof(InProgress)), DataType(LinqToDB.DataType.Boolean)]
#endif
        public bool InProgress { get; set; }

#if __SERVER__
        [Column(Name = nameof(StartTime)), NotNull, DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime StartTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(EndTime)), DataType(LinqToDB.DataType.DateTime)]
#endif
        public DateTime? EndTime { get; set; }
    }
}