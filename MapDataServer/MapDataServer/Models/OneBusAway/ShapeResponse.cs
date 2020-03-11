using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapDataServer.Models.OneBusAway
{
    public class ShapeResponse
    {
        public static ShapeResponse FromXml(XmlNode element)
        {
            var result = new ShapeResponse();
            var children = element.ChildNodes;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.Item(i);
                switch (child.Name)
                {
                    case "version":
                        result.Version = child.InnerText;
                        break;
                    case "code":
                        result.Code = child.InnerText;
                        break;
                    case "currentTime":
                        result.CurrentTime = long.Parse(child.InnerText);
                        break;
                    case "text":
                        result.Text = child.InnerText;
                        break;
                    case "data":
                        var data = child.SelectSingleNode("entry");
                        result.Data = EncodedPolyline.FromXml(data);
                        break;
                }
            }
            return result;
        }

        public string Version { get; set; }

        public string Code { get; set; }

        public long CurrentTime { get; set; }

        public string Text { get; set; }

        public EncodedPolyline Data { get; set; }
    }
}
