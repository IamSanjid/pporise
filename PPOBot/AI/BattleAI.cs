using PPOProtocol;

// ReSharper disable once CheckNamespace
namespace PPOBot
{
    // ReSharper disable once InconsistentNaming
    public class BattleAI : AI
    {
        public BattleAI(GameClient client) : base(client) {}
        public bool Run()
        {
            if (!Client.Battle) return false;
            if (ActivePokemon.CurrentHealth <= 0) return false;
            return Client.Run();
        }
        public int UsablePokemonsCount
        {
            get
            {
                int usablePokemons = 0;
                foreach (var pokemon in Client.Team)
                {
                    if (IsPokemonUsable(pokemon))
                    {
                        usablePokemons++;
                    }
                }
                return usablePokemons;
            }
        }
        public bool Attack()
        {
            if (!IsPokemonUsable(ActivePokemon)) return false;
            return UseAttack(true);
        }

        public bool WeakAttack()
        {
            if (!IsPokemonUsable(ActivePokemon)) return false;
            return UseAttack(false);
        }
        public bool SendAnyPokemon()
        {
            if (Client.IsTrapped) return false;
            if (!Client.Battle) return false;
            foreach (var pk in Client.Team)
            {
                if (pk.CurrentHealth > 0 && pk != ActivePokemon)
                    return SendPokemon(pk.Uid);
            }
            return false;
        }
        public Pokemon ActivePokemon => Client.Team[Client.ActiveBattle.ActivePokemon];
        public bool UseMove(string moveName)
        {
            if (ActivePokemon.CurrentHealth == 0) return false;

            moveName = moveName.ToUpperInvariant();
            foreach (var move in ActivePokemon.Moves)
            {
                if (move.Name.ToUpperInvariant() == moveName)
                {
                    Client.UseAttack(move.Position - 1);
                    return true;
                }
            }
            return false;
        }
        public bool SendPokemon(int index)
        {
            if (Client.IsTrapped) return false;
            if (!Client.Battle) return false;
            if (index < 1 || index > Client.Team.Count) return false;
            Pokemon pokemon = Client.Team[index - 1];
            if (pokemon.CurrentHealth > 0 && pokemon != ActivePokemon)
            {
                Client.ChangePokemon(pokemon.Uid - 1);
                return true;
            }
            return false;
        }
        public bool SendUsablePokemon()
        {
            if (Client.IsTrapped) return false;
            foreach (Pokemon pokemon in Client.Team)
            {
                if (IsPokemonUsable(pokemon) && pokemon != ActivePokemon)
                {
                    Client.ChangePokemon(pokemon.Uid - 1);
                    return true;
                }
            }
            return false;
        }
        public bool UseAnyMove()
        {
            if (ActivePokemon.CurrentHealth == 0) return false;
            if (!Client.Battle) return false;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                     Client.UseAttack(i);
                    return true;
                }
            }
            return false;
        }
        public bool UseMove(int index)
        {
            if (!Client.Battle || ActivePokemon.CurrentHealth == 0 || index < 1 || index > 4)
            {
                return false;
            }

            Client.UseAttack(index - 1);
            return true;
        }
        private bool UseAttack(bool useBestAttack)
        {
            PokemonMove bestMove = null;
            var bestIndex = 0;
            double bestPower = 0;

            PokemonMove worstMove = null;
            var worstIndex = 0;
            double worstPower = 0;

            for (var i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                var move = ActivePokemon.Moves[i];
                if (move.CurrentPoints == 0) continue;
                if (move.Name is null || move.Data is null) continue;

                var moveData = MovesManager.Instance.GetMoveData(move.Id);
                if (move.Id == DreamEater && (Client.ActiveBattle.FullWildPokemon != null ? Client.ActiveBattle.FullWildPokemon.Status.ToUpperInvariant() != "SLEEP" : Client.ActiveBattle.WildPokemon.Status.ToUpperInvariant() != "SLEEP"))
                {
                    continue;
                }
                if (move.Id == Explosion || move.Id == Selfdestruct ||
                    (move.Id == DoubleEdge && ActivePokemon.CurrentHealth < (Client.ActiveBattle.FullWildPokemon?.MaxHealth / 3 ?? Client.ActiveBattle.WildPokemon.MaxHealth / 3)))
                {
                    continue;
                }

                var attackType = PokemonTypeExtensions.FromName(moveData.Type);

                var playerType1 = TypesManager.Instance.Type1[ActivePokemon.Id];
                var playerType2 = TypesManager.Instance.Type2[ActivePokemon.Id];

                var opponentType1 = TypesManager.Instance.Type1[Client.ActiveBattle.WildPokemon.Id];
                var opponentType2 = TypesManager.Instance.Type2[Client.ActiveBattle.WildPokemon.Id];

                if (!IsMoveOffensive(move, moveData, opponentType1, opponentType2)) continue;

                var accuracy = moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy;

                var power = moveData.Power * accuracy;

                if (attackType == playerType1 || attackType == playerType2)
                {
                    power *= 1.5;
                }

                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

                if (attackType == PokemonType.Ground && LevitatingPokemons.Contains(Client.ActiveBattle.WildPokemon.Id))
                {
                    power = 0;
                }

                power = ApplySpecialEffects(move, power, ActivePokemon);

                if (move.Id == Synchronoise)
                {
                    if (playerType1 != opponentType1 && playerType1 != opponentType2 &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType1) &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType2))
                    {
                        power = 0;
                    }
                }

                if (power < 0.01) continue;

                if (bestMove == null || power > bestPower)
                {
                    bestMove = move;
                    bestPower = power;
                    bestIndex = i;
                }

                if (worstMove == null || power < worstPower)
                {
                    worstMove = move;
                    worstPower = power;
                    worstIndex = i;
                }
            }

            if (useBestAttack && bestMove != null)
            {
                Client.UseAttack(bestIndex);
                return true;
            }
            if (!useBestAttack && worstMove != null)
            {
                Client.UseAttack(worstIndex);
                return true;
            }
            return false;
        }

        public bool IsPokemonUsable(Pokemon pokemon)
        {
            if (pokemon.CurrentHealth > 0)
            {
                foreach (PokemonMove move in pokemon.Moves)
                {
                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);
                    if (IsMoveOffensive(move, moveData) &&
                        move.Id != DreamEater && move.Id != Synchronoise && move.Id != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
