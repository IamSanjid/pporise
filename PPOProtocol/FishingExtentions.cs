using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class FishingExtentions
    {
        public int FishingLevel { get; set; }
        public int CurrentFishingXp { get; set; }
        public int TotalFishingXp { get; set; }
        public FishingExtentions(string fLevel, string cFishingXp, string totalFishingXp)
        {
            FishingLevel = Convert.ToInt32(fLevel);
            CurrentFishingXp = Convert.ToInt32(cFishingXp);
            TotalFishingXp = Convert.ToInt32(totalFishingXp);
        }
    }
}
