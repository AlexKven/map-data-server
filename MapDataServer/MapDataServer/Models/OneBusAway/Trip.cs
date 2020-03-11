using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class Trip
    {
        public static Trip FromXml(XmlNode element)
        {
            var result = new Trip();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "id":
                        result.Id = child.InnerText;
                        break;
                    case "routeId":
                        result.RouteId = child.InnerText;
                        break;
                    case "tripShortName":
                        result.TripShortName = child.InnerText;
                        break;
                    case "tripHeadsign":
                        result.TripHeadsign = child.InnerText;
                        break;
                    case "serviceId":
                        result.ServiceId = child.InnerText;
                        break;
                    case "shapeId":
                        result.ShapeId = child.InnerText;
                        break;
                    case "directionId":
                        result.DirectionId = child.InnerText;
                        break;
                }
            }
            return result;
        }

        public string Id { get; set; }
        public string RouteId { get; set; }
        public string TripShortName { get; set; }
        public string TripHeadsign { get; set; }
        public string ServiceId { get; set; }
        public string ShapeId { get; set; }
        public string DirectionId { get; set; }
    }
}
