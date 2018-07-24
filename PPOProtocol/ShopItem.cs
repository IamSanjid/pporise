using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PPOProtocol
{
    public class ShopItem
    {
        public string Name { get; }
        public int? Price { get; }
        public int? Uid { get; }
        public ShopItem(XElement element)
        {
            Uid = Convert.ToInt32(element.Attribute("o")?.Value);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(element.ToString());
            var nodes = xml.GetElementsByTagName("var");

            foreach (XmlElement el in nodes)
                switch (el.GetAttribute("n"))
                {
                    case "0":
                        Name = el.InnerText;
                        break;
                    case "1":
                        Price = Convert.ToInt32(el.InnerText);
                        break;
                }
        }
        public ShopItem(string[] data, int uid)
        {
            Uid = uid;
            if (data.Length > 1)
            {
                Name = data[0];
                Price = Convert.ToInt32(data[1]);
            }
        }
    }
}
