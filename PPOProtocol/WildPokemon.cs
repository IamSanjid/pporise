using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class WildPokemon
    {
        //[77,,45,false,Pidgeot,18,false,default,136,0]
        //or
        //`136,136,Pidgeot,18,false,45,a454d1b7848e11c45ab07ca2f49a63a3,,default,false`
        public string Name => PokemonNamesManager.Instance.Names[Id];
        public string Form { get; private set; }
        public string Health => CurrentHealth + "/" + MaxHealth;
        public int Id { get; private set; }
        public int Level { get; private set; }
        public bool IsShiny { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public PokemonType Type1 { get; private set; }
        public PokemonType Type2 { get; private set; }
        public string Ailment { get; private set; }
        public string EncryptedAbility { get; private set; }
        public bool IsRare { get; set; }
        public bool IsElite { get; private set; }

        private string _status;
        public string Status
        {
            get
            {
                return CurrentHealth == 0 ? "KO" : _status;
            }
            set
            {
                _status = value;
            }
        }
        public PokemonAbility Ability { get; private set; }
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
        public void New(string[] data)
        {
            CurrentHealth = Convert.ToInt32(data[0]);
            MaxHealth = Convert.ToInt32(data[1]);
            Id = Convert.ToInt32(data[3]);
            IsShiny = (data[4].ToLowerInvariant() == "true");
            Level = Convert.ToInt32(data[5]);
            EncryptedAbility = data[6];
            Ailment = data[7];
            Form = data[8];
            IsElite = data[9].ToLowerInvariant() == "true";
            IsRare = IsElite;
            Type1 = TypesManager.Instance.Type1[Id];
            Type2 = TypesManager.Instance.Type2[Id];
        }

        public void Update(string[] data)
        {
            Ability = new PokemonAbility(Convert.ToInt32(data[0]));
            Ailment = data[1];
            Level = Convert.ToInt32(data[2]);
            IsElite = data[3].ToLowerInvariant() == "true";
            IsRare = IsElite;
            Id = Convert.ToInt32(data[5]);
            IsShiny = (data[6].ToLowerInvariant() == "true");
            Form = data[7];
            MaxHealth = Convert.ToInt32(data[8]);
            CurrentHealth = Convert.ToInt32(data[9]);
        }

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (maxHealth != -1) MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
        }
    }
}
