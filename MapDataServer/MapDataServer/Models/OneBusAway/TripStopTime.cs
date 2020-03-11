using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class TripStopTime
    {
        public static TripStopTime FromXml(XmlNode element)
        {
            var result = new TripStopTime();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "arrivalTime":
                        result.ArrivalTime = int.Parse(child.InnerText);
                        break;
                    case "departureTime":
                        result.DepartureTime = int.Parse(child.InnerText);
                        break;
                    case "stopId":
                        result.StopId = child.InnerText;
                        break;
                    case "distanceAlongTrip":
                        result.DistanceAlongTrip = double.Parse(child.InnerText);
                        break;
                }
            }
            return result;
        }

        public int ArrivalTime { get; set; }
        public int DepartureTime { get; set; }
        public string StopId { get; set; }
        public double DistanceAlongTrip { get; set; }
    }
}
