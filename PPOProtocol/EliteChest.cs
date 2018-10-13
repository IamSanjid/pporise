using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class EliteChest
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool Opened { get; private set; }
        public EliteChest(int x, int y)
        {
            X = x;
            Y = y;
            Opened = false;
        }

        public EliteChest(string[] data)
        {
            X = Convert.ToInt32(data[0]);
            Y = Convert.ToInt32(data[1]);
            Opened = false;
        }

        public void UpdateChestOpen(bool isOpen)
        {
            Opened = isOpen;
        }
    }
}
