using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class WildPokemon
    {
        //xt`w`-1`121,121,Geodude,74,false,56,4eae937ca1af28ccfcddb004ece3f0b2,,default``403,337,53,257`-1`0`0`15,15,15,10`
        public string Name => PokemonNamesManager.Instance.Names[Id];
        public string Form { get; }
        public string Health => CurrentHealth + "/" + MaxHealth;
        public int Id { get; }
        public int Level { get; }
        public bool IsShiny { get; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public PokemonType Type1 { get; }
        public PokemonType Type2 { get; }
        public string Ailment { get; }
        public string EncryptedAbility { get; }
        public bool IsRare { get; set; }

        public string Status = "None";
        public string Types
        {
            get
            {
                if (Type2 == PokemonType.None)
                    return Type1.ToString();
                else
                    return Type1.ToString() + "/" + Type2.ToString();
            }
        }
        internal WildPokemon(string[] data)
        {
            CurrentHealth = Convert.ToInt32(data[0]);
            MaxHealth = Convert.ToInt32(data[1]);
            Id = Convert.ToInt32(data[3]);
            IsShiny = (data[4].ToLowerInvariant() == "true");
            Level = Convert.ToInt32(data[5]);
            EncryptedAbility = data[6];
            Ailment = data[7];
            Form = data[8];
        }
    }
}
