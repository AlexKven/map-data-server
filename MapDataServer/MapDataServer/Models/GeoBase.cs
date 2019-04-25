using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public abstract class GeoBase
    {
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long Id { get; set; }

        [Column(Name = nameof(GeneratedDate)), DataType(LinqToDB.DataType.DateTime)]
        public DateTime? GeneratedDate { get; set; }

        [Column(Name = nameof(SavedDate)), DataType(LinqToDB.DataType.DateTime), NotNull]
        public DateTime SavedDate { get; set; }

        [Column(Name = nameof(IsVisible)), DataType(LinqToDB.DataType.Boolean)]
        public bool? IsVisible { get; set; }
    }
}
