using System;

namespace PPOBot.Modules
{
    public class AutoReconnector
    {
        public const int MinDelay = 90;
        public const int MaxDelay = 240;

        public event Action<bool> StateChanged;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;

            set
            {
                if (_isEnabled == value)
                {
                    return;
                }

                _isEnabled = value;
                StateChanged?.Invoke(value);
            }
        }

        private readonly BotClient _bot;
        public bool Reconnecting;
        private DateTime _autoReconnectTimeout;

        public AutoReconnector(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game is null) return;
            _bot.Game.Disconnected += Client_ConnectionClosed;
            _bot.Game.LoggingError += Client_ConnectionClosed;
            _bot.Game.Connected += Client_LoggedIn;
        }

        public void Update()
        {
            if (IsEnabled != true || !Reconnecting || (_bot.Game != null && _bot.Game.IsConnected))
            {
                return;
            }

            if (_autoReconnectTimeout >= DateTime.UtcNow)
            {
                return;
            }

            _bot.PrintLogMessage("Reconnecting...");
            _bot.Login(_bot.Account);
            _autoReconnectTimeout = DateTime.UtcNow.AddSeconds(_bot.Rand.Next(MinDelay, MaxDelay + 1));
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            if (!IsEnabled) return;
            Reconnecting = true;
            var seconds = _bot.Rand.Next(MinDelay, MaxDelay + 1);
            _autoReconnectTimeout = DateTime.UtcNow.AddSeconds(seconds);
            _bot.PrintLogMessage("Reconnecting in " + seconds + " seconds.");
        }

        private void Client_LoggedIn()
        {
            if (!Reconnecting) return;
            _bot.Start();
            Reconnecting = false;
        }
    }
}
