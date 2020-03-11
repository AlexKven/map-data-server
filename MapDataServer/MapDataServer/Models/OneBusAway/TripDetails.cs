using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class TripDetails
    {
        public static TripDetails FromXml(XmlNode element)
        {
            var result = new TripDetails();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "tripId":
                        result.TripId = child.InnerText;
                        break;
                    case "serviceDate":
                        result.ServiceDate = long.Parse(child.InnerText);
                        break;
                    case "schedule":
                        result.Schedule = TripSchedule.FromXml(child);
                        break;
                }
            }
            return result;
        }
        public string TripId { get; set; }
        public long ServiceDate { get; set; }
        public TripSchedule Schedule { get; set; }
    }
}
