using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class Route
    {
        public static Route FromXml(XmlNode element)
        {
            var result = new Route();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "id":
                        result.Id = child.InnerText;
                        break;
                    case "shortName":
                        result.ShortName = child.InnerText;
                        break;
                    case "longName":
                        result.LongName = child.InnerText;
                        break;
                    case "description":
                        result.Description = child.InnerText;
                        break;
                    case "type":
                        result.Type = byte.Parse(child.InnerText);
                        break;
                    case "url":
                        result.Url = child.InnerText;
                        break;
                    case "color":
                        result.Color = child.InnerText;
                        break;
                    case "textColor":
                        result.TextColor = child.InnerText;
                        break;
                    case "agencyId":
                        result.AgencyId = child.InnerText;
                        break;
                }
            }
            return result;
        }

        public string Id { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Description { get; set; }
        public byte Type { get; set; }
        public string Url { get; set; }
        public string Color { get; set; }
        public string TextColor { get; set; }
        public string AgencyId { get; set; }
    }
}
