using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class MiningObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsMined { get; set; }
        public string Color { get; set; }
        public bool IsGoldMember { get; set; }
        public MiningObject(string[] data)
        {
            X = Convert.ToInt32(data[0]);
            Y = Convert.ToInt32(data[1]);
            Color = data[2];
            IsMined = data[3] == "0";
            IsGoldMember = data[4] != "0";
        }
    }
}
