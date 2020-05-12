using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPOProtocol;

namespace PPOBot.Modules
{
    public class KeyLogSender
    {
        private readonly Dictionary<uint, object> MAPPED_KEYS = new Dictionary<uint, object>()
        {
            { Actions.USING_ITEM, new Tuple<int, int, int, int>(52, 477, 323, 852) },                                                  /* using item outside of battle */
            { Actions.USING_MOVE, new Tuple<int, int, int, int>(939, 346, 957, 492) },                                                  /* Re-learning move */
            { Actions.SWAPPING_POKEMON, new Tuple<int, int, int, int>(306, 154, 1118, 154) },                                            /* swapping pokemon */
            { Actions.USING_MOVE | Actions.IN_BATTLE, new Tuple<int, int, int, int>(507, 569, 522, 572) },                              /* using move in a battle */
            { Actions.USING_ITEM | Actions.IN_BATTLE, new Tuple<int, int, int, int>(577, 639, 731, 697) },                              /* using item in battle */
            { Actions.SWAPPING_POKEMON | Actions.IN_BATTLE, new Tuple<int, int, int, int>(474, 623, 850, 701) },                        /* swapping pokemon in battle */
            { Actions.USING_ITEM | Actions.USING_ON_POKEMON, new Tuple<int, int, int, int>(52, 477, 1118, 154) },                       /* using item on pokemon outside of battle */
            { Actions.USING_ITEM | Actions.IN_BATTLE | Actions.USING_ON_POKEMON, new Tuple<int, int, int, int>(577, 639, 731, 697) },   /* using item on pokemon in battle */

            { Actions.MOVING_UP, "UP" },
            { Actions.MOVING_DOWN, "DOWN" },
            { Actions.MOVING_LEFT, "LEFT" },
            { Actions.MOVING_RIGHT, "RIGHT" },
            { Actions.ACTION_KEY, "SPACE" }
        };

        private readonly BotClient _bot;
        private string _requestId = "";

        public KeyLogSender(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.AskedForKeyLogs += Game_AskedForKeyLogs;
                _bot.Game.NoKeyLogsNeeded += Game_NoKeyLogsNeeded;
                _bot.Game.PerformingAction += Game_PerformingAction;
            }
        }

        private void Game_PerformingAction(uint action)
        {
            if (string.IsNullOrEmpty(_requestId))
            {
                return;
            }
            var key = MAPPED_KEYS[action];
            if (key is Tuple<int, int, int, int> pos)
            {
                int minX = pos.Item1, minY = pos.Item2, maxX = pos.Item3, maxY = pos.Item4;
                
                int x = _bot.Game.Rand.Next(minX, maxX + 1);
                int y = _bot.Game.Rand.Next(minY, maxY + 1);

                _bot.Game.SendMouseLogs(x, y, _requestId);
            }
            else
            {
                _bot.Game.SendKeyLog(key.ToString(), _requestId);
            }
        }

        private void Game_AskedForKeyLogs(string id)
        {
            _requestId = id;
        }

        private void Game_NoKeyLogsNeeded()
        {
            _requestId = "";
        }
    }
}
