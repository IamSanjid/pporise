using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PPOProtocol
{
    public class Shop
    {
        private readonly List<ShopItem> _items = new List<ShopItem>();
        public IReadOnlyList<ShopItem> ShopItems => _items.OrderBy(itm => itm.Uid).ToList().AsReadOnly();
        public Shop(XContainer xmlDocument)
        {
            var result = xmlDocument.Descendants("obj").ToList().FindAll(el => el.Element("var")?.Value != null);
            foreach (var sXElement in result)
            {
                if (sXElement.Element("var")?.Value != null)
                {                   
                    var sItem = new ShopItem(sXElement);
                    _items.Add(sItem);
                }
            }
        }
    }
}
