using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class TripSchedule
    {
        public static TripSchedule FromXml(XmlNode element)
        {
            var result = new TripSchedule();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "timeZone":
                        result.TimeZone = child.InnerText;
                        break;
                    case "previousTripId":
                        result.PreviousTripId = child.InnerText;
                        break;
                    case "nextTripId":
                        result.NextTripId = child.InnerText;
                        break;
                    case "stopTimes":
                        var stopTimes = child.SelectNodes("tripStopTime");
                        for (int j = 0; j < stopTimes.Count; j++)
                        {
                            result.StopTimes.Add(TripStopTime.FromXml(stopTimes.Item(j)));
                        }
                        break;
                }
            }
            return result;
        }

        public string TimeZone { get; set; }
        public string PreviousTripId { get; set; }
        public string NextTripId { get; set; }
        public List<TripStopTime> StopTimes { get; }
            = new List<TripStopTime>();
    }
}
