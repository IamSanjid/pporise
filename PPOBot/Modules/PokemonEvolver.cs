using PPOProtocol;
using System;

namespace PPOBot.Modules
{
    public class PokemonEvolver
    {
        public event Action<bool> StateChanged;

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get =>_isEnabled; 
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                StateChanged?.Invoke(value);
            }
        }

        private readonly BotClient _bot;

        private ProtocolTimeout _evolutionTimeout = new ProtocolTimeout();

        public PokemonEvolver(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        public bool Update()
        {
            if (_evolutionTimeout.IsActive && !_evolutionTimeout.Update())
            {
                if (IsEnabled)
                {
                    _bot.Game.SendAcceptEvolution();
                }
                else
                {
                    _bot.Game.SendCancelEvolution();
                }
                return true;
            }
            return _evolutionTimeout.IsActive;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.Evolving += Game_Evolving;
            }
        }

        private void Game_Evolving()
        {
            _evolutionTimeout.Set(_bot.Game.Rand.Next(2000, 3000));
        }
    }
}
