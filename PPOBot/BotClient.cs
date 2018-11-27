using PPOBot.Modules;
using PPOBot.Scripting;
using PPOProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PPOBot
{
    public class BotClient
    {
        public enum State
        {
            Stopped,
            Started,
            Paused
        }

        public static BotClient Instance;

        public event Action<State> StateChanged;
        public event Action Connected;
        public event Action<Exception> Disconnected;
        public event Action<string> LogMessage;
        public event Action ClientChanged;
        public event Action<string, Brush> ColoredLogMessage;

        private bool _loginUpdate;
        public GameClient Game { get; private set; }
        // ReSharper disable once InconsistentNaming
        public BattleAI AI { get; private set; }
        // ReSharper disable once InconsistentNaming
        public MiningAI MiningAI { get; private set; }
        public PokemonEvolver PokemonEvolver { get; }
        public UserSettings Settings { get; }
        public MoveTeacher MoveTeacher { get; }
        public AutoReconnector AutoReconnector { get; }
        public AccountManager AccountManager { get; }
        public Account Account;
        public State Running { get; private set; }
        public BaseScript Script { get; private set; }
        private ProtocolTimeout _actionTimeout = new ProtocolTimeout();
        public Random Rand { get; } = new Random();
        public BotClient()
        {
            AccountManager = new AccountManager("Account");
            PokemonEvolver = new PokemonEvolver(this);
            MoveTeacher = new MoveTeacher(this);
            AutoReconnector = new AutoReconnector(this);
            Settings = new UserSettings();
            Account = null;
            Instance = this;
        }
        public void CancelInvokes()
        {
            if (Script != null)
                foreach (Invoker invoker in Script.Invokes)
                    invoker.Called = true;
        }

        public void CallInvokes()
        {
            if (Script != null)
            {
                for (int i = Script.Invokes.Count - 1; i >= 0; i--)
                {
                    if (Script.Invokes[i].Time < DateTime.UtcNow)
                    {
                        if (Script.Invokes[i].Called)
                        {
                            Script.Invokes.RemoveAt(i);
                        }
                        else
                        {
                            Script.Invokes[i].Call();
                        }
                    }
                }
            }
        }
        public void LogoutApi(bool isEnableAutoReconnector)
        {
            AutoReconnector.IsEnabled = isEnableAutoReconnector;
            Game.Logout();
        }
        private void Client_SystemMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnSystemMessage(message);
            }
        }
        private void Client_BattleMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnBattleMessage(message);
            }
        }
        private void Game_Disconnected(Exception obj)
        {
            Disconnected?.Invoke(obj);
            SetClient(null);
        }

        private void Game_Connected()
        {
            Connected?.Invoke();
        }

        public void PrintLogMessage(string obj)
        {
            LogMessage?.Invoke(obj);
        }
        public void C_LogMessage(string msg, Brush color)
        {
            ColoredLogMessage?.Invoke(msg, color);
        }
        public void Update()
        {
            AutoReconnector.Update();
            CallInvokes();
            if (Game is null)
            {
                return;
            }

            if (Running != State.Started)
            {
                return;
            }

            if (PokemonEvolver.Update()) return;
            if (MoveTeacher.Update()) return;
            if (MiningAI != null)
                if (MiningAI.Update())
                    return;

            if (Game.IsInactive && Game.IsMapLoaded && Game.IsConnected)
            {
                ExecuteNextAction();
            }
        }
        public void Start()
        {
            if (Game != null && Script != null && Running == State.Stopped)
            {
                _actionTimeout.Set();
                Running = State.Started;
                StateChanged?.Invoke(Running);
                Script.Start();
            }
        }

        public void Pause()
        {
            if (Game != null && Script != null && Running != State.Stopped)
            {
                if (Running == State.Started)
                {
                    Running = State.Paused;
                    StateChanged?.Invoke(Running);
                    Script.Pause();
                    Game.StopFishing();
                    Game.StopMining();
                }
                else
                {
                    Running = State.Started;
                    StateChanged?.Invoke(Running);
                    Script.Resume();
                }
            }
        }

        public void Stop()
        {
            if (Running != State.Stopped)
            {
                Running = State.Stopped;
                StateChanged?.Invoke(Running);
                Script?.Stop();
                if (Game != null)
                {
                    Game.StopFishing();
                    Game.StopMining();
                }
            }
        }
        private async void LoginUpdate()
        {
            await _gameConnection.Connect();
        }
        private void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            MiningAI = null;
            Stop();

            if (client != null)
            {
                Game.Timer = DateTime.Now;
                AI = new BattleAI(Game);
                MiningAI = new MiningAI(Game);
                client.LogMessage += PrintLogMessage;
                client.Connected += Game_Connected;
                client.Disconnected += Game_Disconnected;
                client.TeleportationOccuring += client_TeleportationOccuring;
                client.BattleMessage += Client_BattleMessage;
                client.LoggingError += Client_LoggingError;
                client.SystemMessage += Client_SystemMessage;
            }
            ClientChanged?.Invoke();
        }

        private void Client_LoggingError(Exception obj)
        {
            SetClient(null);
        }
        //Asynchronous
        public async Task LoadScript(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                //Asynchronous
                var input = await reader.ReadToEndAsync();

                var libs = new List<string>();
                if (Directory.Exists("Libs"))
                {
                    var files = Directory.GetFiles("Libs");
                    foreach (var file in files)
                    {
                        if (file.ToUpperInvariant().EndsWith(".LUA"))
                        {
                            using (var streamReader = new StreamReader(file))
                            {
                                //Asynchronous
                                libs.Add(await streamReader.ReadToEndAsync());
                            }
                        }
                    }
                }

                BaseScript script = new LuaScript(this, Path.GetFullPath(filename), input, libs);

                Stop();
                Script = script;
            }

            try
            {
                Script.ScriptMessage += Script_ScriptMessage;
                //Asynchronous
                await Script.Initialize();
            }
            catch (Exception)
            {
                Script = null;
                throw;
            }
        }
        private void ExecuteNextAction()
        {
            try
            {
                var executed = Script.ExecuteNextAction();
                if (!executed && Running != State.Stopped && !_actionTimeout.Update())
                {
                    C_LogMessage("No action executed: stopping the bot.", Brushes.OrangeRed);
                    Stop();
                }
                else if (executed)
                {
                    _actionTimeout.Set();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                PrintLogMessage("Error during the script execution: " + ex);
#else
                C_LogMessage("Error during the script execution: " + ex.Message, Brushes.OrangeRed);
#endif
                Stop();
            }
        }
        private void Script_ScriptMessage(string obj)
        {
            PrintLogMessage(obj);
        }

        private void client_TeleportationOccuring(string map, int x, int y)
        {
            var message = "Position updated: " + map + " (" + x + ", " + y + ")";
            MiningAI = new MiningAI(Game);
            if (map != Game.MapName)
            {
                message += " [WARNING, different map] /!\\";
            }
            else
            {
                int distance = GameClient.DistanceBetween(x, y, Game.PlayerX, Game.PlayerY);
                if (distance < 8 && distance > 0)
                {
                    message += " [OK, lag, distance=" + distance + "]";
                }
                else if (distance > 0 && distance >= 8)
                {
                    message += " [WARNING, distance=" + distance + "] /!\\";
                }

                if (Game.MapName != null && !(distance < 8 && distance > 0))
                {
                    message += " [OK]";
                }
            }
            if (message.Contains("[OK]"))
            {
                C_LogMessage(message, Brushes.Lime);
            }
            if (message.Contains("WARNING"))
            {
                C_LogMessage(message, Brushes.OrangeRed);
            }
        }

        public bool MoveToCell(int x, int y, string whatReason)
        {
            Pathfinding path = new Pathfinding(Game);
            bool result;

            if (Game.PlayerX == x && Game.PlayerY == y)
            {
                result = path.MoveToSameCell(whatReason);
            }
            else
            {
                result = path.MoveTo(x, y, whatReason);
            }
            return result;
        }
        public bool MoveLeftRight(int startX, int startY, int destX, int destY, string movingReason)
        {
            bool result;

            if (startX != destX && startY != destY)
                return false;

            if (Game.PlayerX == destX && Game.PlayerY == destY)
            {
                result = MoveToCell(startX, startY, movingReason);
            }
            else if (Game.PlayerX == startX && Game.PlayerY == startY)
            {
                result = MoveToCell(destX, destY, movingReason);
            }
            else
            {
                result = MoveToCell(startX, startY, movingReason);
            }

            return result;
        }

        private GameConnection _gameConnection;
        public async void Login(Account acc)
        {
            if (acc is null) return;
            try
            {
                Account = acc;
                if (Account.Socks.Version != SocksVersion.None)
                {
                    if (Account.HttpProxy.Port > -1 && !string.IsNullOrEmpty(Account.HttpProxy.Host))
                        _gameConnection = new GameConnection(Account.Name, (int)Account.Socks.Version, Account.Socks.Host,
                            Account.Socks.Port, Account.Socks.Username, Account.Socks.Password, Account.HttpProxy.Host, Account.HttpProxy.Port);
                    else
                        _gameConnection = new GameConnection(Account.Name, (int)Account.Socks.Version, Account.Socks.Host,
                            Account.Socks.Port, Account.Socks.Username, Account.Socks.Password);
                }
                else
                {
                    if (Account.HttpProxy.Port > -1 && !string.IsNullOrEmpty(Account.HttpProxy.Host))
                        _gameConnection = new GameConnection(Account.Name, Account.HttpProxy.Host, Account.HttpProxy.Port);
                    else
                        _gameConnection = new GameConnection(Account.Name);
                }
                if (Settings.Versions != null)
                {
                    _gameConnection.GameVersion = Settings.Versions.Split(':')[0];
                    _gameConnection.Version = Convert.ToInt32(Settings.Versions.Split(':')[1]);
                }
                if (Settings.ProtocolKeys != null)
                {
                    _gameConnection.KG1Value = Settings.ProtocolKeys.Split(':')[0];
                    _gameConnection.KG2Value = Settings.ProtocolKeys.Split(':')[1];
                }

                SetClient(new GameClient(_gameConnection));

                await _gameConnection.PostLogin(Account.Name, Account.Password);

                if (_gameConnection._httpConnection.IsLoggedIn)
                    LoginUpdate();
            }
            catch (Exception e)
            {
                Game_Disconnected(e);
            }
        }
    }
}
