using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class PortablePc
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Owner { get; set; }

        public double DistanceToPosition(int x1, int y1)
        {
            int dX = x1 - X;
            int dY = y1 - Y;

            return Math.Sqrt((dX * dX) + (dY * dY));
        }
    }
}
