using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPOProtocol;

namespace PPOBot
{
    public enum RockPriority
    {
        Gold = 9,
        Rainbow = 8,
        Dark = 7,
        Pale = 5,
        Prism = 4,
        Green = 3,
        Blue = 2,
        Red = 1,
        None = 0
    }

    public static class RockProrityExtensions
    {
        public static RockPriority Priority(this MiningObject rock)
        {
            return PriorityFromColor(rock.Color);
        }
        public static int RequiredLevel(this RockPriority priority)
        {
            switch (priority)
            {
                case RockPriority.Red:
                    return 1;
                case RockPriority.Blue:
                    return 10;
                case RockPriority.Green:
                    return 20;
                case RockPriority.Prism:
                    return 35;
                case RockPriority.Pale:
                    return 50;
                case RockPriority.Dark:
                    return 65;
                case RockPriority.Rainbow:
                    return 85;
                case RockPriority.Gold:
                    return 1; // *Gold have a 1/200 chance to appear on any deposit except Rainbow Deposit...
                default:
                    return 5;
            }
        }
        public static RockPriority PriorityFromColor(string color)
        {
            if (!Enum.TryParse(color.Trim(), out RockPriority rock)) return RockPriority.None;
            return rock;
        }
#if DEBUG
        public static int CountPriorityPower(RockPriority pr)
        {
            return (int)pr;
        }
#endif
    }
}
