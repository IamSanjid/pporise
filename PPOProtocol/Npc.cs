using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class Npc
    {
        public int Id { get; }
        public string Name { get; }
        public bool IsBattler { get; }
        public bool CanBattle { get; set; }
        public Npc(string[] data)
        {
            Id = Convert.ToInt32(data[0]);
            Name = data[1];
            IsBattler = Convert.ToInt32(data[2]) == 1;
            CanBattle = Convert.ToInt32(data[3]) == 1;
        }
    }
}
