﻿using System.Collections.Generic;
using PPOProtocol;

// ReSharper disable once CheckNamespace
namespace PPOBot
{
    // ReSharper disable once InconsistentNaming
    public class BattleAI
    {
        private const int DoubleEdge = 38;
        private const int DragonRage = 82;
        private const int DreamEater = 138;
        private const int Explosion = 153;
        private const int FalseSwipe = 206;
        private const int NightShade = 101;
        private const int Psywave = 149;
        private const int SeismicToss = 69;
        private const int Selfdestruct = 120;
        private const int Synchronoise = 485;

        public readonly HashSet<int> _levitatingPokemons = new HashSet<int>
        {
            92, 93, 94, 109, 110, 200, 201, 329, 330, 337, 338, 343, 344, 355, 358, 380, 381,
            429, 433, 436, 437, 455, 479, 480, 481, 482, 487, 488, 602, 603, 604, 615, 635
        };

        private bool IsMoveOffensive(PokemonMove move, MovesManager.MoveData moveData)
        {
            return moveData?.Power > 0 || move.Id == DragonRage || move.Id == SeismicToss || move.Id == NightShade || move.Id == Psywave;
        }

        private double ApplySpecialEffects(PokemonMove move, double power)
        {
            if (move.Id == DragonRage)
            {
                return _client.ActiveBattle.WildPokemon.CurrentHealth <= 40 ? 10000.0 : 1.0;
            }

            if (move.Id == SeismicToss || move.Id == NightShade)
            {
                return _client.ActiveBattle.WildPokemon.CurrentHealth <= ActivePokemon.Level ? 10000.0 : 1.0;
            }

            if (move.Id == Psywave)
            {
                return _client.ActiveBattle.WildPokemon.CurrentHealth <= (ActivePokemon.Level / 2) ? 10000.0 : 1.0;
            }

            if (move.Id == FalseSwipe)
            {
                return 0.1;
            }

            return power;
        }

        private readonly GameClient _client;

        public BattleAI(GameClient client)
        {
            _client = client;
        }

        public bool Run()
        {
            if (!_client.IsInBattle) return false;
            if (_client.ActiveBattle.IsDungeonBattle) return false;
            if (ActivePokemon.CurrentHealth <= 0) return false;
            return _client.Run();
        }

        public int UsablePokemonsCount
        {
            get
            {
                var usablePokemons = 0;
                foreach (var pokemon in _client.Team)
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
            if (_client.IsTrapped) return false;
            if (!_client.IsInBattle) return false;
            foreach (var pk in _client.Team)
            {
                if (pk.CurrentHealth > 0 && pk != ActivePokemon)
                    return SendPokemon(pk.Uid);
            }
            return false;
        }

        public Pokemon ActivePokemon => _client.Team[_client.ActiveBattle.ActivePokemon];

        public bool UseMove(string moveName)
        {
            if (ActivePokemon.CurrentHealth == 0) return false;

            moveName = moveName.ToUpperInvariant();
            foreach (var move in ActivePokemon.Moves)
            {
                if (move.Name.ToUpperInvariant() == moveName)
                {
                    _client.UseAttack(move.Position - 1);
                    return true;
                }
            }
            return false;
        }

        public bool SendPokemon(int index)
        {
            if (_client.IsTrapped) return false;
            if (!_client.IsInBattle) return false;
            if (index < 1 || index > _client.Team.Count) return false;
            Pokemon pokemon = _client.Team[index - 1];
            if (pokemon.CurrentHealth > 0 && pokemon != ActivePokemon)
            {
                _client.ChangePokemon(pokemon.Uid - 1);
                return true;
            }
            return false;
        }

        public bool SendUsablePokemon()
        {
            if (_client.IsTrapped) return false;
            foreach (Pokemon pokemon in _client.Team)
            {
                if (IsPokemonUsable(pokemon) && pokemon != ActivePokemon)
                {
                    _client.ChangePokemon(pokemon.Uid - 1);
                    return true;
                }
            }
            return false;
        }

        public bool UseAnyMove()
        {
            if (ActivePokemon.CurrentHealth == 0) return false;
            if (!_client.IsInBattle) return false;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                    _client.UseAttack(i);
                    return true;
                }
            }
            return false;
        }

        public bool UseMove(int index)
        {
            if (!_client.IsInBattle || ActivePokemon.CurrentHealth == 0 || index < 1 || index > 4)
            {
                return false;
            }

            _client.UseAttack(index - 1);
            return true;
        }

        private bool UseAttack(bool useBestAttack)
        {
            PokemonMove bestMove = null;
            int bestIndex = 0;
            double bestPower = 0;

            PokemonMove worstMove = null;
            int worstIndex = 0;
            double worstPower = 0;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints == 0) continue;

                MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);

                if (move.Id == DreamEater && _client.ActiveBattle.WildPokemon.Status != "SLEEP")
                {
                    continue;
                }

                if (move.Id == Explosion || move.Id == Selfdestruct ||
                    (move.Id == DoubleEdge && ActivePokemon.CurrentHealth < _client.ActiveBattle.WildPokemon.CurrentHealth / 3))
                {
                    continue;
                }

                if (!IsMoveOffensive(move, moveData)) continue;

                PokemonType attackType = PokemonTypeExtensions.FromName(moveData.Type);

                PokemonType playerType1 = TypesManager.Instance.Type1[ActivePokemon.Id];
                PokemonType playerType2 = TypesManager.Instance.Type2[ActivePokemon.Id];

                PokemonType opponentType1 = _client.ActiveBattle.WildPokemon.Type1;
                PokemonType opponentType2 = _client.ActiveBattle.WildPokemon.Type2;

                double accuracy = (moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy);

                double power = moveData.Power * accuracy;

                if (attackType == playerType1 || attackType == playerType2)
                {
                    power *= 1.5;
                }

                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

                if (attackType == PokemonType.Ground && _levitatingPokemons.Contains(_client.ActiveBattle.WildPokemon.Id))
                {
                    power = 0;
                }

                power = ApplySpecialEffects(move, power);

                if (move.Id == Synchronoise)
                {
                    if (playerType1 != opponentType1 && playerType1 != opponentType2 &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType1) &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType2))
                    {
                        power = 0;
                    }
                }

                if (move.Id == DragonRage)
                {
                    if (opponentType1 == PokemonType.Fairy || opponentType2 == PokemonType.Fairy)
                        power = 0;
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


            switch (useBestAttack)
            {
                case true when bestMove != null:
                    _client.UseAttack(bestIndex);
                    return true;
                case false when worstMove != null:
                    _client.UseAttack(worstIndex);
                    return true;
                default:
                    return false;
            }
        }

        public bool IsPokemonUsable(Pokemon pokemon)
        {
            if (pokemon.CurrentHealth > 0)
            {
                foreach (PokemonMove move in pokemon.Moves)
                {
                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);
                    if (move.CurrentPoints > 0 && IsMoveOffensive(move, moveData) &&
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
