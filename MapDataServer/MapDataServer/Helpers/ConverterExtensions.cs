using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Helpers
{
    public static class ConverterExtensions
    {
        public static (double, double) ToTuple(this GeoPoint point) => (point.Longitude, point.Latitude);

        public static GeoPoint ToGeoPoint(this (double, double) tuple) => new GeoPoint(tuple.Item1, tuple.Item2);

        public static GeoPoint GetPoint(this MapNode node)
        {
            return new GeoPoint(node.Longitude, node.Latitude);
        }
        public static GeoPoint GetPoint(this TripPoint node)
        {
            return new GeoPoint(node.Longitude, node.Latitude);
        }
    }
}
