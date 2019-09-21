using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Models
{
    public enum HovStatus
    {
#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Sov))]
#endif
        Sov,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Hov2))]
#endif
        Hov2,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Hov3))]
#endif
        Hov3,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Motorcycle))]
#endif
        Motorcycle,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Transit))]
#endif
        Transit,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Pedestrian))]
#endif
        Pedestrian,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Bicycle))]
#endif
        Bicycle,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(Streetcar))]
#endif
        Streetcar,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(LightRail))]
#endif
        LightRail,

#if __SERVER__
        [LinqToDB.Mapping.MapValue(Value = nameof(HeavyRail))]
#endif
        HeavyRail,
    }
}
