using LinqToDB;
using MapDataServer.Models;
using MapDataServer.Services;
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


        public static async Task<MapNode[]> GetClosestNodes(this IMapDataSchema database, GeoPoint point, int count)
        {
            var lon = point.Longitude;
            var lat = point.Latitude;
            var query = (from node in database.MapNodes
                         join link in database.WayNodeLinks on new { id = node.Id, highway = true } equals new { id = link.NodeId, highway = link.Highway }
                         orderby Math.Sqrt((node.Longitude - lon) * (node.Longitude - lon) + (node.Latitude - lat) * (node.Latitude - lat)) ascending
                         select node).Take(count);
            return await query.ToArrayAsync();
        }

        public static async Task<FullTrip> GetFullTrip(this IMapDataSchema database, long tripId)
        {
            var trip = await database.Trips.Where(t => t.Id == tripId).ToAsyncEnumerable().FirstOrDefault();
            if (trip == null)
                return null;
            var points = await database.TripPoints.Where(tp => tp.TripId == tripId).OrderBy(tp => tp.Time).ToAsyncEnumerable().ToList();

            var result = new FullTrip() { TripId = tripId, VehicleType = trip.VehicleType, HovStatus = trip.HovStatus, BusRoute = trip.BusRoute };
            result.Points.AddRange(points);
            return result;
        }

    }
}
