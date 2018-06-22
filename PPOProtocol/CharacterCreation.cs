using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public enum Gender
    {
        Male,
        Female
    }
    public enum BodyColor
    {
        Black,
        Brown,
        White,
        Pale,
        Tan,
        Yellow
    }
    internal class CharacterCreation
    {
        private Random _rand;
        public string Body => Gender.ToString() + BodyColor.ToString();
        public Gender Gender => _rand.Next(0, 1) == 1 ? Gender.Female : Gender.Male;
        public readonly BodyColor BodyColor = BodyColor.Tan;
        public int HairColorR => _rand.Next(35, 135);
        public int HairColorG => _rand.Next(35, 135);
        public int HairColorB => _rand.Next(35, 135);

        public int EyesColorR => _rand.Next(35, 135);
        public int EyesColorG => _rand.Next(35, 135);
        public int EyesColorB => _rand.Next(35, 135);

        public int ShirtColorR => _rand.Next(35, 135);
        public int ShirtColorG => _rand.Next(35, 135);
        public int ShirtColorB => _rand.Next(35, 135);

        public int PantsColorR => _rand.Next(35, 135);
        public int PantsColorG => _rand.Next(35, 135);
        public int PantsColorB => _rand.Next(35, 135);

        public readonly string Hair;
        public readonly string Face;
        public readonly string Eyes;
        public readonly string Shirt;
        public readonly string Pants;

        internal CharacterCreation(Random rand)
        {
            try
            {
                _rand = rand;
                var listHair = new ArrayList();
                var listEyes = new ArrayList();
                var listFace = new ArrayList();
                var listShirt = new ArrayList();
                var listPants = new ArrayList();
                for (var i = 1; i <= 19; i++)
                {
                    if (Gender == Gender.Male && i <= 17)
                    {
                        if (i <= 16)
                            listHair.Add(Gender.ToString() + i);
                        else if (i == 17)
                            listHair.Add("Bald");
                    }
                    else if (Gender == Gender.Female)
                    {
                        if (i <= 18)
                            listHair.Add(Gender.ToString() + i);
                        else
                            listHair.Add("Bald");
                    }
                }
                Hair = listHair[_rand.Next(0, listHair.Count - 1)].ToString();
                if (Gender == Gender.Male)
                {
                    listEyes.AddRange(new[] { "Both1", "Both2", "Both3", "Both4", "Both5" });
                    listFace.AddRange(new[] { "", "Goatee1", "Goatee2" });
                    listShirt.AddRange(new[] { "Male1", "Male2", "Male3" });
                    listPants.AddRange(new[] { "Male1", "Male2", "Male3" });
                }
                else
                {
                    listEyes.AddRange(new[] { "Both1", "Both2", "Both3", "Both4", "Both5", "Female1", "Female2" });
                    listFace.AddRange(new[] { "", "Blush" });
                    listShirt.AddRange(new[] { "Female1", "Female2", "Female3" });
                    listPants.AddRange(new[] { "Female1", "Female2", "Female3", "Female4" });
                }

                Face = listFace[_rand.Next(0, listFace.Count - 1)].ToString();
                Eyes = listEyes[_rand.Next(0, listEyes.Count - 1)].ToString();
                Shirt = listShirt[_rand.Next(0, listShirt.Count - 1)].ToString();
                Pants = listPants[_rand.Next(0, listPants.Count - 1)].ToString();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
