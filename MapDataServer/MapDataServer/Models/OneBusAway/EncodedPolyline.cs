using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class EncodedPolyline
    {
        public static EncodedPolyline FromXml(XmlNode element)
        {
            var result = new EncodedPolyline();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "points":
                        result.Points = child.InnerText;
                        break;
                    case "length":
                        result.Length = int.Parse(child.InnerText);
                        break;
                }
            }
            return result;
        }

        public string Points { get; set; }
        public int Length { get; set; }
    }
}
