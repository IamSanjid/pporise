using System;
using Network;

namespace PPOProtocol
{
    public class Battle
    {
        private readonly GameClient _client;
        public Battle(string[] data, bool isWildBattle, bool disconnect = false, GameClient client = null)
        {
            IsWildBattle = isWildBattle;
            if (IsWildBattle)
            {
                BattleType = "wild";
                IsTrapped = false;
                _client = client;
                var activePokemon = 0;
                if (_client?.Team != null && _client.Team.Count > 0)
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
                WildPokemon = new WildPokemon(spl);
                if (_client != null)
                {
                    WildPokemon.IsRare = _client.HasEncounteredRarePokemon;
                    if (disconnect)
                    {
                        BattleType = "wild";
                    }

                    if (_client?.Team != null && (WildPokemon.EncryptedAbility ==
                                                  Connection.CalcMd5(
                                                      new char[23] + "asion1asfonapsfobq1n12iofrasnfra") &&
                                                  _client?.Team[activePokemon].Type1 != PokemonType.Steel &&
                                                  _client?.Team[activePokemon].Type2 != PokemonType.Steel))
                    {
                        IsTrapped = true;
                    }
                    else if (_client?.Team != null && (WildPokemon.EncryptedAbility ==
                                                       Connection.CalcMd5(
                                                           new char[42] + "asion1asfonapsfobq1n12iofrasnfra") &&
                                                       _client?.Team[activePokemon].Type1 != PokemonType.Fire &&
                                                       _client?.Team[activePokemon].Type2 != PokemonType.Fire))
                    {
                        IsTrapped = true;
                    }

                    IsAlreadyCaught = _client?.PokemonCaught[WildPokemon.Id - 1] == "true";
                }

                IsDungeonBattle = data[8] == "1";

                if (data[6] != null && data[6] != "-1")
                {
                    BattleType = "wild";
                }
                else if (data.Length > 9)
                {
                    if (data[10] == "1")
                        BattleType = "wild";
                }
            }
        }

        public void UpdateBattle(string[] resObj)
        {
            ActivePokemon = Convert.ToInt32(resObj[7]);
            if (IsWildBattle)
            {
                FullWildPokemon = _client.ParseOnePokemon(resObj[12]);
                FullWildPokemon.IsRare = _client.HasEncounteredRarePokemon;
                if (FullWildPokemon.Ability.Id == 23 && _client?.Team[ActivePokemon].Type1 != PokemonType.Steel &&
                    _client?.Team[ActivePokemon].Type2 != PokemonType.Steel &&
                    _client?.Team[ActivePokemon].Ability.Id != 23)
                {
                    IsTrapped = true;
                }

                if (FullWildPokemon.Ability.Id == 42 && _client?.Team[ActivePokemon].Type1 != PokemonType.Fire &&
                    _client?.Team[ActivePokemon].Type2 != PokemonType.Fire &&
                    _client?.Team[ActivePokemon].Ability.Id != 23 &&
                    _client?.Team[ActivePokemon].Type1 != PokemonType.Steel &&
                    _client?.Team[ActivePokemon].Type2 != PokemonType.Steel)
                {
                    IsTrapped = true;
                }

                IsAlreadyCaught = _client.PokemonCaught[FullWildPokemon.Id - 1] == "true";
            }

            ProcessBattleMessage(resObj[10]);
        }
        private void ProcessBattleMessage(string str)
        {
            if (str.ToLowerInvariant().Contains("can not run away") ||
                str.ToLowerInvariant().Contains("can not switch"))
                IsTrapped = true;
            else
                IsTrapped = false;
        }
        public int ActivePokemon { get; private set; }
        public bool ActivePokemonError { get; }
        public string BattleType { get; } = "";
        public WildPokemon WildPokemon { get; }
        public Pokemon FullWildPokemon { get; private set; }
        public bool IsTrapped { get; private set; }
        public bool IsAlreadyCaught { get; private set; }
        public bool IsDungeonBattle { get; }
        public bool IsWildBattle { get; }
    }
}
