using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "ObaRoutes")]
    public class ObaRoute
    {

        [Column(Name = nameof(ObaServicePeriodId)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long ObaServicePeriodId { get; set; }

        [Column(Name = nameof(ObaRouteId)), PrimaryKey, NotNull, DataType("VARCHAR(32)")]
        public string ObaRouteId { get; set; }

        [Column(Name = nameof(ShortName)), DataType("VARCHAR(32)")]
        public string ShortName { get; set; }

        [Column(Name = nameof(LongName)), DataType("VARCHAR(64)")]
        public string LongName { get; set; }

        [Column(Name = nameof(Description)), DataType(LinqToDB.DataType.Text)]
        public string Description { get; set; }

        [Column(Name = nameof(Type)), DataType(LinqToDB.DataType.Byte)]
        public string Type { get; set; }

        [Column(Name = nameof(Url)), DataType("VARCHAR(64)")]
        public string Url { get; set; }

        [Column(Name = nameof(Color)), DataType("VARCHAR(16)")]
        public string Color { get; set; }

        [Column(Name = nameof(TextColor)), DataType("VARCHAR(16)")]
        public string TextColor { get; set; }
    }
}
