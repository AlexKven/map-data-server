using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    public abstract class GeoBase : IEquatable<GeoBase>
    {
        [Column(Name = nameof(Id)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Int64)]
        public long Id { get; set; }

        [Column(Name = nameof(GeneratedDate)), DataType(LinqToDB.DataType.Date)]
        public DateTime? GeneratedDate { get; set; }

        [Column(Name = nameof(SavedDate)), DataType(LinqToDB.DataType.Date), NotNull]
        public DateTime SavedDate { get; set; }

        [Column(Name = nameof(IsVisible)), DataType(LinqToDB.DataType.Boolean)]
        public bool? IsVisible { get; set; }

        public override bool Equals(object obj)
        {
            return ((obj as GeoBase)?.Id == Id);
        }

        public bool Equals(GeoBase other)
        {
            return other?.Id == Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(GeoBase left, GeoBase right)
        {
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return ReferenceEquals(left, null) == ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(GeoBase left, GeoBase right)
        {
            return !(left == right);
        }
    }
}