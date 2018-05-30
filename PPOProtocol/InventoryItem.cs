using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PPOProtocol
{
    public class InventoryItem
    {
        public string Name { get; }
        public int Quntity { get; set; } = -1;
        public string Scope { get; }
        public int Uid { get; set; }
        public InventoryItem(string name, int qu = 1, string scope = "")
        {
            Name = name;
            Quntity = qu;
            Scope = scope;
        }
        public InventoryItem(XElement sXElement)
        {
            Uid = Convert.ToInt32(sXElement.Attribute("o")?.Value);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(sXElement.ToString());
            var node = xml.GetElementsByTagName("var");
            foreach (XmlElement el in node)
                switch (el.GetAttribute("n"))
                {
                    case "0":
                        Name = el.InnerText;
                        break;
                    case "1":
                        Quntity = Convert.ToInt32(el.InnerText);
                        break;
                }
        }

        public bool IsEquipAble(string pokeName = "")
        {
            //----------------I promise I am not mad or stupid, Pokemon Planet checks like below lol-----------------//
            if (Name == "Red Card" || Name == "Air Balloon" ||
                Name == "Light Clay" || Name == "Soul Shard (Enhanced)" ||
                Name == "Focus Sash (Permanent)" || Name == "Fire Stone (Enhanced)" ||
                Name == "Water Stone (Enhanced)" ||
                Name == "Thunder Stone (Enhanced)" || Name == "Exp Share" ||
                Name == "Metal Coat" || Name == "Kings Rock" ||
                Name == "Kings Rock" || Name == "Leftovers" ||
                Name == "Big Root" || Name == "Macho Brace" ||
                Name == "Soothe Bell" || Name == "Muscle Band" ||
                Name == "Wise Glasses" || Name == "Focus Band" ||
                Name == "Focus Sash" || Name == "Expert Belt" ||
                Name == "Razor Claw" || Name == "Razor Fang" ||
                Name == "Black Sludge" || Name == "Shed Shell" ||
                Name == "Shell Bell" || Name == "Lagging Tail" ||
                Name == "Quick Claw" || Name == "Rocky Helmet" ||
                Name == "Bright Powder" || Name == "Scope Lens" ||
                Name == "Wide Lens" || Name == "Binding Band" ||
                Name == "Metronome" || Name == "Eviolite" ||
                Name == "Assault Vest" || Name == "Life Orb" ||
                Name == "Choice Scarf" || Name == "Choice Specs" ||
                Name == "Choice Band" || Name == "Effort Brace" ||
                Name == "Gold Effort Brace" || Name == "Feather of Articuno" ||
                Name == "Feather of Moltres" || Name == "Feather of Zapdos" ||
                Name == "Feather of Moltres (Enhanced)" ||
                Name == "Feather of Zapdos (Enhanced)" ||
                Name == "Feather of Articuno (Enhanced)" || Name == "White Herb" ||
                Name == "Weakness Policy" || Name == "Toxic Orb" ||
                Name == "Flame Orb" || Name == "Damp Rock" ||
                Name == "Heat Rock" || Name == "Smooth Rock" ||
                Name == "Icy Rock" || Name == "Fire Gem" ||
                Name == "Water Gem" || Name == "Electric Gem" ||
                Name == "Grass Gem" || Name == "Ice Gem" ||
                Name == "Fighting Gem" || Name == "Poison Gem" ||
                Name == "Ground Gem" || Name == "Flying Gem" ||
                Name == "Psychic Gem" || Name == "Bug Gem" ||
                Name == "Rock Gem" || Name == "Ghost Gem" ||
                Name == "Dragon Gem" || Name == "Dark Gem" ||
                Name == "Steel Gem" || Name == "Normal Gem" ||
                Name == "Fairy Gem")
            {
                return true;
            }
            switch (Name)
            {
                case "Light Ball" when pokeName == "Pikachu":
                case "Lucky Punch" when pokeName == "Chansey":
                case "Thick Club" when pokeName == "Cubone" || pokeName == "Marowak":
                case "Quick Powder" when pokeName == "Ditto":
                case "Metal Powder" when pokeName == "Ditto":
                    return true;
                case "Deep Sea Scale" when pokeName == "Clamperl":
                    return true;
                case "Deep Sea Tooth" when pokeName == "Clamperl":
                    return true;
            }
            //----------------I promise I am not mad or stupid, Pokemon Planet checks like above lol-----------------//
            return false;
        }
    }
}
