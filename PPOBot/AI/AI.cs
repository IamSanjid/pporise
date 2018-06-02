using System.Collections.Generic;
using PPOProtocol;

// ReSharper disable once CheckNamespace
namespace PPOBot
{
    // ReSharper disable once InconsistentNaming
    public class AI
    {
        protected const int DoubleEdge = 38;
        protected const int DragonRage = 82;
        protected const int DreamEater = 138;
        protected const int Explosion = 153;
        protected const int FalseSwipe = 206;
        protected const int NightShade = 101;
        protected const int Psywave = 149;
        protected const int SeismicToss = 69;
        protected const int Selfdestruct = 120;
        protected const int Synchronoise = 485;

        protected readonly HashSet<int> LevitatingPokemons = new HashSet<int>
        {
            92, 93, 94, 109, 110, 200, 201, 329, 330, 337, 338, 343, 344, 355, 358, 380, 381,
            429, 433, 436, 437, 455, 479, 480, 481, 482, 487, 488, 602, 603, 604, 615, 635
        };

        protected AI(GameClient client)
        {
            Client = client;
            Client.RockDepleted += _client_RockDepleted;
            Client.RockRestored += _client_RockRestored;
        }

        public readonly GameClient Client;

        protected static class RockPrority
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

            public static RockPriority PriorityFromColor(string color)
            {
                switch (color.ToLowerInvariant().Trim())
                {
                    case "gold":
                        return RockPriority.Gold;
                    case "rainbow":
                        return RockPriority.Rainbow;
                    case "dark":
                        return RockPriority.Dark;
                    case "pale":
                        return RockPriority.Pale;
                    case "prism":
                        return RockPriority.Prism;
                    case "green":
                        return RockPriority.Green;
                    case "blue":
                        return RockPriority.Blue;
                    case "red":
                        return RockPriority.Red;
                    default:
                        return RockPriority.None;
                }
            }

            public static int CountPriorityPower(RockPriority pr)
            {
                return (int)pr;
            }
        }
        protected bool IsMoveOffensive(PokemonMove move, MovesManager.MoveData moveData)
        {
            return moveData.Power > 0 || move.Id == DragonRage || move.Id == SeismicToss || move.Id == NightShade || move.Id == Psywave;
        }
        protected bool IsMoveOffensive(PokemonMove move, MovesManager.MoveData moveData, PokemonType opponentType1, PokemonType opponentType2)
        {
            return moveData.Power > 0 ||
                   move.Id == DragonRage && opponentType1 != PokemonType.Fairy && opponentType2 != PokemonType.Fairy ||
                   move.Id == SeismicToss || move.Id == NightShade || move.Id == Psywave;
        }
        protected double ApplySpecialEffects(PokemonMove move, double power, Pokemon activePokemon)
        {
            if (move.Id == DragonRage)
            {
                if (Client.ActiveBattle.FullWildPokemon != null)
                    return Client.ActiveBattle.FullWildPokemon.CurrentHealth <= 40 ? 10000.0 : 1.0;
                return Client.ActiveBattle.WildPokemon.CurrentHealth <= 40 ? 10000.0 : 1.0;
            }

            if (move.Id == SeismicToss || move.Id == NightShade)
            {
                if (Client.ActiveBattle.FullWildPokemon != null)
                    return Client.ActiveBattle.FullWildPokemon.CurrentHealth <= activePokemon.Level ? 10000.0 : 1.0;
                return Client.ActiveBattle.WildPokemon.CurrentHealth <= activePokemon.Level ? 10000.0 : 1.0;
            }

            if (move.Id == Psywave)
            {
                if (Client.ActiveBattle.FullWildPokemon != null)
                    return Client.ActiveBattle.FullWildPokemon.CurrentHealth <= (activePokemon.Level / 2) ? 10000.0 : 1.0;
                return Client.ActiveBattle.WildPokemon.CurrentHealth <= (activePokemon.Level / 2) ? 10000.0 : 1.0;
            }

            if (move.Id == FalseSwipe)
            {
                return 0.1;
            }

            return power;
        }
        protected virtual void _client_RockRestored(MiningObject rock) { }
        protected virtual void _client_RockDepleted(MiningObject rock) { }
    }
}
