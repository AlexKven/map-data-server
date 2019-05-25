using LinqToDB;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class DatabaseExtensions
    {
        public static IQueryable<MapNode> WithinBoundingBox(this IQueryable<MapNode> table, GeoPoint swCorner, GeoPoint neCorner)
            => table.Where(node => node.Longitude >= swCorner.Longitude && node.Longitude <= neCorner.Longitude &&
                            node.Latitude >= swCorner.Latitude && node.Latitude <= neCorner.Latitude);

        public static IQueryable<MapNode> ClosestToPoint(this IQueryable<MapNode> table, GeoPoint point, int count) =>
            table.OrderBy(node => Math.Sqrt((node.Longitude - point.Longitude) * (node.Longitude - point.Longitude) +
                (node.Latitude - point.Latitude) * (node.Latitude - point.Latitude))).Take(count);

    }
}
