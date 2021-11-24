using System;

namespace PPOProtocol
{
    public class Battle
    {
        private readonly GameClient _client;
        public Battle(string[] data, bool isWildBattle, bool disconnect = false, GameClient client = null)
        {
            IsWildBattle = isWildBattle;
            BattleType = "wild";
            IsTrapped = false;
            _client = client;
            var activePokemon = 0;
            if (_client?.Team != null && _client?.Team.Count > 0)
                for (var i = 0; i < _client.Team.Count; i++)
                    if (_client.Team[i].CurrentHealth > 0)
                    {
                        activePokemon = i;
                        break;
                    }
                    else if (i == _client.Team.Count - 1)
                    {
                        ActivePokemonError = true;
                        activePokemon = 0;
#if DEBUG
                        Console.WriteLine("error, all pokemon have 0 hp");
#endif
                        break;
                    }

            ActivePokemon = activePokemon;
            var spl = data[3].Split(',');
            WildPokemon = new WildPokemon();
            WildPokemon.New(spl);

            if (disconnect)
            {
                BattleType = "wild";
            }

            if (_client?.Team != null && (WildPokemon.EncryptedAbility
                                          == ObjectSerilizer.CalcMd5(
                                              new char[23] + "asion1asfonapsfobq1n12iofrasnfra") &&
                                          _client.Team[activePokemon].Type1 != PokemonType.Steel &&
                                          _client.Team[activePokemon].Type2 != PokemonType.Steel))
            {
                IsTrapped = true;
            }
            else if (_client?.Team != null && (WildPokemon.EncryptedAbility ==
                                               ObjectSerilizer.CalcMd5(
                                                   new char[42] + "asion1asfonapsfobq1n12iofrasnfra") &&
                                               _client.Team[activePokemon].Type1 != PokemonType.Fire &&
                                               _client.Team[activePokemon].Type2 != PokemonType.Fire))
            {
                IsTrapped = true;
            }

            IsAlreadyCaught = _client?.PokemonCaught[WildPokemon.Id - 1] == "true";

            IsDungeonBattle = data[8] == "1";

            if (data[6] != null && data[6] != "-1")
            {
                BattleType = "wild";
                // Fishing Wild Battle
            }
            else if (data.Length > 9)
            {
                if (data[10] == "1")
                {
                    BattleType = "wild";
                    IsWildBattle = true;
                }
                // Normal/Mining Wild Battle
            }
            if (_client.HasEncounteredRarePokemon)
                WildPokemon.IsRare = true;

            var currentPPs = data[9].Split(',');
            for (int i = 0; i < currentPPs.Length; i++)
            {
                _client.Team[ActivePokemon].Moves[i].CurrentPoints = Convert.ToInt32(currentPPs[i]);
                _client.Team[ActivePokemon].Moves[i].MaxPoints = Convert.ToInt32(currentPPs[i]);
            }
        }

        public void UpdateBattle(string[] resObj)
        {
            BattleHasWon = resObj[3] == "W";
            BattleEnded = !BattleHasWon && resObj[6] == "1";
            ActivePokemon = Convert.ToInt32(resObj[7]);
            
            var data = GameClient.ParseArray(resObj[12]);
            int lastId = Convert.ToInt32(data[5]);
            if (lastId != WildPokemon.Id)
                WildPokemon = new WildPokemon();
            WildPokemon.Update(data);

            if (_client.HasEncounteredRarePokemon)
                WildPokemon.IsRare = true;

            IsTrapped |= (WildPokemon.Ability.Id == 23 && _client?.Team[ActivePokemon].Type1 != PokemonType.Steel
                && _client?.Team[ActivePokemon].Type2 != PokemonType.Steel
                && _client?.Team[ActivePokemon].Ability.Id != 23);

            IsTrapped |= (WildPokemon.Ability.Id == 42 && _client?.Team[ActivePokemon].Type1 != PokemonType.Fire
                && _client?.Team[ActivePokemon].Type2 != PokemonType.Fire
                && _client?.Team[ActivePokemon].Ability.Id != 23
                && _client?.Team[ActivePokemon].Type1 != PokemonType.Steel
                && _client?.Team[ActivePokemon].Type2 != PokemonType.Steel);

            IsAlreadyCaught = _client.PokemonCaught[WildPokemon.Id - 1] == "true";

            var currentPPs = resObj[13].Split(',');
            for (int i = 0; i < currentPPs.Length; i++)
            {
                _client.Team[ActivePokemon].Moves[i].CurrentPoints = Convert.ToInt32(currentPPs[i]);
            }
            var maxPPs = resObj[14].Split(',');
            for (int i = 0; i < maxPPs.Length; i++)
            {
                _client.Team[ActivePokemon].Moves[i].MaxPoints = Convert.ToInt32(maxPPs[i]);
            }

            ProcessBattleMessage(resObj[10]);
        }

        private void ProcessBattleMessage(string str)
        {
            IsTrapped = str.IndexOf("can not run away", StringComparison.InvariantCultureIgnoreCase) >= 0
                || str.IndexOf("can not switch", StringComparison.InvariantCultureIgnoreCase) >= 0 ? true : false;
        }

        public int ActivePokemon { get; private set; }
        public bool ActivePokemonError { get; }
        public string BattleType { get; } = "";
        public WildPokemon WildPokemon { get; private set; }
        public bool IsTrapped { get; private set; }
        public bool IsAlreadyCaught { get; private set; }
        public bool IsDungeonBattle { get; }
        public bool IsWildBattle { get; }
        public bool LostBattle { get; set; } = false;
        public bool BattleHasWon { get; set; } = false;
        public bool BattleEnded { get; set; } = false;
    }
}
