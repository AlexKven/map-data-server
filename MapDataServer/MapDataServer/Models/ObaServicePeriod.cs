using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "ObaServicePeriods")]
    public class ObaServicePeriod
    {
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long Id { get; set; }

        [Column(Name = nameof(EndTime)), DataType(LinqToDB.DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Column(Name = nameof(ObaAgencyId)), DataType("VARCHAR(10)")]
        public string ObaAgencyId { get; set; }
    }
}
