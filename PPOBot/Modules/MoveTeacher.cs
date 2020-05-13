﻿using PPOProtocol;
using PPOBot;

namespace PPOBot.Modules
{
    public class MoveTeacher
    {
        public bool IsLearning { get; private set; }
        public int PokemonUid { get; private set; }
        public int MoveToForget { get; set; }

        private readonly BotClient _bot;

        private ProtocolTimeout _learningTimeout = new ProtocolTimeout();

        public MoveTeacher(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        public bool Update()
        {
            if (_learningTimeout.IsActive && !_learningTimeout.Update())
            {
                IsLearning = false;
                _bot.Game.LearnMove(MoveToForget);
                return true;
            }
            return _learningTimeout.IsActive;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.LearningMove += Game_LearningMove;
            }
        }

        private void Game_LearningMove(string moveName, int pokemonUid)
        {
            if (_bot.Game == null || _bot.Script == null) return;

            IsLearning = true;
            PokemonUid = pokemonUid;
            MoveToForget = -1;
            _learningTimeout.Set(_bot.Rand.Next(1000, 3000));

            _bot.Script.OnLearningMove(moveName, pokemonUid);
        }
    }
}
