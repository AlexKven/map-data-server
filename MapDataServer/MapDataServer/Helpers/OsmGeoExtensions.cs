using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class OsmGeoExtensions
    {
        public static GeoType GetGeoType(this OsmSharp.OsmGeoType geo, bool highway = false)
        {
            if (highway)
                return GeoType.Highway;
            switch (geo)
            {
                case OsmSharp.OsmGeoType.Node:
                    return GeoType.Node;
                case OsmSharp.OsmGeoType.Way:
                    return GeoType.Way;
                case OsmSharp.OsmGeoType.Relation:
                    return GeoType.Relation;
            }
            return default(GeoType);
        }
    }
}
