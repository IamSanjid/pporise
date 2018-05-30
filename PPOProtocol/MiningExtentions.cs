using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOProtocol
{
    public class MiningExtentions
    {
        public int MiningLevel { get; set; }
        public int CurrentMiningXp { get; set; }
        public int TotalMiningXp { get; set; }
        public MiningExtentions(string mLevel, string mCurrentXp, string mTotalXp)
        {
            MiningLevel = Convert.ToInt32(mLevel);
            CurrentMiningXp = Convert.ToInt32(mCurrentXp);
            TotalMiningXp = Convert.ToInt32(mTotalXp);
        }
    }
}
