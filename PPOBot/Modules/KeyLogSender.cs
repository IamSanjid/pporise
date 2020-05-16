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
            { Actions.USING_ITEM, new[]{ 52, 477, 323, 852 } },                                                  /* using item outside of battle */
            { Actions.USING_MOVE, new[]{ 939, 346, 957, 492 } },                                                  /* Re-learning move */
            { Actions.SWAPPING_POKEMON, new[] { 306, 154, 1118, 154 } },                                            /* swapping pokemon */
            { Actions.USING_MOVE | Actions.IN_BATTLE, new[]{ 507, 569, 522, 572 } },                              /* using move in a battle */
            { Actions.USING_ITEM | Actions.IN_BATTLE, new[] { 577, 639, 731, 697 } },                              /* using item in battle */
            { Actions.SWAPPING_POKEMON | Actions.IN_BATTLE, new[] { 474, 623, 850, 701 } },                        /* swapping pokemon in battle */
            { Actions.USING_ITEM | Actions.USING_ON_POKEMON, new[] { 52, 477, 1118, 154 } },                       /* using item on pokemon outside of battle */

            { Actions.MOVING_UP, "UP" },
            { Actions.MOVING_DOWN, "DOWN" },
            { Actions.MOVING_LEFT, "LEFT" },
            { Actions.MOVING_RIGHT, "RIGHT" },
            { Actions.ACTION_KEY, "SPACE" }
        };

        private readonly GameClient _client;
        private string _requestId = "";

        public KeyLogSender(GameClient client)
        {
            _client = client;
            _client.AskedForKeyLogs += Game_AskedForKeyLogs;
            _client.NoKeyLogsNeeded += Game_NoKeyLogsNeeded;
            _client.PerformingAction += Game_PerformingAction;
        }

        private void Game_PerformingAction(uint action)
        {
            if (string.IsNullOrEmpty(_requestId))
            {
                return;
            }

            var key = MAPPED_KEYS[action];
            if (key == null)
            {
                _client.PrintSystemMessage("Invalid action: " + action);
                return;
            }
            if (key is int[] pos)
            {
                int minX = pos[0], minY = pos[1], maxX = pos[2], maxY = pos[3];
                
                int x = _client.Rand.Next(minX, maxX + 1);
                int y = _client.Rand.Next(minY, maxY + 1);

                _client.SendMouseLogs(x, y, _requestId);
            }
            else
            {
                _client.SendKeyLog(key.ToString(), _requestId);
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
