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
    public class DaySummary
    {
#if __SERVER__
        [Column(Name = nameof(Date)), PrimaryKey, NotNull, DataType(LinqToDB.DataType.Date)]
#endif
        public DateTime Date { get; set; }

#if __SERVER__
        [Column(Name = nameof(UnprocessedCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int UnprocessedCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(InsignificantCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int InsignificantCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(SovCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int SovCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov2Count)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int Hov2Count { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov3Count)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int Hov3Count { get; set; }

#if __SERVER__
        [Column(Name = nameof(MotorcycleCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int MotorcycleCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(TransitCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int TransitCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(PedestrianCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int PedestrianCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(BicycleCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int BicycleCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(StreetcarCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int StreetcarCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(LightRailCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int LightRailCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(HeavyRailCount)), NotNull, DataType(LinqToDB.DataType.Int16)]
#endif
        public int HeavyRailCount { get; set; }

#if __SERVER__
        [Column(Name = nameof(SovDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint SovDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov2Distance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint Hov2Distance { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov3Distance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint Hov3Distance { get; set; }

#if __SERVER__
        [Column(Name = nameof(MotorcycleDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint MotorcycleDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(TransitDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint TransitDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(PedestrianDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint PedestrianDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(BicycleDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint BicycleDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(StreetcarDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint StreetcarDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(LightRailDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint LightRailDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(HeavyRailDistance)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint HeavyRailDistance { get; set; }

#if __SERVER__
        [Column(Name = nameof(SovTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint SovTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov2Time)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint Hov2Time { get; set; }

#if __SERVER__
        [Column(Name = nameof(Hov3Time)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint Hov3Time { get; set; }

#if __SERVER__
        [Column(Name = nameof(MotorcycleTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint MotorcycleTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(TransitTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint TransitTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(PedestrianTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint PedestrianTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(BicycleTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint BicycleTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(StreetcarTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint StreetcarTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(LightRailTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint LightRailTime { get; set; }

#if __SERVER__
        [Column(Name = nameof(HeavyRailTime)), NotNull, DataType(LinqToDB.DataType.UInt32)]
#endif
        public uint HeavyRailTime { get; set; }

        public ActivitySummary ToActivitySummary()
        {
            var result = new ActivitySummary();
            result.UnprocessedCount = UnprocessedCount;
            result.InsignificantCount = InsignificantCount;
            result.Counts[0] = SovCount;
            result.Counts[1] = Hov2Count;
            result.Counts[2] = Hov3Count;
            result.Counts[3] = MotorcycleCount;
            result.Counts[4] = TransitCount;
            result.Counts[5] = PedestrianCount;
            result.Counts[6] = BicycleCount;
            result.Counts[7] = StreetcarCount;
            result.Counts[8] = LightRailCount;
            result.Counts[9] = HeavyRailCount;
            result.Distances[0] = SovDistance;
            result.Distances[1] = Hov2Distance;
            result.Distances[2] = Hov3Distance;
            result.Distances[3] = MotorcycleDistance;
            result.Distances[4] = TransitDistance;
            result.Distances[5] = PedestrianDistance;
            result.Distances[6] = BicycleDistance;
            result.Distances[7] = StreetcarDistance;
            result.Distances[8] = LightRailDistance;
            result.Distances[9] = HeavyRailDistance;
            result.Times[0] = TimeSpan.FromSeconds(SovTime);
            result.Times[1] = TimeSpan.FromSeconds(Hov2Time);
            result.Times[2] = TimeSpan.FromSeconds(Hov3Time);
            result.Times[3] = TimeSpan.FromSeconds(MotorcycleTime);
            result.Times[4] = TimeSpan.FromSeconds(TransitTime);
            result.Times[5] = TimeSpan.FromSeconds(PedestrianTime);
            result.Times[6] = TimeSpan.FromSeconds(BicycleTime);
            result.Times[7] = TimeSpan.FromSeconds(StreetcarTime);
            result.Times[8] = TimeSpan.FromSeconds(LightRailTime);
            result.Times[9] = TimeSpan.FromSeconds(HeavyRailTime);

            return result;
        }

        //public uint[] Distances { get; set; }
        //public TimeSpan[] Times { get; set; }
        //public int[] Counts { get; set; }
        //public int UnprocessedCount { get; set; }
        //public int InsignificantCount { get; set; }
    }
}