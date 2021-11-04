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
        public string Name { get; set; }
        public int Quantity { get; set; } = -1;
        public int Uid { get; set; }
        private IEnumerable<string> EquipableItems;
        public InventoryItem(string name, int qu = 1, int uid = -1)
        {
            Name = name;
            Quantity = qu;
            Uid = uid;

            EquipableItems = new List<string>()
            {
                "Red Card", "Air Balloon" ,
                "Light Clay", "Soul Shard (Enhanced)" ,
                "Focus Sash (Permanent)", "Fire Stone (Enhanced)" ,
                "Water Stone (Enhanced)" ,
                "Thunder Stone (Enhanced)", "Exp Share" ,
                "Metal Coat", "Kings Rock" ,
                "Kings Rock", "Leftovers" ,
                "Big Root", "Macho Brace" ,
                "Soothe Bell", "Muscle Band" ,
                "Wise Glasses", "Focus Band" ,
                "Focus Sash", "Expert Belt" ,
                "Razor Claw", "Razor Fang" ,
                "Black Sludge", "Shed Shell" ,
                "Shell Bell", "Lagging Tail" ,
                "Quick Claw", "Rocky Helmet" ,
                "Bright Powder", "Scope Lens" ,
                "Wide Lens", "Binding Band" ,
                "Metronome", "Eviolite" ,
                "Assault Vest", "Life Orb" ,
                "Choice Scarf", "Choice Specs" ,
                "Choice Band", "Effort Brace" ,
                "Gold Effort Brace", "Feather of Articuno" ,
                "Feather of Moltres", "Feather of Zapdos" ,
                "Feather of Moltres (Enhanced)" ,
                "Feather of Zapdos (Enhanced)" ,
                "Feather of Articuno (Enhanced)", "White Herb" ,
                "Weakness Policy", "Toxic Orb" ,
                "Flame Orb", "Damp Rock" ,
                "Heat Rock", "Smooth Rock" ,
                "Icy Rock", "Fire Gem" ,
                "Water Gem", "Electric Gem" ,
                "Grass Gem", "Ice Gem" ,
                "Fighting Gem", "Poison Gem" ,
                "Ground Gem", "Flying Gem" ,
                "Psychic Gem", "Bug Gem" ,
                "Rock Gem", "Ghost Gem" ,
                "Dragon Gem", "Dark Gem" ,
                "Steel Gem", "Normal Gem" ,
                "Fairy Gem"
            };
        }

        public bool IsEquipAble(string pokeName = "")
        {
            //----------------I promise I am not mad or stupid, Pokemon Planet checks like below lol-----------------//
            if (EquipableItems.Contains(Name))
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
                case "Deep Sea Scale" when pokeName == "Clamperl":
                case "Deep Sea Tooth" when pokeName == "Clamperl":
                    return true;
            }
            //----------------I promise I am not mad or stupid, Pokemon Planet checks like above lol-----------------//
            return false;
        }

        public bool IsUsableInBattle()
        {
            return Name.Contains("Ball") || Name.Contains("Potion") || Name == "Halloween Candy" || Name == "Soda Pop" || Name == "Lemonade";
        }

        public bool IsNormallyUsable()
        {
            return !IsUsableInBattle() && !IsEquipAble() || Name.Contains("Potion") || Name.Contains("Candy")
                || Name == "Halloween Candy" || Name == "Soda Pop" || Name == "Lemonade";
        }
    }
}
