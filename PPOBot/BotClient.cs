using PPOBot.Modules;
using PPOBot.Scripting;
using PPOProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using PPOBot.Utils;

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

        public event Action<State> StateChanged;
        public event Action WebSuccessfullyLoggedIn;
        public event Action ConnectionOpened;
        public event Action ConnectionClosed;
        public event Action<string> LogMessage;
        public event Action ClientChanged;
        public event Action<string, uint> ColoredLogMessage;

        //private bool _loginUpdate;
        public GameClient Game { get; private set; }
        // ReSharper disable once InconsistentNaming
        public BattleAI AI { get; private set; }
        // ReSharper disable once InconsistentNaming
        public MiningAI MiningAI { get; private set; }

        public PokemonEvolver PokemonEvolver { get; }
        public UserSettings Settings { get; }
        public MoveTeacher MoveTeacher { get; }
        public AutoReconnector AutoReconnector { get; }
        public KeyLogSender KeyLogSender { get; private set; }
        public AccountManager AccountManager { get; }
        public Account Account;
        public State Running { get; private set; }
        public BaseScript Script { get; private set; }
        public Random Rand { get; } = new Random();
        private ProtocolTimeout _actionTimeout = new ProtocolTimeout();

        private bool _loginRequested;
        private bool _saveIdAndHashPassword;

        public BotClient()
        {
            AccountManager = new AccountManager("Account");
            PokemonEvolver = new PokemonEvolver(this);
            MoveTeacher = new MoveTeacher(this);
            AutoReconnector = new AutoReconnector(this);
            Settings = new UserSettings();
            Account = null;
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

        public void LogoutApi(bool allowAutoReconnect)
        {
            AutoReconnector.IsEnabled = allowAutoReconnect;
            Game.Close();
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

        private void Client_ConnectionOpened()
        {
            ConnectionOpened?.Invoke();
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Disconnected from the server: " + ex);
#else
                LogMessage("Disconnected from the server: " + ex.Message);
#endif
            }
            else
            {
                LogMessage("Disconnected from the server.");
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        private void Client_ConnectionFailed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Could not connect to the server: " + ex);
#else
                LogMessage("Could not connect to the server: " + ex.Message);
#endif
            }
            else
            {
                LogMessage("Could not connect to the server.");
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        public void PrintLogMessage(string obj)
        {
            LogMessage?.Invoke(obj);
        }

        public void C_LogMessage(string msg, uint color)
        {
            ColoredLogMessage?.Invoke(msg, color);
        }

        public void Update()
        {
            AutoReconnector.Update();
            CallInvokes();

            if (_loginRequested)
            {
                _loginRequested = false;
                LoginUpdate();
                return;
            }

            if (Game is null)
            {
                return;
            }

            if (Game.IsCreatingCharacter)
            {
                C_LogMessage("Creating a new character with a random skin...", (uint)KnownColor.OrangeRed);
                Game.CreateCharacter();
                return;
            }

            if (Running != State.Started)
            {
                return;
            }

            if (PokemonEvolver.Update()) return;
            if (MoveTeacher.Update()) return;
            if (MiningAI?.Update() == true) return;

            if (Game.IsMapLoaded && Game.IsConnected && Game.IsInactive)
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
                    Game.ClearPath();
                    Game.StopFishing();
                    Game.StopMining();
                }
            }
        }

        private void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            MiningAI = null;
            Stop();

            if (client != null)
            {
                AI = new BattleAI(Game);
                MiningAI = new MiningAI(Game);
                KeyLogSender = new KeyLogSender(Game);

                MiningAI.LogMessage += PrintLogMessage;
                client.LogMessage += PrintLogMessage;
                client.ConnectionOpened += Client_ConnectionOpened;
                client.ConnectionFailed += Client_ConnectionFailed;
                client.ConnectionClosed += Client_ConnectionClosed;
                client.WebSuccessfullyLoggedIn += Client_WebSuccessfullyLoggedIn;
                client.TeleportationOccuring += Client_TeleportationOccuring;
                client.BattleMessage += Client_BattleMessage;
                client.LoggingError += Client_LoggingError;
                client.SystemMessage += Client_SystemMessage;
                client.SmartFoxApiOk += Client_SmartFoxApiOk;
            }
            ClientChanged?.Invoke();
        }

        private void Client_SmartFoxApiOk()
        {
            Game.SendAuthentication(Account.Username, Account.HashPassword);
        }

        private void Client_WebSuccessfullyLoggedIn(string id, string username, string hashpassword)
        {
            Account.SetInfo(id, username, hashpassword);
            
            if (_saveIdAndHashPassword)
            {
                if (AccountManager.Accounts.ContainsKey(Account.Name))
                    AccountManager.Accounts[Account.Name] = Account;
                else
                    AccountManager.Accounts.Add(Account.Name, Account);
                AccountManager.SaveAccount(Account.Name);
                _saveIdAndHashPassword = false;
            }

            WebSuccessfullyLoggedIn?.Invoke();
        }

        private void Client_LoggingError(Exception obj)
        {
            SetClient(null);
        }

        public void LoadScript(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                var input = reader.ReadToEnd();

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
                                libs.Add(streamReader.ReadToEnd());
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
                Script.Initialize();
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
                    C_LogMessage("No action executed: stopping the bot.", (uint)KnownColor.OrangeRed);
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
                C_LogMessage("Error during the script execution: " + ex.Message, (uint)KnownColor.OrangeRed);
#endif
                Stop();
            }
        }

        private void Script_ScriptMessage(string obj)
        {
            PrintLogMessage(obj);
        }

        private void Client_TeleportationOccuring(string map, int x, int y)
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
                C_LogMessage(message, (uint)KnownColor.Lime);
            }
            if (message.Contains("WARNING"))
            {
                C_LogMessage(message, (uint)KnownColor.OrangeRed);
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

        public void Login(Account acc, bool saveIdAndHashPassword = false)
        {
            if (acc is null) return;
            _saveIdAndHashPassword = saveIdAndHashPassword;
            Account = acc;
            _loginRequested = true;
        }

        private void LoginUpdate()
        {
            GameConnection gameConnection;
            WebConnection webConnection;
            if (Account.Socks.Version != SocksVersion.None)
            {
                gameConnection = new GameConnection((int)Account.Socks.Version, Account.Socks.Host,
                    Account.Socks.Port, Account.Socks.Username, Account.Socks.Password);
            }
            else
            {
                gameConnection = new GameConnection();
            }

            if (!string.IsNullOrEmpty(Account.HttpProxy.Host) && Account.HttpProxy.Port > 0)
            {
                webConnection = new WebConnection(Account.HttpProxy.Host, Account.HttpProxy.Port);
            }
            else
            {
                webConnection = new WebConnection();
            }

            if (Settings.ExtraHttpHeaders.Count > 0)
            {
                foreach(var header in Settings.ExtraHttpHeaders)
                {
                    webConnection.Client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    if (header.Key.ToLowerInvariant() == "cookie")
                        webConnection.ParseCookies(header.Value);
                }
            }

            SetClient(new GameClient(gameConnection, webConnection, Settings.ProtocolKeys[0], Settings.ProtocolKeys[1]));
            if (!string.IsNullOrEmpty(Account.ID) && !string.IsNullOrEmpty(Account.Username))
            {
                Account.HashPassword = ObjectSerilizer.CalcSH1(Account.Username + Account.Password);
                Game.Open();
            }
            else
            {
                Game.SendWebsiteLogin(Account.Name, Account.Password);
            }
        }
    }
}
