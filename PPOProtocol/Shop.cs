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

        public Shop(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.IndexOf("[", StringComparison.Ordinal) != -1 && loc2.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    while (loc1 < strArrayA.Length)
                    {
                        var data = "[" + strArrayA[loc1] + "]";
                        _items.Add(ParseItem(data, loc1));
                        loc1 = loc1 + 1;
                    }
                    return;
                }

                Console.WriteLine("parse shop items bracket error: " + loc2);
            }
        }

        private ShopItem ParseItem(string tempStr2, int uid)
        {
            if (tempStr2 != "[]" && tempStr2 != "")
            {
                if (tempStr2.IndexOf("[", StringComparison.Ordinal) != -1 && tempStr2.IndexOf("]", StringComparison.Ordinal) == tempStr2.Length - 1)
                {
                    tempStr2 = tempStr2.Substring(1, tempStr2.Length - 2);
                    var itm = new ShopItem(tempStr2.Split(','), uid);
                    return itm;
                }

                Console.WriteLine("parseArray bracket error");
            }

            return null;
        }
    }
}
