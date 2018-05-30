using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PPOProtocol
{
    public class Pokemon
    {
        public string Form { get; }
        public int Uid { get; set; }
        public int UniqueId { get; private set; }
        public string OriginalTrainer { get; }
        public PokemonAbility Ability { get; set; }
        public int AbilityLength { get; set; }
        public string Ailment { get; set; }
        private string _itemHeld;
        public string ItemHeld {
            get
            {
                if (_itemHeld is null)
                    return "";
                else if (_itemHeld.ToLowerInvariant() == "none")
                    return "";
                else
                    return _itemHeld;
            }
            set
            {
                _itemHeld = value;
            }
        }
        public string Name => PokemonNamesManager.Instance.Names[Id];

        public string Health => CurrentHealth + "/" + MaxHealth;
        public int Id { get; }
        public int Level => Experience.CurrentLevel;
        public bool IsShiny { get; }
        public PokemonExperience Experience { get; set; }

        public string Nature
        {
            get
            {
                if (_nature != null)
                    return _nature.FirstOrDefault().ToString().ToUpper() + _nature.Substring(1);
                return _nature;
            }
        }

        private string _nature;
        public PokemonStats Stats { get; }
        public PokemonStats IV { get; }
        public PokemonStats EV { get; }
        public PokemonType Type1 { get; set; }
        public PokemonType Type2 { get; set; }
        public int MaxHealth { get; set; }
        public int CurrentHealth { get; set; }
        public int Happiness { get; }
        public string Gender { get; }
        public PokemonMove[] Moves { get; set; }
        public bool IsRare { get; set; }
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
        int level = -1;
        int exp = -1;
        int totalExp = -1;
        public Pokemon(XmlNodeList node)
        {
            if (node is null)
                return;
            int damnR = int.MinValue;
            EV = new PokemonStats();
            IV = new PokemonStats();
            Stats = new PokemonStats();
            Moves = new PokemonMove[4];
            foreach (XmlNode no in node)
            {
                if (!int.TryParse(no.Attributes[0].Value, out damnR))
                {
                    if (no.Attributes[0].Value.ToLowerInvariant() == "moves")
                    {
                        foreach (XmlElement el in no)
                        {
                            switch (el.GetAttribute("n"))
                            {
                                case "3":
                                    Moves[3] = new PokemonMove(4, Convert.ToInt32(el.InnerText), 999); //Infinite PP lol
                                    break;
                                case "2":
                                    Moves[2] = new PokemonMove(3, Convert.ToInt32(el.InnerText), 999); //Infinite PP lol
                                    break;
                                case "1":
                                    Moves[1] = new PokemonMove(2, Convert.ToInt32(el.InnerText), 999); //Infinite PP lol
                                    break;
                                case "0":
                                    Moves[0] = new PokemonMove(1, Convert.ToInt32(el.InnerText), 999); //Infinite PP lol
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (no.Attributes[0].Value)
                        {
                            //normal
                            case "id":
                                Id = Convert.ToInt32(no.InnerText);
                                break;
                            case "heldItem":
                                _itemHeld = no.InnerText;
                                break;
                            case "currentHp":
                                CurrentHealth = Convert.ToInt32(no.InnerText);
                                break;
                            case "hp":
                                MaxHealth = Convert.ToInt32(no.InnerText);
                                Stats.Health = Convert.ToInt32(no.InnerText);
                                break;
                            case "uniqueId":
                                UniqueId = Convert.ToInt32(no.InnerText);
                                break;
                            case "nature":
                                _nature = no.InnerText;
                                break;
                            case "ability":
                                Ability = new PokemonAbility(Convert.ToInt32(no.InnerText));
                                break;
                            case "ailment":
                                Ailment = no.InnerText;
                                break;
                            case "ailmentLength":
                                AbilityLength = Convert.ToInt32(no.InnerText);
                                break;
                            case "exp":
                                exp = Convert.ToInt32(no.InnerText);
                                break;
                            case "level":
                                level = Convert.ToInt32(no.InnerText);
                                break;
                            case "totalExp":
                                totalExp = Convert.ToInt32(no.InnerText);
                                break;
                            case "originalCatcher":
                                OriginalTrainer = no.InnerText;
                                break;
                            case "happiness":
                                Happiness = Convert.ToInt32(no.InnerText);
                                break;
                            case "form":
                                Form = no.InnerText;
                                break;
                            case "status":
                                Status = no.InnerText;
                                break;
                            case "shiny":
                                IsShiny = no.InnerText != "0";
                                break;
                            //stat
                            case "attack":
                                Stats.Attack = Convert.ToInt32(no.InnerText);
                                break;
                            case "speed":
                                Stats.Speed = Convert.ToInt32(no.InnerText);
                                break;
                            case "defense":
                                Stats.Defence = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialAttack":
                                Stats.SpAttack = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialDefense":
                                Stats.SpDefence = Convert.ToInt32(no.InnerText);
                                break;
                            //iv
                            case "attackIV":
                                IV.Attack = Convert.ToInt32(no.InnerText);
                                break;
                            case "speedIV":
                                IV.Speed = Convert.ToInt32(no.InnerText);
                                break;
                            case "defenseIV":
                                IV.Defence = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialAttackIV":
                                IV.SpAttack = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialDefenseIV":
                                IV.SpDefence = Convert.ToInt32(no.InnerText);
                                break;
                            case "hpIV":
                                IV.Health = Convert.ToInt32(no.InnerText);
                                break;
                            //ev
                            case "attackEV":
                                EV.Attack = Convert.ToInt32(no.InnerText);
                                break;
                            case "speedEV":
                                EV.Speed = Convert.ToInt32(no.InnerText);
                                break;
                            case "defenseEV":
                                EV.Defence = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialAttackEV":
                                EV.SpAttack = Convert.ToInt32(no.InnerText);
                                break;
                            case "specialDefenseEv":
                                EV.SpDefence = Convert.ToInt32(no.InnerText);
                                break;
                            case "hpEV":
                                EV.Health = Convert.ToInt32(no.InnerText);
                                break;
                        }
#if DEBUG
                    Console.WriteLine($"Name: {no.Attributes[0].Value} - Value:{no.InnerText}");
#endif
                    }
                }
            }
            Experience = new PokemonExperience(level, exp, totalExp);
            Type1 = TypesManager.Instance.Type1[Id];
            Type2 = TypesManager.Instance.Type2[Id];
        }
        internal Pokemon(string[] data)
        {
            if (data[0] != "")
                UniqueId = Convert.ToInt32(data[0]);
            if (data[32] != "")
                Id = Convert.ToInt32(data[32]);
            if (data[7] != "")
                MaxHealth = Convert.ToInt32(data[7]);
            if (data[27] != "")
                CurrentHealth = Convert.ToInt32(data[27]);
            if (data[31] != "" && data[29] != "" && data[28] != "")
                Experience = new PokemonExperience(Convert.ToInt32(data[31]), Convert.ToInt32(data[29]), Convert.ToInt32(data[28]));
            IsShiny = (data[30].ToLowerInvariant() == "true");
            Status = data[36];
            Gender = "M";

            Moves = new PokemonMove[4];
            if (data[21] != "")
                Moves[0] = new PokemonMove(1, Convert.ToInt32(data[21]), 999, 999); //Infinite PP lol
            if (data[22] != "")
                Moves[1] = new PokemonMove(2, Convert.ToInt32(data[22]), 999, 999);
            if (data[23] != "")
                Moves[2] = new PokemonMove(3, Convert.ToInt32(data[23]), 999, 999);
            if (data[24] != "")
                Moves[3] = new PokemonMove(4, Convert.ToInt32(data[24]), 999, 999);

            OriginalTrainer = data[38];
            Form = data[39];

            _nature = data[20];
            if (data[35] != "")
                Ability = new PokemonAbility(Convert.ToInt32(data[35]));
            if (data[37] != "")
                AbilityLength = Convert.ToInt32(data[37]);
            if (data[1] != "")
                Happiness = Convert.ToInt32(data[1]);
            ItemHeld = data[34];
            if (data[2] != "")
                Stats = new PokemonStats(data, 2, MaxHealth);
            Ailment = data[36];
            if (data[14] != "")
                IV = new PokemonStats(data, 14);
            if (data[8] != "")
                EV = new PokemonStats(data, 8);

            Type1 = TypesManager.Instance.Type1[Id];
            Type2 = TypesManager.Instance.Type2[Id];
        }
        public void UpdateHealth(int max, int current)
        {
            MaxHealth = max;
            CurrentHealth = current;
        }

        public string[] GetMoveNames() => Moves.Where(m => m.Id > 0).Select(m => m.Name).ToArray();
    }
}
