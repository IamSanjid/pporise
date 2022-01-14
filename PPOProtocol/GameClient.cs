using BrightNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using PPOBot.Utils;

namespace PPOProtocol
{
    public class GameClient
    {
        private const string Zone = "PokemonPlanet";
        private const int SmartFoxVersion = 163;
        public const string RareLengendaryPattern = "has encountered a";

        private readonly string _kg1;
        private readonly string _kg2;

        private int _avatarType;
        private double _mapMovements;
        private double _movementSpeedMod;
        private double _mapMovementSpeed = 8;

        private bool _isBusy = false;

        public bool IsTrainerBattle { get; private set; } = false;

        private readonly GameConnection _gameConnection;
        //private readonly WebConnection _webConnection;
        private MiningObject _lastRock;
        private int _mapInstance = -1;
        private string _moveType = "";
        private string _mount = "";
        private bool movingForBattle;
        private bool lm = false;
        private DateTime Timer;

        public List<PortablePc> PortablePcList;

        private Direction _lastMovement = Direction.Down;

        private List<Direction> _movements = new List<Direction>();
        private CharacterCreation _characterCreation { get; set; }

        private bool _needToLoadR;

        private DateTime _lastSentPing = DateTime.MaxValue;
        private DateTime _lastSavedData = DateTime.MaxValue;
        private DateTime _lastUpdatePos = DateTime.MaxValue;

        public string[] PokemonCaught { get; private set; }

        //private ExecutionPlan _updatePositionTimeout;
        private ExecutionPlan _checkForLoggingTimeout;

        private int _stepsWalked;

        private readonly ProtocolTimeout _loadingTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _swapTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _itemUseTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _fishingTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _battleTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _movementTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _miningTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _dialogTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _refreshPCTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _npcBattleTimeout = new ProtocolTimeout();

        private bool _updatedMap = false;

        public bool CanMove = true;
        public int Credits;
        public string EncryptedstepsWalked;
        public Battle LastBattle;
        public bool HasEncounteredRarePokemon;

        private ExecutionPlan miningExecutionPlan;
        private ExecutionPlan fishingExecutionPlan;

        private Npc _npcBattler;

        public bool IsGoldMember => MemberType != null && MemberType.ToLowerInvariant().Trim() == "gold";
        public FishingExtentions Fishing { get; private set; }
        public bool IsFishing { get; private set; }
        public bool IsMinning { get; private set; }
        public bool IsTrapped { get; private set; }
        public MiningExtentions Mining { get; private set; }
        public List<MiningObject> MiningObjects { get; }
        public List<EliteChest> EliteChests { get; }
        public Battle ActiveBattle { get; private set; }
        public int Money { get; private set; }

        public Dictionary<string, PlayerInfos> Players { get; }
        private Dictionary<string, PlayerInfos> _removedPlayers;
        private DateTime _updatePlayers;

        public bool IsCreatingCharacter { get; private set; }

        public ThreadSafeRandom Rand;

        public Shop OpenedShop { get; private set; }
        private Shop Shop { get; set; }
        public string MemberType { get; private set; }
        public int MemberTime { get; private set; }
        public string Clan { get; private set; }
        public bool IsSurfing => _mount == "surf" || _moveType == "surf";
        public bool IsOnGround => _mount != "surf" && _moveType != "surf";
        public bool IsBiking => _moveType == "bike";

        /*public GameClient(GameConnection gameConnection, WebConnection webConnection, string kg1, string kg2)
        {
            _kg1 = kg1;
            _kg2 = kg2;
            _webConnection = webConnection;

            _webConnection.LoggingError += OnWebConnectionLoggingError;
            _webConnection.LoggedIn += OnWebConnectionLoggedIn;

            _gameConnection = gameConnection;
            _gameConnection.PacketReceived += OnPacketReceived;
            _gameConnection.Connected += OnConnectionOpened;
            _gameConnection.Disconnected += OnConnectionClosed;

            Items = new List<InventoryItem>();
            MiningObjects = new List<MiningObject>();
            EliteChests = new List<EliteChest>();
            Team = new List<Pokemon>();
            PCPokemon = new Dictionary<int, List<Pokemon>>();
            WildPokemons = new List<WildPokemon>();
            Badges = new List<string>();
            TempMap = "";
            PokemonCaught = new string[900];
            Players = new Dictionary<string, PlayerInfos>();
            _removedPlayers = new Dictionary<string, PlayerInfos>();
            PortablePcList = new List<PortablePc>();

            Rand = new ThreadSafeRandom();
        }*/

        public GameClient(GameConnection gameConnection, string kg1, string kg2)
        {
            _kg1 = kg1;
            _kg2 = kg2;

            _gameConnection = gameConnection;
            _gameConnection.PacketReceived += OnPacketReceived;
            _gameConnection.Connected += OnConnectionOpened;
            _gameConnection.Disconnected += OnConnectionClosed;

            Items = new List<InventoryItem>();
            MiningObjects = new List<MiningObject>();
            EliteChests = new List<EliteChest>();
            Team = new List<Pokemon>();
            PCPokemon = new Dictionary<int, List<Pokemon>>();
            WildPokemons = new List<WildPokemon>();
            Badges = new List<string>();
            TempMap = "";
            PokemonCaught = new string[900];
            Players = new Dictionary<string, PlayerInfos>();
            _removedPlayers = new Dictionary<string, PlayerInfos>();
            PortablePcList = new List<PortablePc>();

            Rand = new ThreadSafeRandom();
        }

        public void Open()
        {
            Timer = DateTime.UtcNow;
            _gameConnection.Connect();
        }

        /*public void SendWebsiteLogin(string name, string password)
        {
            _webConnection.PostLogin(name, password);
        }*/
        
        public int PlayerX { get; private set; }
        public int PlayerY { get; private set; }
        public int TileZ { get; private set; }
        public List<InventoryItem> Items { get; private set; }
        public List<Pokemon> Team { get; set; }
        public Dictionary<int, List<Pokemon>> PCPokemon { get; private set; }
        public IList<WildPokemon> WildPokemons { get; }
        private int LastUpdateX { get; set; }
        private int LastUpdateY { get; set; }
        private int LastUpdateZ { get; set; }
        public string EncryptedTileX { get; private set; }
        public string EncryptedTileY { get; private set; }

        public string MapName { get; private set; }
        private string TempMap { get; set; }
        public string EncryptedMap { get; private set; }
        public bool IsInBattle { get; private set; }
        public string Username { get; private set; }
        public string HashPassword { get; private set; }
        public string Id { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public bool IsInactive =>
                    _movements.Count == 0
                    && !_isBusy
                    && !_loadingTimeout.IsActive
                    && !_movementTimeout.IsActive
                    && !_battleTimeout.IsActive
                    && !_swapTimeout.IsActive
                    && !_fishingTimeout.IsActive
                    && !_itemUseTimeout.IsActive
                    && !_miningTimeout.IsActive
                    && !_dialogTimeout.IsActive
                    && !_refreshPCTimeout.IsActive
                    && !_npcBattleTimeout.IsActive
                    && !(miningExecutionPlan?.IsActive() ?? false)
                    && !(fishingExecutionPlan?.IsActive() ?? false);

        public bool IsMapLoaded => !string.IsNullOrEmpty(MapName) && _updatedMap;

        public event Action ConnectionOpened;
        public event Action SmartFoxApiOk;
        public event Action<Exception> ConnectionFailed;
        public event Action<Exception> ConnectionClosed;
        public event Action<string> LogMessage;
        public event Action<string, bool> ChatMessage;
        public event Action<string, int, int> TeleportationOccuring;
        public event Action PlayerDataUpdated;
        public event Action InventoryUpdated;
        public event Action MapUpdated;
        public event Action<bool> TeamUpdated;
        public event Action PlayerPositionUpdated;
        public event Action SuccessfullyAuthenticated;
        //public event Action<string, string, string> WebSuccessfullyLoggedIn;
        public event Action<string> AuthenticationFailed;
        public event Action BattleStarted;
        public event Action<int> BattleEnded;
        public event Action<string> BattleMessage;
        public event Action<IList<WildPokemon>, int> EnemyUpdated;
        public event Action Evolving;
        public event Action<string, int> LearningMove;
        //public event Action<Exception> LoggingError;
        public event Action LoggedIn;
        public event Action<string[]> PrivateChat;
        public event Action<string> SystemMessage;
        public event Action<Shop> ShopOpened;
        public event Action<MiningObject> RockRestored;
        public event Action<MiningObject> RockDepleted;
        public event Action<PlayerInfos> PlayerUpdated;
        public event Action<PlayerInfos> PlayerAdded;
        public event Action<PlayerInfos> PlayerRemoved;

        public event Action<string> AskedForKeyLogs;
        public event Action NoKeyLogsNeeded;
        public event Action<uint> PerformingAction;

        public List<string> Badges { get; private set; }

        public DateTime LastBreakTime;

        private void CheckForLoggingIn()
        {
            if (!IsMapLoaded)
            {
                Close();
                AuthenticationFailed?.Invoke("Probably MD5 key was wrong.");
            }
        }

        public bool PercentSuccess(double _chance)
        {
            return Rand.NextDouble() * 100 < _chance;
        }
        public bool PercentSuccess(int _chance)
        {
            return Rand.NextDouble() * 100 < _chance;
        }

        /*private void OnWebConnectionLoggingError(Exception obj)
        {
            LoggingError?.Invoke(obj);
        }

        private void OnWebConnectionLoggedIn()
        {
            Open();
            _webConnection.Client.Dispose();
            WebSuccessfullyLoggedIn?.Invoke(_webConnection.Id, _webConnection.Username, _webConnection.HashPassword);
        }*/

        public int GetTimer() => (int)(DateTime.UtcNow - Timer).TotalMilliseconds;

        private void SetUserInfos(string username, string hashpassword)
        {
            //Id = id;
            Username = username;
            HashPassword = hashpassword;
            EncryptedstepsWalked = ObjectSerilizer.CalcMd5(_stepsWalked + _kg1 + Username);
        }

        private void OnJoinedRoom()
        {
            LogMessage?.Invoke("Loading Game Data...");
            GetTimeStamp("getStartingInfo", "2");

            _checkForLoggingTimeout?.Dispose();
            _checkForLoggingTimeout = ExecutionPlan.Delay(180000, () => CheckForLoggingIn());
        }

        private void OnAuthentication(bool success)
        {
            if (success)
            {
                SendCmd("tsys", "getRmList", -1, "");
                SuccessfullyAuthenticated?.Invoke();
                _checkForLoggingTimeout = ExecutionPlan.Delay(20000, () => CheckForLoggingIn());
                IsAuthenticated = true;
            }
        }

        private void OnConnectionOpened()
        {
#if DEBUG
            Console.WriteLine("[+++] Connection opened");
#endif
            IsConnected = true;
            var verChk = $"<ver v='{SmartFoxVersion}' />";
            SendCmd("tsys", "verChk", 0, verChk);
            ConnectionOpened?.Invoke();
        }

        private void OnConnectionClosed(Exception ex)
        {
            if (!IsConnected)
            {
#if DEBUG
                Console.WriteLine("[---] Connection failed");
#endif
                ConnectionFailed?.Invoke(ex);
            }
            else
            {
                IsConnected = false;
#if DEBUG
                Console.WriteLine("[---] Connection closed");
#endif
                ConnectionClosed?.Invoke(ex);
            }
        }

        /*private void Connection_LogMessage(string obj)
        {
            LogMessage?.Invoke(obj);
        }*/

        private void UpdatePosition()
        {
            if (LastUpdateX != PlayerX || LastUpdateY != PlayerY || LastUpdateZ != TileZ)
            {
                LastUpdateX = PlayerX;
                LastUpdateY = PlayerY;
                LastUpdateZ = TileZ;
                GetTimeStamp("updateXYZ");
            }
        }

        public void CreateCharacter()
        {
            if (!IsCreatingCharacter) return;
            IsCreatingCharacter = false;
            _characterCreation = new CharacterCreation(Rand);
            GetTimeStamp("createCharacter");
            _dialogTimeout.Set();
            ExecutionPlan.Delay(1500, () =>
            {
                GetTimeStamp("sendAddPlayer");
            });
        }

        private void OnPacketReceived(string packet)
        {
#if DEBUG
            Console.WriteLine(packet);
#endif
            packet = ObjectSerilizer.DecodeEntities(packet);
            if (packet.StartsWith("`"))
            {
                var data = packet.Substring(1).Split('`');

                if (data[0] != "xt") throw new Exception("Received unknown packet: " + packet);

                switch (data[1])
                {
                    case "l":
                        OnAuthentication(true);
                        break;
                    case "pmsg":
                    case "r17":
                    case "r59":
                        ProcessChatMessage(packet.Substring(1), data[1] == "r59");
                        break;
                    case "r36":
                        PrivateChat?.Invoke(data);
                        break;
                    case "r10":
                        HandleStartingInfo(data);
                        break;
                    case "r44":
                        Money = Convert.ToInt32(data[3]);
                        var ii = Items.Find(i => i.Name == data[4]);
                        ii.Quantity += 1;
                        Items = Items.OrderBy(i => i.Uid).ToList();
                        InventoryUpdated?.Invoke();
                        break;
                    case "r51":
                        PrintSystemMessage($"The server wants to teleport somone/may be you to {data[5]} ({data[3]}, {data[4]})");
                        break;
                    case "avn":
                        PrintSystemMessage($"M: {data[3]}, {data[4]} ({data[5]})");
                        break;
                    case "avm":
                        PrintSystemMessage($"K: {data[3]}, ({data[4]})");
                        break;
                    case "b85":
                        var var1 = data[3].Split(',');
                        var var2 = data[4].Split(',');
                        var iss = 0;
                        while (iss < var1.Length)
                        {
                            var i1 = Items.Find(i => i.Name == var1[iss]);
                            i1.Quantity -= 1;
                            iss++;
                        }
                        iss = 0;
                        while (iss < var2.Length)
                        {
                            var i1 = Items.Find(i => i.Name == var2[iss]);
                            if (i1 is null)
                            {
                                var newI = new InventoryItem(var2[iss]);
                                Items.Add(newI);
                                newI.Uid = Items.IndexOf(newI);
                            }
                            else
                            {
                                i1.Quantity += 1;
                            }
                            iss++;
                        }

                        Items = Items.OrderBy(i => i.Uid).ToList();
                        InventoryUpdated?.Invoke();
                        break;
                    case "b86":
                        var itmData = data[3].Split(',');
                        var inventoryItem = Items.Find(i => i.Name == itmData[0]);
                        if (inventoryItem != null)
                            inventoryItem.Quantity += Convert.ToInt32(itmData[1]);
                        else
                        {
                            inventoryItem = new InventoryItem(itmData[0], Convert.ToInt32(itmData[1]), Items.LastOrDefault().Uid);
                            Items.Add(inventoryItem);
                        }
                        Items = Items.OrderBy(o => o.Uid).ToList();
                        InventoryUpdated?.Invoke();
                        break;
                    case "b88":
                        GetTimeStamp("updateMap", MapName);
                        break;
                    case "r61":
                        _avatarType = 1;
                        if (!_updatedMap)
                            LoadMap(false, MapName);
                        break;
                    case "r65":
                        var loc11 = data[3].Split('|');
                        var loc8 = 0;
                        Team.Clear();
                        while (loc8 < loc11.Length)
                        {
                            ParsePokemon(loc11[loc8]);
                            loc8++;
                        }

                        EndBattle();
                        break;
                    case "r66":
                        var loc12 = data[3].Split('|');
                        var loc9 = 0;
                        Team.Clear();
                        while (loc9 < loc12.Length)
                        {
                            ParsePokemon(loc12[loc9]);
                            loc9++;
                        }

                        if (data[4] != "1") LoadMap(true, TempMap, PlayerX, PlayerY);
                        EndBattle();
                        break;
                    case "a":
                    case "b":
                        OnAddPlayer(data, data[1] == "a");
                        break;
                    case "m":
                        OnPlayerMovement(data);
                        break;
                    case "r62":
                        if (Players.ContainsKey(data[3]))
                        {
                            PlayerRemoved?.Invoke(Players[data[3]]);
                            if (!_removedPlayers.ContainsKey(data[3]))
                                _removedPlayers.Add(data[3], Players[data[3]]);
                            Players.Remove(data[3]);
                        }
                        break;
                    case "b2":
                        var to = Team[Convert.ToInt32(data[3])];
                        var from = Team[Convert.ToInt32(data[4])];
                        Team[Convert.ToInt32(data[4])] = to;
                        Team[Convert.ToInt32(data[3])] = from;
                        if (!_swapTimeout.IsActive)
                        {
                            _swapTimeout.Set(Rand.Next(500, 1000));
                        }
                        _isBusy = false;
                        TeamUpdated?.Invoke(false);
                        GetTimeStamp("updateFollowPokemon");
                        break;
                    case "r67":
                        GetTimeStamp("endBattleDisconnect");
                        break;
                    case "r70":
                        GetTimeStamp("endBattleDisconnect2");
                        break;
                    case "r5":
                        var pokeIndex = Convert.ToInt32(data[3]);
                        var itm = GetItemFromName(Team[pokeIndex].ItemHeld);
                        if (itm == null)
                        {
                            var newItm = new InventoryItem(Team[pokeIndex].ItemHeld);
                            Items.Add(newItm);
                            newItm.Uid = Items.IndexOf(newItm);
                            Items[Items.IndexOf(newItm)].Uid = Items.IndexOf(newItm);
                        }
                        else
                        {
                            Items[Items.IndexOf(Items.Find(i => i.Name == itm.Name))].Quantity += 1;
                        }
                        Team[pokeIndex].ItemHeld = "";
                        if (_swapTimeout.IsActive)
                        {
                            _swapTimeout.Set(Rand.Next(500, 1000));
                        }
                        _isBusy = false;
                        TeamUpdated?.Invoke(false);
                        InventoryUpdated?.Invoke();
                        break;
                    case "r4":
                        var pokeUid = Convert.ToInt32(data[3]);
                        var itemUid = Convert.ToInt32(data[4]);
                        var item = GetItemByUid(itemUid);
                        Items.Remove(item);
                        Team[pokeUid].ItemHeld = item.Name;
                        Items = Items.OrderBy(i => i.Uid).ToList();
                        InventoryUpdated?.Invoke();
                        if (_swapTimeout.IsActive)
                        {
                            _swapTimeout.Set(Rand.Next(500, 1000));
                        }
                        _isBusy = false;
                        TeamUpdated?.Invoke(false);
                        break;
                    case "r78":

                        //Well I really don't know what to do with these....

                        // ReSharper disable once UnusedVariable
                        var pokedexSeen = data[3].Split(',');
                        // ReSharper disable once UnusedVariable
                        var pokedexCaught = data[4].Split(',');

                        break;
                    case "w":
                        HandleBattle(data);
                        break;
                    case "w2":
                        HandleBattle(data, true);
                        break;
                    case "c":
                        HandleBattleUpdate(data);
                        break;
                    case "bl":
                        TempMap = data[3];
                        EncryptedMap = data[4];
                        PlayerX = Convert.ToInt32(data[5]);
                        PlayerY = Convert.ToInt32(data[6]);
                        if (ActiveBattle != null)
                            ActiveBattle.LostBattle = true;
                        break;
                    case "ui":
                        HandleUseItem2(data);
                        break;
                    case "ab":
                        Badges.Add(data[3]);
                        break;
                    case "b87":
                        Items.Find(i => i.Name == data[3].Split(',')[0]).Quantity -=
                            Convert.ToInt32(data[3].Split(',')[1]);
                        if (Items.Find(i => i.Name == data[3].Split(',')[0]).Quantity <= 0)
                            Items.Remove(Items.Find(i => i.Name == data[3].Split(',')[0]));
                        Items = Items.OrderBy(o => o.Uid).ToList();
                        InventoryUpdated?.Invoke();
                        break;
                    case "r26":
                        break;
                    case "r28":
                        break;
                    case "e":
                        EndBattle();
                        BattleMessage?.Invoke("You have ran away from the battle.");
                        break;
                    case "b123":
                        PokemonCaught[Convert.ToInt32(data[3]) - 1] = "true";
                        break;
                    case "b130":
                        HandlePortablePcPlaced(data);
                        break;
                    case "b132":
                        HandlePortablePcExpired(data);
                        break;
                    case "b139":
                        Team[Convert.ToInt32(data[3])] = ParseOnePokemon(data[4]);
                        break;
                    case "b119":
                        ParseMultiPokemon(data[3]);
                        break;
                    case "b95":
                        foreach (var pokemon in Team)
                        {
                            pokemon.CurrentHealth = pokemon.MaxHealth;
                            pokemon.Ailment = "";
                            pokemon.AbilityLength = 0;
                        }

                        if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
                        TeamUpdated?.Invoke(true);
                        break;
                    case "b121":
                        OnFishingBattle();
                        break;
                    case "b164":
                        HandleMiningRockDepleted(data);
                        break;
                    case "b165":
                        HandleMiningRockRestored(data);
                        break;
                    case "b166":
                        StopMining();
                        break;
                    case "b167":
                        HandleFinishMining(data);
                        break;
                    case "b141":
                    case "b141_2":
                    case "blg":
                    case "blg2":
                    case "b177":
                        OnAuthenticationFailed(data[1]);
                        break;
                    case "b179":
                        HandleBattleRequests(data);
                        break;
                    case "b182":
                        HandleEliteBuy(data);
                        break;
                    case "b186":
                        HandleOpenEliteChest(data);
                        break;
                    case "b187":
                        HandleRemoveEliteChest(data);
                        break;
                    case "b5":
                        MapUpdate(data);
                        break;
                    case "b79e":
                        HandleTrainerBattleCooldownError(data);
                        break;
                }
            }
            else
            {
                OnXmlPacket(packet);
            }
        }

        private void OnAuthenticationFailed(string reason)
        {
            string msg = "";
            switch (reason)
            {
                case "b141":
                    msg = "You've been permanently banned from the game.";
                    break;
                case "b141_2":
                    msg = "This account has been banned. See the forums for more details.";
                    break;
                case "blg":
                    msg = "Incorrect username/password!";
                    break;
                case "blg2":
                    msg = "Failed to log in - please try again.";
                    break;
                case "b177":
                    msg = "Client out of date.";
                    break;
            }
            AuthenticationFailed?.Invoke(msg);
            Close();
        }
        private void HandlePortablePcExpired(string[] data)
        {
            PrintSystemMessage($"The Portable PC placed by {data[5]} has expired.");
            foreach (var portablePc in PortablePcList.Where(portablePc => portablePc.X == Convert.ToInt32(data[3]) && portablePc.Y == Convert.ToInt32(data[4]) &&
                                                                          portablePc.Owner == data[5]))
            {
                PortablePcList.Remove(portablePc);
                break;
            }
        }

        private void HandlePortablePcPlaced(string[] data)
        {
            var pc = new PortablePc()
            {
                X = Convert.ToInt32(data[3]) * 32,
                Y = Convert.ToInt32(data[4]) * 32,
                Owner = data[5]
            };
            PortablePcList.Add(pc);

            PrintSystemMessage($"{data[5]} placed a Portable PC on this map!");
        }

        private void OnFishingBattle()
        {
            IsInBattle = true;
            CanMove = false;
            IsFishing = false;
            GetTimeStamp("goodHook", "1");
            PerformingAction?.Invoke(Actions.ACTION_KEY);
            StopFishing();
        }

        private void HandleTrainerBattleCooldownError(string[] data)
        {
            var timeLeft = Convert.ToInt32(data[3]);
            var timeFloor = Math.Floor((double)timeLeft / 3600);
            var time = Math.Round((timeLeft - timeFloor * 3600) / 60);
            string str;
            if (timeFloor > 0)
            {
                if (time > 0)
                {
                    str = timeFloor + " hours, " + time + " minutes";
                }
                else
                {
                    str = timeFloor + " hours";
                } // end else if
            }
            else
            {
                str = time + " minutes";
            }
            PrintSystemMessage("You need to wait another " + str + ".");
            _npcBattler = null;
            IsTrainerBattle = false;
            _npcBattleTimeout.Cancel();
            _isBusy = false;
        }

        // xt`b179`-1`Username`Level`??`Wager
        private void HandleBattleRequests(string[] data)
        {
            CanMove = false;
            if (data[6] != null && data[6] != "")
            {
                SystemMessage?.Invoke(data[3] + " would like to start a $" + Int32.Parse(data[6]) + " wager battle with you.");
                ExecutionPlan.Delay(Rand.Next(2000, 5000),
                    () =>
                    {
                        LogMessage?.Invoke("Declined battle request");
                        GetTimeStamp("declineBattle");
                        CanMove = true;
                    });
            }
            else
            {
                SystemMessage?.Invoke(data[3] + " would like to battle you.");
                ExecutionPlan.Delay(Rand.Next(2000, 5000),
                    () =>
                    {
                        LogMessage?.Invoke("Declined battle request");
                        GetTimeStamp("declineBattle");
                        CanMove = true;
                    });
            }
        }

        private void HandleRemoveEliteChest(string[] data)
        {
            var x = Convert.ToInt32(data[3]);
            var y = Convert.ToInt32(data[4]);
            var chest = EliteChests.Find(ch => ch.X == x && ch.Y == y);
            if (chest != null)
            {
                chest.UpdateChestOpen(true);
            }
        }

        private void HandleOpenEliteChest(string[] data)
        {
            var items = ParseArray(data[3]);
            var key = Items.Find(x => string.Equals(x.Name, "treasure key", StringComparison.InvariantCultureIgnoreCase));
            if (key != null)
            {
                if (key.Quantity > 1)
                {
                    var index = Items.IndexOf(key);
                    Items[index].Quantity -= 1;
                }
                else
                {
                    Items.Remove(key);
                }
            }
            var newItm = new InventoryItem(items[0])
            {
                Uid = (int)(Items.LastOrDefault()?.Uid + 1)
            };
            Items.Add(newItm);

            SystemMessage?.Invoke("You opened the Elite Chest and found " + items[1] + " " + items[0] + ".");

            Items = Items.OrderBy(x => x.Uid).ToList();
            InventoryUpdated?.Invoke();
        }

        private void HandleEliteBuy(string[] data)
        {
            var tokenItem = Items.Find(it => it.Name.ToLowerInvariant() == "elite token");
            if (data[3] == "1")
            {
                ParseMultiPokemon(data[5]);
                if (tokenItem != null)
                {
                    if (tokenItem.Quantity > 200)
                    {
                        var index = Items.IndexOf(tokenItem);
                        Items[index].Quantity = Items[index].Quantity - 200;
                    }
                    else
                    {
                        Items.Remove(tokenItem);
                    }
                }
            }
            else
            {
                LogMessage?.Invoke("You have found something cool. Relog to get those items!");
            }

            SystemMessage?.Invoke(data[4]);

            Items = Items.OrderBy(x => x.Uid).ToList();
            InventoryUpdated?.Invoke();
        }

        private void HandleFinishMining(string[] resObj)
        {
            if (resObj[4] != "")
                Mining.MiningLevel = Convert.ToInt32(resObj[4]);
            Mining.CurrentMiningXp = Convert.ToInt32(resObj[3]);
            IsMinning = false;
            StopMining(false);
        }

        private void HandleMiningRockDepleted(string[] resObj)
        {
            var x = Convert.ToInt32(resObj[3]);
            var y = Convert.ToInt32(resObj[4]);
            if (_lastRock != null)
                if (x == _lastRock.X && y == _lastRock.Y)
                    _lastRock.IsMined = true;
            var rock = MiningObjects.Find(r => r.X == x && r.Y == y);
            if (rock != null)
            {
                rock.IsMined = true;
                RockDepleted?.Invoke(rock);
                _miningTimeout.Set(700);
                if (_lastRock == rock)
                    StopMining();
            }
        }
        private void HandleMiningRockRestored(string[] resObj)
        {
            var x = Convert.ToInt32(resObj[3]);
            var y = Convert.ToInt32(resObj[4]);
            if (_lastRock != null)
                if (x == _lastRock.X && y == _lastRock.Y)
                    _lastRock.IsMined = false;
            var rock = MiningObjects.Find(r => r.X == x && r.Y == y);
            if (rock != null)
            {
                rock.IsMined = false;
                rock.IsGoldMember = resObj[5] == "1";
                RockRestored?.Invoke(rock);
                _miningTimeout.Set(700);
            }
        }

        private void ProcessChatMessage(string packet, bool isClan = false)
        {
            //-----------------Stupid way to check chat messages xD------------------------//

            HasEncounteredRarePokemon = (packet.IndexOf(RareLengendaryPattern, StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                packet.IndexOf(Username, StringComparison.InvariantCultureIgnoreCase) >= 0) || (packet.IndexOf("you have encountered a", StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (packet.IndexOf("error with fishing rod location in inventory", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                packet.ToLowerInvariant().Contains("can't fish again yet") && IsFishing)
            {
                IsFishing = false;
                StopFishing();
            }

            if (packet.IndexOf("you need at least", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                packet.ToLowerInvariant().Contains("to fish in these waters.") && IsFishing)
            {
                IsFishing = false;
                StopFishing();
            }

            if (packet.Contains("Please finish what you are doing first"))
            {
                _dialogTimeout.Set(Rand.Next(1500, 2500)); // wait for few seconds...
            }

            if (RemoveUnknownSymbolsFromString(packet)
                    .ToLowerInvariant().Contains("you can't mine"))
                _miningTimeout.Set();

            if (packet.ToLowerInvariant().Contains("rock has already been mined") || packet.ToLowerInvariant().Contains("you mined a") ||
                packet.ToLowerInvariant().Contains("mine again yet") || packet.ToLowerInvariant().Contains("you need a higher mining level to mine that"))
            {
                if (MiningObjects.Count > 0 && _lastRock != null && IsMinning)
                {
                    _lastRock.IsMined = true;

                    MiningObjects.Find(r => r.X == _lastRock.X && r.Y == _lastRock.Y)
                        .IsMined = true;
                    RockDepleted?.Invoke(MiningObjects.Find(r => r.X == _lastRock.X && r.Y == _lastRock.Y));
                    StopMining();
                }
                else
                    _miningTimeout.Set(Rand.Next(2500, 3500));
            }
            if (packet.Contains("<font color='#00FF00'>"))
            {
                var data = packet.Split('`');
                var rmsg = data[3];
                if (rmsg.Contains("<br>"))
                {
                    rmsg = Regex.Replace(rmsg, @"\<(.*?)\>", "||*.");
                    var rmsgs = rmsg.Split(new[] { "||*." }, StringSplitOptions.RemoveEmptyEntries);
                    SystemMessage?.Invoke(rmsgs[0]);
                    rmsgs.ToList().Remove(rmsgs[0]);
                    foreach (var eMsg in rmsgs)
                    {
                        if (rmsgs.ToList().IndexOf(eMsg) > 0)
                        {
                            LogMessage?.Invoke(RemoveUnknownSymbolsFromString(eMsg));
                        }
                    }
                }
                else
                {
                    rmsg = Regex.Replace(rmsg, @"\<(.*?)\>", "");
                    SystemMessage?.Invoke(RemoveUnknownSymbolsFromString(rmsg));
                }
            }
            //-----------------Stupid way to check chat messages xD------------------------//

            ChatMessage?.Invoke(packet, isClan);
        }

        //I just hate Xml idk why....
        private void OnXmlPacket(string packet)
        {
            try
            {
                dynamic data = XMLApi.DynamicXml.Parse(packet);

                var xml = new XmlDocument();

#if DEBUG
                Console.WriteLine("Xml action: " + data.body.action);
#endif

                if (!string.IsNullOrEmpty(data.body.action))
                {
                    switch (data.body.action)
                    {
                        case "apiOK":
                            SmartFoxApiOk?.Invoke();
                            break;
                        case "rmList":
                            SendCmd("tsys", "autoJoin", -1, "");
                            break;
                        case "joinOK":
                            OnJoinedRoom();
                            break;
                    }
                }
                if (data.body.dataObj != null && !string.IsNullOrEmpty(data.body.dataObj._cmd))
                {
#if DEBUG
                    Console.WriteLine("Xml action: " + data.body.dataObj._cmd);
#endif
                    switch (data.body.dataObj._cmd)
                    {
                        case "learnMove":
                            xml.LoadXml(packet);
                            OnLearningMove(xml);
                            break;
                        case "updateMap":
                            MapUpdate(packet);
                            break;
                        case "askEvolve":
                            CanMove = false;
                            Evolving?.Invoke();
                            break;
                        case "acceptEvolve":
                            CanMove = true;
                            xml.LoadXml(packet);
                            OnAcceptEvolve(xml);
                            break;
                        case "declineEvolve":
                            CanMove = true;
                            break;
                        case "updateInventory":
                            OnInventoryUpdateThroughXml(data.body.dataObj.inventory);
                            break;
                        case "sentBattleRequest":
                            ExecutionPlan.Delay(Rand.Next(2000, 5000),
                                () => GetTimeStamp("declineBattle"));
                            break;
                        case "sentTradeRequest":
                            ExecutionPlan.Delay(Rand.Next(2000, 5000),
                                () => GetTimeStamp("declineTrade"));
                            break;
                        case "clanRequest":
                            ExecutionPlan.Delay(Rand.Next(2000, 5000),
                                () => GetTimeStamp("declineClanInvite"));
                            break;
                        case "forgetMove":
                            xml.LoadXml(packet);
                            UpdateTeamThroughXml(xml);
                            break;
                        case "buyItem":
                            OnInventoryUpdateThroughXml(data);
                            xml.LoadXml(packet);
                            BoughtItem(xml);
                            break;
                        case "useItem2":
                            _isBusy = false;
                            OnInventoryUpdateThroughXml(data);
                            xml.LoadXml(packet);
                            UpdateTeamThroughXml(xml);
                            UsedItemMsg(xml);
                            break;
                        case "choosePokemon":
                            xml.LoadXml(packet);
                            UpdateTeamThroughXml(xml);
                            GetTimeStamp("updateFollowPokemon");
                            break;
                        case "reorderStoragePokemon":
                            xml.LoadXml(packet);
                            UpdateTeamThroughXml(xml);
                            GetTimeStamp("updateFollowPokemon");
                            break;
                        case "acceptMerchantItem":
                            if (data.body.dataObj.money != null)
                            {
                                Money = Convert.ToInt32(data.body.dataObj.money);
                                PlayerDataUpdated?.Invoke();
                            }
                            if (data.body.dataObj.inventory != null)
                            {
                                OnInventoryUpdateThroughXml(data.body.dataObj.inventory);
                            }
                            break;
                        case "trainerData":
                            if (data.body.dataObj.badges != null)
                                PrintSystemMessage("Contact to bot developer: " + packet);
                            break;
                        case "b2adb2":
                            AskedForKeyLogs?.Invoke(data.body.dataObj.a.ToString());
                            PrintSystemMessage("You're being monitored by an admin or a moderator. Don't worry sending some random keys to fool them. Packet: " + packet);
                            break;
                        case "b2adb2z":
                            PrintSystemMessage("You're no longer being monitored. You've successfully fooled them. Packet: " + packet);
                            NoKeyLogsNeeded?.Invoke();
                            break;
                    }
                }
                //var node = xml.DocumentElement?.GetElementsByTagName("dataObj")[0];
                //if (node != null)
                //    foreach (XmlElement textNode in node)
                //        if (textNode.GetAttribute("n") != "" && textNode.InnerText != "")
                //        {
                //            var type = textNode.GetAttribute("n");
                //            switch (type)
                //            {
                //                case "_cmd":
                //                    switch (textNode.InnerText)
                //                    {

                //                    }
                //                    break;
                //            }
                //        }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                // ignore
            }
            catch (Exception e)
            {
                //ignored coz ReSherper says "Why you even put it here if you don't use it!" lol..
                Console.WriteLine(e);
            }
        }
        //I just hate Xml idk why....
        private void UsedItemMsg(XmlDocument xml)
        {
            var node = xml.DocumentElement?.GetElementsByTagName("dataObj")[0];
            if (node != null)
            {
                foreach (XmlElement textNode in node)
                {
                    if (textNode.GetAttribute("n") != "" && textNode.InnerText != "")
                    {
                        var type = textNode.GetAttribute("n");
                        if (type == "msg")
                        {
                            PrintSystemMessage(RemoveUnknownSymbolsFromString(textNode.InnerText));
                        }
                    }
                }
            }
        }

        public static string RemoveUnknownSymbolsFromString(string input)
        {
            return input.Replace("???", "'").Replace("?", "'").Replace("\0", "'").Replace("\0\0\0", "'").Replace("&#44", "");
        }
        //I just hate Xml idk why....
        private void BoughtItem(XmlDocument xml)
        {
            var node = xml.DocumentElement?.GetElementsByTagName("dataObj")[0];
            if (node != null)
            {
                foreach (XmlElement textNode in node)
                {
                    if (textNode.GetAttribute("n") != "" && textNode.InnerText != "")
                    {
                        var type = textNode.GetAttribute("n");
                        if (type == "money")
                        {
                            Money = Convert.ToInt32(textNode.InnerText);
                        }
                        else if (type == "msg")
                        {
                            PrintSystemMessage(RemoveUnknownSymbolsFromString(textNode.InnerText));
                        }
                    }
                }
            }
            PlayerDataUpdated?.Invoke();
        }

        public void PrintSystemMessage(string msg)
        {
            SystemMessage?.Invoke(msg);
        }

        public void LearnMove(int moveUid)
        {
            if (moveUid < 0)
                return;
            _swapTimeout.Set();
            PerformingAction?.Invoke(Actions.USING_MOVE);
            GetTimeStamp("forgetMove", moveUid.ToString());
        }

        public void OpenShop()
        {
            if (Shop is null) return;
            OpenedShop = Shop;
            ShopOpened?.Invoke(OpenedShop);
        }

        private void OnLearningMove(XmlDocument xml)
        {
            Shop = null;
            OpenedShop = null;
            var learningMove = "";
            var objNode = xml.GetElementsByTagName("var");
            var slotNo = -1;
            foreach (XmlNode n in objNode)
                if (n.Attributes != null && n.Attributes[0].Value == "moveName")
                    learningMove = n.InnerText;
                else if (n.Attributes != null && n.Attributes[0].Value == "slot")
                    int.TryParse(n.InnerText, out slotNo);
            if (Team[slotNo].Moves.Any(m => m.Name is null && m.Data is null))
            {
                for (var i = 0; i < Team[slotNo].Moves.Length; i++)
                {
                    if (Team[slotNo].Moves[i].Name is null && Team[slotNo].Moves[i].Data is null)
                    {
                        LearnMove(i);
                        break;
                    }
                }
            }
            LearningMove?.Invoke(learningMove, slotNo);
            UpdateTeamThroughXml(xml);
#if DEBUG
            Console.WriteLine(learningMove);
#endif
        }

        private void OnAcceptEvolve(XmlDocument xml)
        {
            Shop = null;
            OpenedShop = null;
            UpdateTeamThroughXml(xml);
        }

        private void UpdateTeamThroughXml(XmlDocument xml)
        {
            var xmlDocument = XDocument.Parse(xml.InnerXml);
            var pokeElement = xmlDocument.Descendants("obj").ToList().Find(o => o.Attribute("o")?.Value != null && o.Attribute("o")?.Value == "userPokemon");
            if (pokeElement == null)
            {
                return;
            }
            Team.Clear();
            xml = new XmlDocument();
            xml.LoadXml(pokeElement.ToString());
            var nodes = xml.GetElementsByTagName("obj");
            foreach (XmlElement element in nodes)
            {
                var result = int.TryParse(element.GetAttribute("o"), out var uid);
                if (!result)
                    uid = -1;
                if (element.GetAttribute("o") != "" && result)
                    ParsePokemonFromXml(element.ChildNodes, uid >= 0 ? uid : -1);
                else if (string.Equals(element.GetAttribute("o"), "moves", StringComparison.InvariantCultureIgnoreCase))
                    ParsePokemonFromXml(element.ChildNodes, uid >= 0 ? uid : -1);
#if DEBUG
                if (uid >= 0)
                    Console.WriteLine(uid);
#endif
            }

            if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
            TeamUpdated?.Invoke(true);
        }

        private void OnInventoryUpdateThroughXml(dynamic data)
        {
            _isBusy = false;
            try
            {
                Items.Clear();
                foreach (var obj in data.obj)
                {
                    InventoryItem item = new InventoryItem(obj.var[1], Convert.ToInt32(obj.var[0]));
                    item.Uid = Convert.ToInt32(obj.o);
                    Items.Add(item);
                }
                Items = Items.OrderBy(itm => itm.Uid).ToList();
                _itemUseTimeout.Set(Rand.Next(2000, 2500));
                InventoryUpdated?.Invoke();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                // ignore
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public void Close()
        {
            _gameConnection.Close();
            //_updatePositionTimeout?.Dispose();
            _checkForLoggingTimeout?.Dispose();
            fishingExecutionPlan?.Dispose();
            miningExecutionPlan?.Dispose();
        }

        private void ParsePokemonFromXml(XmlNodeList nodes, int uid = -1)
        {
            var pokemon = new Pokemon(nodes);
            if (!pokemon.Name.Contains("???") || pokemon.Nature != null)
                Team.Add(pokemon);
            if (Team.Any(pok => pok.UniqueId == pokemon.UniqueId))
                pokemon.Uid = uid != -1 ? uid + 1 : Team.FindIndex(pok => pok.UniqueId == pokemon.UniqueId) + 1;
        }

        private void HandleBattle(string[] data, bool disconnect = false)
        {
            _movements.Clear();
            _isBusy = false;
            movingForBattle = false;
            Shop = null;
            LastBattle = null;
            OpenedShop = null;
            IsInBattle = true;
            StopMining();
            StopFishing();

            ActiveBattle = new Battle(data, !IsTrainerBattle, disconnect, this);

            // if Lastbreak was more than 5 mins ago, by .5 perc chance, we are taking a break between 10 to 120 seconds
            //if (PercentSuccess(0.5) && LastBreakTime.AddMinutes(5) < DateTime.Now)
            //{
            //    LastBreakTime = DateTime.Now;
            //    var breakTime = Rand.Next(10000, 120000);
            //    PrintSystemMessage($"Taking a break of {breakTime} milliseconds...");

            //    _battleTimeout.Set(breakTime);

            //}
            //else
            //{
                _battleTimeout.Set(Rand.Next(2000, 4500));
            // }

            CanMove = false;
            if (ActiveBattle.IsWildBattle)
            {
                BattleMessage?.Invoke("A wild " + (ActiveBattle.WildPokemon.IsShiny ? "Shiny " : "")
                                                + (ActiveBattle.WildPokemon.IsElite ? "Elite " : "") +
                                                ActiveBattle.WildPokemon.Name + 
                                                (ActiveBattle.WildPokemon.Form != "default"
                    ? "(" + ActiveBattle.WildPokemon.Form + ")"
                    : string.Empty) +
                      " has appeared!");
            }
            else
            {
                BattleMessage?.Invoke("Opponent sent " + (ActiveBattle.WildPokemon.IsShiny ? "Shiny " : "")
                    + (ActiveBattle.WildPokemon.IsElite ? "Elite " : "") + ActiveBattle.WildPokemon.Name +
                                      "!");
            }

            if (disconnect)
            {
                PrintSystemMessage($"Disconnecting because w2...");

                BattleStarted?.Invoke();
            }

            if (data[6] != null && data[6] != "-1")
            {
                CanMove = false;
                var t = Fishing.TotalFishingXp.ToString();
                Fishing = new FishingExtentions(data[6], data[7], t);
            }
            else if (data.Length > 9)
            {
                if (data[10] == "1")
                {
                    IsInBattle = true;
                    CanMove = false;
                }
            }
            WildPokemons.Add(ActiveBattle.WildPokemon);
            BattleStarted?.Invoke();
            EnemyUpdated?.Invoke(WildPokemons, ActiveBattle.ActivePokemon);
        }

        private void HandleBattleUpdate(string[] resObj)
        {
            try
            {
                _isBusy = false;
                ActiveBattle.UpdateBattle(resObj);
                if (resObj[11]?.IndexOf("[") == 0 && resObj[11]?.IndexOf("]") != -1)
                {
                    ParseMultiPokemon(resObj[11]);
                }
                else
                {
                    Console.WriteLine(resObj[11]);
                }

                if (ActiveBattle.BattleHasWon && resObj[9] != "0")
                {
                    var lastMoney = Money;
                    Money = Convert.ToInt32(resObj[9]);
                    if (!resObj[10].Contains("You gained") && !resObj[10].Contains("$"))
                        resObj[10] += $"|You gained ${Money - lastMoney}.";
                    PlayerDataUpdated?.Invoke();
                }

                IsInBattle = !ActiveBattle.BattleHasWon && !ActiveBattle.LostBattle && !ActiveBattle.BattleEnded;
                OnBattleText(resObj[10]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void OnBattleText(string str)
        {
            var battleTexts = str.Split('|');
            var loc2 = 0;

            int totalDelay = battleTexts.Sum(text => Rand.Next(2000, 3000));

            //if (PercentSuccess(0.2) && LastBreakTime.AddMinutes(5) < DateTime.Now)
            //{
            //    LastBreakTime = DateTime.Now;
            //    var breakTime = Rand.Next(5000, 12000) + totalDelay;
            //    PrintSystemMessage($"Taking a break of {breakTime} milliseconds...");

            //    _battleTimeout.Set(breakTime);
            //}
            //else
            //{
                _battleTimeout.Set(totalDelay + Rand.Next(1000, 3500));
            // }

            var lastWPFainted = false;
            while (loc2 < battleTexts.Length)
            {
                var battleOtherText = battleTexts[loc2].Split(',');
                var builder = new System.Text.StringBuilder();

                foreach (var text in battleOtherText)
                {
                    var st = text;

                    if (st.IndexOf(".", StringComparison.Ordinal) != -1)
                        builder.Append(st);
                    else if (st.IndexOf("!", StringComparison.Ordinal) != -1)
                        builder.Append(st.Replace("&#44", ""));

                    if (st.Contains("has fainted"))
                    {
                        var lastPoke = WildPokemons.LastOrDefault();
                        if (st.Contains(lastPoke.Name))
                            if (lastPoke.Name == Team[ActiveBattle.ActivePokemon].Name)
                                lastWPFainted = Team[ActiveBattle.ActivePokemon].CurrentHealth > 0;
                            else
                                lastWPFainted = true;
                    }
                }

                BattleMessage?.Invoke(builder.ToString());
                InventoryUpdated?.Invoke();
                loc2++;
            }

            if (WildPokemons.Count > 150)
                WildPokemons.Clear();

            if (!WildPokemons.Contains(ActiveBattle.WildPokemon))
            {
                if (lastWPFainted)
                    WildPokemons.LastOrDefault()?.UpdateHealth(0, -1);
                WildPokemons.Add(ActiveBattle.WildPokemon);
            }
            else
            {
                WildPokemons[WildPokemons.ToList().FindIndex(wp => wp == ActiveBattle.WildPokemon)] = ActiveBattle.WildPokemon;
            }

            EnemyUpdated?.Invoke(WildPokemons, ActiveBattle.ActivePokemon);

            if (!IsInBattle)
            {
                if (ActiveBattle.LostBattle)
                {
                    LoadMap(true, TempMap, PlayerX, PlayerY, false, MapName);
                    EndBattle(true);
                }
                else
                {
                    EndBattle();
                }
            }
        }

        public void EndBattle(bool lostBattle = false)
        {
            //if (PercentSuccess(0.5) && LastBreakTime.AddMinutes(5) < DateTime.Now)
            //{
            //    LastBreakTime = DateTime.Now;
            //    var breakTime = Rand.Next(15000, 75000);

            //    PrintSystemMessage($"Taking a break of {breakTime} milliseconds...");


            //    _battleTimeout.Set(breakTime);

            //}
            //else
            //{
                _battleTimeout.Set(Rand.Next(3500, 5500));
            // }

            LastBattle = ActiveBattle;
            ActiveBattle = null;
            IsTrapped = false;
            _isBusy = false;

            for (int i = 0; i < Team[LastBattle.ActivePokemon].Moves.Length; i++)
            {
                Team[LastBattle.ActivePokemon].Moves[i].CurrentPoints = 999;
                Team[LastBattle.ActivePokemon].Moves[i].MaxPoints = 999;
            }

            GetTimeStamp("updateFollowPokemon");

            if (!lostBattle)
                GetTimeStamp("r");
            else
                _needToLoadR = true;


            IsInBattle = false;
            IsTrainerBattle = false;
            movingForBattle = false;
            CanMove = true;
            int battleStatus = (LastBattle.BattleHasWon ? 1 : 0) << 2 | (lostBattle ? 1 : 0) << 1 | (LastBattle.BattleEnded ? 1 : 0) << 0;
            BattleEnded?.Invoke(battleStatus);
        }

        public Pokemon ParseOnePokemon(string str)
        {
            if (str != "[]" && str != "")
                if (str.IndexOf("[", StringComparison.Ordinal) != -1 && str.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    str = str.Substring(1, str.Length - 2);
                    var data = str.Split(',');
                    if (data.Length == 40)
                    {
                        var pokemon = new Pokemon(data);
                        return pokemon;
                    }

                    if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
                    TeamUpdated?.Invoke(true);
                }

            return null;
        }

        public int DistanceTo(int posX, int posY)
        {
            return DistanceBetween(PlayerX, PlayerY, posX, posY);
        }

        public static int DistanceBetween(int fromX, int fromY, int toX, int toY)
        {
            return Math.Abs(fromX - toX) + Math.Abs(fromY - toY);
        }

        private void ParseAllMiningRocks(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.IndexOf("[", StringComparison.Ordinal) != -1 && loc2.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    while (loc1 < strArrayA.Length)
                    {
                        var data = "[" + strArrayA[loc1] + "]";
                        MiningObjects.Add(ParseRock(data));
                        loc1 = loc1 + 1;
                    }
                    return;
                }

                Console.WriteLine("parseAllMiningRocks bracket error: " + loc2);
            }
        }

        private MiningObject ParseRock(string tempStr2)
        {
            if (tempStr2 != "[]" && tempStr2 != "")
            {
                if (tempStr2.IndexOf("[", StringComparison.Ordinal) != -1 && tempStr2.IndexOf("]", StringComparison.Ordinal) == tempStr2.Length - 1)
                {
                    tempStr2 = tempStr2.Substring(1, tempStr2.Length - 2);
                    var rock = new MiningObject(tempStr2.Split(','));
                    return rock;
                }

                Console.WriteLine("parseArray bracket error");
            }

            return null;
        }

        private void MapUpdate(string packet)
        {
            try
            {
                Players.Clear();
                packet = ObjectSerilizer.DecodeEntities(packet);
                var xml = new XmlDocument();
                xml.LoadXml(packet);
                var xmlDocument = XDocument.Parse(packet);
                var csl = xmlDocument.Descendants("obj").FirstOrDefault(o => o.Attribute("o")?.Value != null)?.Attribute("o")?.Value;
                if (csl != null && csl.ToLowerInvariant() == "sl")
                {
                    Shop = new Shop(xmlDocument);
                }
                else
                {
                    Shop = null;
                    OpenedShop = null;
                }
                var node = xml.DocumentElement?.GetElementsByTagName("dataObj")[0];
                if (node != null)
                    foreach (XmlElement textNode in node)
                        if (textNode.GetAttribute("n") != "" && textNode.InnerText != "")
                            if (textNode.GetAttribute("n") == "mr")
                                ParseAllMiningRocks(textNode.InnerText);
#if DEBUG
                Console.WriteLine("Xml from server: " + packet);
#endif
                TeleportationOccuring?.Invoke(MapName, PlayerX, PlayerY);
                _updatedMap = true;
                _loadingTimeout.Set(Rand.Next(2000, 3000));
                MapUpdated?.Invoke();
            }
            catch (Exception)
            {
                //ignore
            }
        }
        private void MapUpdate(string[] data)
        {
            try
            {
                Players.Clear();
                _removedPlayers.Clear();
                if (data[4] != "")
                {
                    Shop = new Shop(data[4]);
                }

                if (data[6] != "")
                {
                    ParseAllPortablePc(data[6]);
                }
                if (data[7] != "")
                {
                    ParseAllMiningRocks(data[7]);
                }

                if (data[15] != "")
                {
                    ParseAllEliteChests(data[15]);
                }

                _mapInstance = data[3] != "" ? Convert.ToInt32(data[3]) : -1;

                if (_avatarType != 0)
                    GetTimeStamp("sendAddPlayer");

                TeleportationOccuring?.Invoke(MapName, PlayerX, PlayerY);
                _updatedMap = true;
                _loadingTimeout.Set(Rand.Next(2000, 3000));
                //_updatePositionTimeout = ExecutionPlan.Repeat(8000, UpdatePosition);
                _lastUpdatePos = DateTime.UtcNow;
                MapUpdated?.Invoke();
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void ParseAllPortablePc(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.LastIndexOf("[", StringComparison.Ordinal) != -1 && loc2.LastIndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    while (loc1 < strArrayA.Length)
                    {
                        var data = "[" + strArrayA[loc1] + "]";
                        PortablePcList.Add(ParsePortablePc(data));
                        loc1 = loc1 + 1;
                    }
                    return;
                }

                Console.WriteLine("parseAllPortablePc bracket error: " + loc2);
            }
        }

        private PortablePc ParsePortablePc(string tmpdata)
        {
            if (tmpdata != "[]" && tmpdata != "")
            {
                if (tmpdata.IndexOf("[", StringComparison.Ordinal) != -1 && tmpdata.IndexOf("]", StringComparison.Ordinal) == tmpdata.Length - 1)
                {
                    tmpdata = tmpdata.Substring(1, tmpdata.Length - 2);
                    var data = tmpdata.Split(',');
                    var portablePc = new PortablePc()
                    {
                        X = Convert.ToInt32(data[0]),
                        Y = Convert.ToInt32(data[1]),
                        Owner = data[2]
                    };
#if DEBUG
                    PrintSystemMessage($"{portablePc.X}, {portablePc.Y}, {portablePc.Owner}");
#endif
                    return portablePc;
                }
            }

            return null;
        }
        private void ParseAllEliteChests(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.IndexOf("[", StringComparison.Ordinal) != -1 && loc2.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    while (loc1 < strArrayA.Length)
                    {
                        var data = "[" + strArrayA[loc1] + "]";
                        EliteChests.Add(ParseChest(data));
                        loc1 = loc1 + 1;
                    }
                    return;
                }

                Console.WriteLine("parseAllMiningRocks bracket error: " + loc2);
            }
        }

        private EliteChest ParseChest(string tempStr2)
        {
            if (tempStr2 != "[]" && tempStr2 != "")
            {
                if (tempStr2.IndexOf("[", StringComparison.Ordinal) != -1 && tempStr2.IndexOf("]", StringComparison.Ordinal) == tempStr2.Length - 1)
                {
                    tempStr2 = tempStr2.Substring(1, tempStr2.Length - 2);
                    var rock = new EliteChest(tempStr2.Split(','));
                    return rock;
                }

                Console.WriteLine("parseArray bracket error");
            }

            return null;
        }

        public static string[] ParseArray(string tempStr2)
        {
            if (tempStr2 != "[]" && tempStr2 != "")
            {
                if (tempStr2.IndexOf("[", StringComparison.Ordinal) != -1 && tempStr2.IndexOf("]", StringComparison.Ordinal) == tempStr2.Length - 1)
                {
                    tempStr2 = tempStr2.Substring(1, tempStr2.Length - 2);
                    var rock = tempStr2.Split(',');
                    return rock;
                }

                Console.WriteLine("parseArray bracket error");
            }

            return new string[0];
        }

        public bool BuyItem(int itemId, int quantity)
        {
            if (OpenedShop != null && OpenedShop.ShopItems.Any(item => item.Uid == itemId))
            {
                _itemUseTimeout.Set();
                SendShopPokemart(itemId, quantity);
                return true;
            }

            return false;
        }

        public bool HealFromPc()
        {
            GetTimeStamp("portablePcHeal");
            return true;
        }

        private void SendShopPokemart(int itemId, int quantity)
        {
            GetTimeStamp("buyItem", itemId.ToString(), quantity.ToString());
        }
        //Stupid SWF handles player things like below lol :D
        private void HandleStartingInfo(string[] resObj)
        {
            Money = Convert.ToInt32(resObj[3]);
            Credits = Convert.ToInt32(resObj[4]);
            var loopNum = 0;
            //`Map,1`Pokedex,1`Potion,7`Escape Rope,2`Backpack,1`Great Ball (untradeable),10`Ultra Ball (untradeable),5`Christmas Present,3`Revive,1`Great Ball,1`)()(09a0jd
            // ReSharper disable once AccessToModifiedClosure
            for (int loc7 = 4; loc7 < 99999; ++loc7)
            {
                if (resObj[loc7] != ")()(09a0jd")
                {
                    // ReSharper disable once AccessToModifiedClosure
                    OnInventoryUpdate(resObj[loc7]);
                    continue;
                }
                loopNum = loc7;
                break;
            }
            Badges = resObj[loopNum + 1].Split(',').ToList();
            PlayerX = Convert.ToInt32(resObj[loopNum + 2]);
            PlayerY = Convert.ToInt32(resObj[loopNum + 3]);
            EncryptedTileX = ObjectSerilizer.CalcMd5(PlayerX + _kg1 + Username);
            EncryptedTileY = ObjectSerilizer.CalcMd5(PlayerY + _kg1 + Username);
            LastUpdateX = PlayerX;
            LastUpdateY = PlayerY;
            MapName = resObj[loopNum + 4];
            MapName = MapName.Replace(" (", "").Replace(")", "");
            EncryptedMap =
                ObjectSerilizer.CalcMd5(MapName + "dlod02jhznpd02jdhggyambya8201201nfbmj209ahao8rh2pb" + Username);
            PlayerDataUpdated?.Invoke();
            for (var _loc7 = loopNum + 5; _loc7 < 99999; ++_loc7)
            {
                if (resObj[_loc7] != ")()(09a0jc")
                {
                    continue;
                }
                loopNum = _loc7;
                break;
            }
            _avatarType = Convert.ToInt32(resObj[loopNum + 3]);
            IsCreatingCharacter = _avatarType == 0;
            MemberType = resObj[loopNum + 4];
            int.TryParse(resObj[loopNum + 5], out var time);
            MemberTime = time;
            Clan = resObj[loopNum + 6] == "0" ? "" : resObj[loopNum + 6];
            //User Pokemons
            //[7216762,150,46,46,52,41,38,72,0,0,66,39,5,10,29,1,6,15,0,23,hardy,82,82,52,108,0,10,72,58332,2142,false,26,5,Charmeleon,none,66,,0,Professor Oak,default]`[9388091,148,38,48,52,48,58,78,0,0,44,12,7,41,24,5,9,29,24,18,lax,95,33,109,36,0,1,78,54672,1163,false,25,234,Stantler,none,119,,0,Nuhash2004,default]`
            Team.Clear();
            for (var loc7 = loopNum + 19; loc7 < 99999; ++loc7)
            {
                if (resObj[loc7] != ")()(09a0jb")
                {
                    ParsePokemon(resObj[loc7]);
                    continue;
                }

                loopNum = loc7;
                break;
            }
            if (!_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
            TeamUpdated?.Invoke(true);
            if (Team != null && Team.Count > 0)
                GetTimeStamp("updateFollowPokemon");

            var pokes = new List<Pokemon>();
            for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
            {
                if (resObj[loc7] != ")()(09a0ja")
                {
                    pokes.Add(ParseOnePokemon(resObj[loc7]));
                    continue;
                }
                loopNum = loc7;
                break;
            }
            PCPokemon[1] = pokes;

            for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
            {
                if (resObj[loc7] != ")()(09a0jz")
                {
                    continue;
                }
                loopNum = loc7;
                break;
            }

            _lastSentPing = DateTime.UtcNow;
            _lastSavedData = DateTime.UtcNow;

            if (_avatarType > 0)
                LoadMap(false, MapName);

            for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
            {
                if (resObj[loc7] != ")()(09a0js")
                {
                    continue;
                }
                loopNum = loc7;
                break;
            }

            _movementSpeedMod = 1;

            if (Convert.ToInt32(resObj[loopNum + 10]) > 0)
            {
                if (Convert.ToInt32(resObj[loopNum + 9]) == 2)
                {
                    _movementSpeedMod = 2;
                }
                else if (Convert.ToDouble(resObj[loopNum + 9]) == 0.500000)
                    _movementSpeedMod = 0.500000;
            }

            _mapMovementSpeed = 8 * _movementSpeedMod;


            Fishing = new FishingExtentions(resObj[loopNum + 11], resObj[loopNum + 12], resObj[loopNum + 13]);
            PokemonCaught = ParseStringArray(resObj[loopNum + 36]);
            Mining = new MiningExtentions(resObj[loopNum + 48], resObj[loopNum + 49], resObj[loopNum + 50]);

            PCPokemon[2] = ParseMultiPokemonToList(resObj[loopNum + 25]);
            PCPokemon[3] = ParseMultiPokemonToList(resObj[loopNum + 26]);
            PCPokemon[4] = ParseMultiPokemonToList(resObj[loopNum + 27]);
            PCPokemon[5] = ParseMultiPokemonToList(resObj[loopNum + 28]);
            PCPokemon[6] = ParseMultiPokemonToList(resObj[loopNum + 29]);
            PCPokemon[7] = ParseMultiPokemonToList(resObj[loopNum + 59]);
            PCPokemon[8] = ParseMultiPokemonToList(resObj[loopNum + 60]);
            PCPokemon[9] = ParseMultiPokemonToList(resObj[loopNum + 61]);
            PCPokemon[10] = ParseMultiPokemonToList(resObj[loopNum + 62]);

            TileZ = Convert.ToInt32(resObj[loopNum + 54]);
            LastUpdateZ = TileZ;

            LoggedIn?.Invoke();
            _checkForLoggingTimeout?.Dispose();
            _checkForLoggingTimeout = null;
            //updatePositionTimeout = ExecutionPlan.Repeat(8000, UpdatePosition);
            _lastUpdatePos = DateTime.UtcNow;
        }

        private void ParsePokemon(string str)
        {
            if (str != "[]" && str != "")
                if (str.IndexOf("[", StringComparison.Ordinal) != -1 && str.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    str = str.Substring(1, str.Length - 2);
                    var data = str.Split(',');
                    if (data.Length == 41)
                    {
                        var pokemon = new Pokemon(data);
                        if (!Team.Any(poke => pokemon.UniqueId == poke.UniqueId))
                            Team.Add(pokemon);
                        var index = Team.FindIndex(poke => pokemon.UniqueId == poke.UniqueId);
                        Team[index] = pokemon;
                        Team[index].Uid = index + 1;
                    }
                }
        }

        private void ParseMultiPokemon(string str)
        {
            if (str != "[]" && str != "")
            {
                // ReSharper disable once StringIndexOfIsCultureSpecific.1
                // ReSharper disable once ConstantConditionalAccessQualifier
                if (str?.IndexOf("[") == 0 && str?.IndexOf("]") != -1)
                {
                    str = str.Substring(2, str.Length - 4);
                    str = str.Replace("],[", "#");
                    var loc2 = str.Split('#');
                    var loc1 = 0;
                    Team.Clear();
                    while (loc1 < loc2.Length)
                    {
                        loc2[loc1] = "[" + loc2[loc1] + "]";
                        ParsePokemon(loc2[loc1]);
                        loc1++;
                    }

                    if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
                    TeamUpdated?.Invoke(true);
                    return;
                }
#if DEBUG
                Console.WriteLine("ParseMultiPokemon bracket error");
#endif
            }
        }

        private List<Pokemon> ParseMultiPokemonToList(string str)
        {
            if (str != "[]" && str != "")
            {
                // ReSharper disable once StringIndexOfIsCultureSpecific.1
                // ReSharper disable once ConstantConditionalAccessQualifier
                if (str?.IndexOf("[") == 0 && str?.IndexOf("]") != -1)
                {
                    str = str.Substring(2, str.Length - 4);
                    str = str.Replace("],[", "#");
                    var loc2 = str.Split('#');
                    var loc1 = 0;
                    var pokes = new List<Pokemon>();
                    while (loc1 < loc2.Length)
                    {
                        loc2[loc1] = "[" + loc2[loc1] + "]";
                        pokes.Add(ParseOnePokemon(loc2[loc1]));
                        loc1++;
                    }
                    return pokes;
                }
#if DEBUG
                Console.WriteLine("ParseMultiPokemon bracket error");
#endif
            }
            return new List<Pokemon>();
        }

        public bool LoadMap(bool switchingMap, string tempMap, int x = int.MinValue, int y = int.MinValue,
            bool customMap = false, string oldMapBattleLost = "", int l = 0)
        {
            _updatedMap = false;
            movingForBattle = false;
            _movementTimeout.Cancel();
            _movements.Clear();

            MiningObjects.Clear();
            PortablePcList.Clear();
            Shop = null;
            OpenedShop = null;
            if (x == int.MinValue)
                x = PlayerX;
            if (y == int.MinValue)
                y = PlayerY;

            if (l == 1)
                lm = true;

            if (IsFishing)
                StopFishing();
            if (IsMinning)
                StopMining(false);

            _loadingTimeout.Set(Rand.Next(2000, 3000));
            //_updatePositionTimeout?.Dispose();
            _lastUpdatePos = DateTime.MaxValue;

            GetTimeStamp("removePlayer", oldMapBattleLost);
            _updatedMap = false;
            _mapInstance = -1;
            if (switchingMap)
            {
                MapName = tempMap;
                //TempMap = "";
                EncryptedMap =
                    ObjectSerilizer.CalcMd5(MapName + "dlod02jhznpd02jdhggyambya8201201nfbmj209ahao8rh2pb" + Username);
                PlayerX = x;
                PlayerY = y;
                TileZ = 0;
                EncryptedTileX = ObjectSerilizer.CalcMd5(PlayerX + _kg1 + Username);
                EncryptedTileY = ObjectSerilizer.CalcMd5(PlayerY + _kg1 + Username);
            }

            if (_needToLoadR)
            {
                GetTimeStamp("r");
                _needToLoadR = false;
            }

            return true;
        }

        private void HandleUseItem2(string[] data)
        {
            _isBusy = false;
            OnInventoryUpdate(data[3]);
            ParseMultiPokemon(data[4]);
            PrintSystemMessage(data[10]);
            if (data[6] != "")
            {
                _avatarType = Convert.ToInt32(data[6]);
                CanMove = false;
                IsCreatingCharacter = true;
            }
            if (data[7] != "")
            {
                Money = Convert.ToInt32(data[7]);
            }
            if (data[9] == "2")
            {
                _movementSpeedMod = 2;
            }
            else if (data[9] == "0.5")
            {
                _movementSpeedMod = 0.500000;
            }
            else if (data[9] == "1")
            {
                _movementSpeedMod = 1;
            }
            if (IsBiking)
            {
                _mapMovementSpeed = 16 * _movementSpeedMod;

            }
            else if (IsSurfing && HasItemName("Surfboard"))
            {
                _mapMovementSpeed = 16 * _movementSpeedMod;
            }
            else
            {
                _mapMovementSpeed = 8 * _movementSpeedMod;
            }

            if (data[11] != "")
            {
                if (IsFishing == true)
                {
                    StopFishing();
                }
                if (IsMinning == true)
                {
                    StopMining(false);
                }
                CanMove = false;
                if (data[11] == "Indigo Plateau")
                {
                    LoadMap(true, data[11], 8, 20);
                }
                else if (data[11] == "Hoenn Pokemon League Lobby")
                {
                    LoadMap(true, data[11], 15, 15);
                }
                else if (data[11] == "Accumula Pokecenter" || data[11] == "Striaton Pokecenter" || data[11] == "Nacrene Pokecenter" || data[11] == "Castelia Pokecenter" || data[11] == "Nimbasa Pokecenter" || data[11] == "Driftveil Pokecenter" || data[11] == "Mistralton Pokecenter" || data[11] == "Icirrus Pokecenter" || data[11] == "Opelucid Pokecenter" || data[11] == "Lacunosa Pokecenter" || data[11] == "Undella Pokecenter" || data[11] == "Lentimas Pokecenter" || data[11] == "Black City Pokecenter" || data[11] == "Humilau Pokecenter" || data[11] == "Unova Victory Road Pokecenter")
                {
                    LoadMap(true, data[11], 10, 17);
                }
                else
                {
                    LoadMap(true, data[11], 19, 14);
                }
            }
        }

        private void OnInventoryUpdate(string data)
        {
            string[] n;
            if (data.Contains(")()(09a0jd")) return;
            if (data.Contains("[") && data.Contains("]"))
            {
                if (data.IndexOf("[") == 0 && data.IndexOf("]") != -1)
                {
                    Items.Clear();
                    data = data.Substring(2, data.Length - 4);
                    data = data.Replace("],[", "#");
                    var loc2 = data.Split('#');
                    foreach (var q in loc2)
                    {
                        n = q.Split(',');
                        var item = new InventoryItem(n[0], Convert.ToInt32(n[1]));
                        Items.Add(item);
                        item.Uid = Items.IndexOf(item);
                    }
                }
                goto UPDATE;
            }
            n = data.Split(',');
            if (Regex.IsMatch(n[0], @"^\d+$")) return; // Check if first item's name is all numeric
            if (n.Length > 2)
            {
                var item = new InventoryItem(n[0], Convert.ToInt32(data[1]), Convert.ToInt32(n[2]));
                Items.Add(item);
                item.Uid = Items.IndexOf(item);
                Items[Items.IndexOf(item)].Uid = Items.IndexOf(item);
            }
            else if (n.Length > 1)
            {
                var item = new InventoryItem(n[0], Convert.ToInt32(n[1]));
                Items.Add(item);
                item.Uid = Items.IndexOf(item);
                Items[Items.IndexOf(item)].Uid = Items.IndexOf(item);
            }
            else
            {
                var item = new InventoryItem(n[0]);
                Items.Add(item);
                item.Uid = Items.IndexOf(item);
                Items[Items.IndexOf(item)].Uid = Items.IndexOf(item);
            }
        UPDATE:
            Items = Items.OrderBy(o => o.Uid).ToList();
            _itemUseTimeout.Set(Rand.Next(1000, 1500));
            InventoryUpdated?.Invoke();
        }

        // xt`b`-1`K4M41G4M3R`25`28`right`24785````Male7%35-35-35`Male1%35-35-35`Male2%15-55-35`Both5%70-70-70`TanMale``373`bike`0`-1`0`%undefined`%undefined`%undefined`Arcanine Mount`
        private void OnAddPlayer(string[] data, bool addBack)
        {
            bool isNewPlayer = false;
            PlayerInfos player;
            DateTime expiration = DateTime.UtcNow.AddSeconds(20);
            if (Players.ContainsKey(data[3]))
            {
                player = Players[data[3]];
                player.Expiration = expiration;
            }
            else
            {
                isNewPlayer = true;
                player = new PlayerInfos(expiration);
                player.Name = data[3];
            }

            player.Updated = DateTime.UtcNow;
            player.PosX = Convert.ToInt32(data[4]);
            player.PosY = Convert.ToInt32(data[5]);
            player.Direction = data[6];
            player.Id = Convert.ToInt32(data[7]);
            // setting skin values from index 11 to 15 and from 22 to 25
            for (int i = 0; i < 5; i++)
            {
                player.Skin += data[11 + i] + "`";
            }
            for (int i = 0; i < 4; i++)
            {
                player.Skin += data[22 + i] + "`";
            }

            player.PlayerType = Convert.ToInt32(data[20]);

            var petId = Convert.ToInt32(data[17]);
            player.PokemonPetId = petId > _shinyDifference ? petId - _shinyDifference : petId;
            player.IsPokemonPetShiny = petId > _shinyDifference;

            player.IsOnground = data[21] == "0" && data[18] == ""; // useless....

            Players[player.Name] = player;

            if (_removedPlayers.ContainsKey(player.Name))
                _removedPlayers.Remove(player.Name);

            if (isNewPlayer)
            {
                PlayerAdded?.Invoke(player);
            }
            else
            {
                PlayerUpdated?.Invoke(player);
            }

            double offsetAmount = 0;

            if (_movements.Count > 0)
                offsetAmount = 64 - _mapMovements;
            if (_avatarType != 0 && addBack)
                GetTimeStamp("sendAddPlayerTarget", data[3], offsetAmount.ToString());
        }

        private void OnPlayerMovement(string[] data)
        {
            if (Players.ContainsKey(data[4]))
            {
                var player = Players[data[4]];
                int destX = player.PosX;
                int destY = player.PosY;
                DirectionExtensions.ApplyToCoordinates(DirectionExtensions.FromChar(data[3][0]), ref destX, ref destY);
                player.PosX = destX;
                player.PosY = destY;
                player.Expiration = DateTime.UtcNow.AddSeconds(20);
                player.Updated = DateTime.UtcNow;
                Players[data[4]] = player;
                PlayerUpdated?.Invoke(Players[data[4]]);
            }
            else if (_removedPlayers.ContainsKey(data[4]))
            {
                var player = _removedPlayers[data[4]];
                int destX = player.PosX;
                int destY = player.PosY;
                DirectionExtensions.ApplyToCoordinates(DirectionExtensions.FromChar(data[3][0]), ref destX, ref destY);
                player.PosX = destX;
                player.PosY = destY;
                player.Expiration = DateTime.UtcNow.AddSeconds(20);
                player.Updated = DateTime.UtcNow;
                Players.Add(player.Name, player);
                _removedPlayers.Remove(player.Name);
                PlayerAdded?.Invoke(Players[data[4]]);
            }
        }

        private void SendPacket(string packet)
        {
#if DEBUG
            Console.WriteLine("[>] " + packet);
#endif
            _gameConnection.Send(packet);
        }

        private void SendCmd(string header, string action, object fromRoom, string message)
        {
            SendPacket($"<msg {header[0]}='{header.Substring(1)}'><body action=\'{action}\' r=\'{fromRoom}\'>{message}</body></msg>");
        }

        private void SendXtMessage(string xtName, string cmdName, ArrayList ps, string type, bool sendEnc = true,
            int roomId = 1)
        {
            var timer = GetTimer().ToString();
            var packetKey = ObjectSerilizer.GenerateRandomString(Rand.Next(5, 20));
            var encP = ObjectSerilizer.CalcMd5(packetKey + _kg2 + timer);

            if (type == "" || type == "xml")
            {
                var list = new ArrayList();
                list.Add($"name:{xtName}");
                list.Add($"cmd:{cmdName}");
                if (sendEnc)
                {
                    ps.Add("pke:" + encP);
                    ps.Add("pk:" + packetKey);
                    ps.Add("te:" + timer);
                }
                var srializedText = ObjectSerilizer.Serialize(list, ps);

                var packet = $"<![CDATA[{srializedText}]]>";
                SendCmd("txt", "xtReq", roomId, packet);
            }
            else if (type == "str")
            {
                var packet = $"`xt`{xtName}`{cmdName}`{roomId}`";
                if (sendEnc)
                {
                    packet += $"{timer}`{packetKey}`{encP}`";
                }
                if (ps != null)
                {
                    if (ps.Count > 0)
                    {
                        foreach (var p in ps)
                        {
                            packet += p + "`";
                        }
                    }
                }
                SendPacket(packet);
            }
        }

        public void SendAuthentication(string username, string password)
        {
            SetUserInfos(username, password);
            var packet = $"<login z=\'" + Zone + "\'><nick><![CDATA[" + username + "]]></nick><pword><![CDATA[" + password + "]]></pword></login>";
            SendCmd("tsys", "login", 0, packet);
        }

        private string[] ParseStringArray(string tempStr2)
        {
            if (tempStr2 != "[]" && tempStr2 != "")
            {
                if (tempStr2.IndexOf("[", StringComparison.Ordinal) != -1 && tempStr2.IndexOf("]", StringComparison.Ordinal) == tempStr2.Length - 1)
                {
                    tempStr2 = tempStr2.Substring(1, tempStr2.Length - 2);
                    var returnArray2 = tempStr2.Split(',');
                    return returnArray2;
                }

                Console.WriteLine("parseArray bracket error");
            }

            return null;
        }

        //Pokemon Planet sends packet like below, I am lazy to re-code it :p
        public void GetTimeStamp(string type, string p1 = "", string p2 = "", string p3 = "", string p4 = "")
        {
            if (!IsConnected) return;

            var loc8 = new ArrayList();
            var timer = GetTimer().ToString();
            var packetKey = ObjectSerilizer.GenerateRandomString(Rand.Next(5, 20));
            var encP = ObjectSerilizer.CalcMd5(packetKey + _kg2 + timer);

            if (type == "forgetMove")
            {
                loc8.Add($"moveNum:{p1}");
                SendXtMessage("PokemonPlanetExt", "b0", loc8, "xml");
            }
            else if (type == "command")
            {
                loc8.Add($"command:{p1}");
                SendXtMessage("PokemonPlanetExt", "b4", loc8, "xml");
            }
            else if (type == "acceptMerchantItem")
            {
                loc8.Add($"merchantId:{p1}");
                SendXtMessage("PokemonPlanetExt", "b20", loc8, "xml");
            }
            else if (type == "updateMap")
            {
                if (lm)
                {
                    loc8.Add("l:1");
                    lm = false;
                }
                loc8.Add("y:" + PlayerY);
                loc8.Add("x:" + PlayerX);
                loc8.Add("map:" + p1);
                SendXtMessage("PokemonPlanetExt", "b5", loc8, "xml");
            }
            else if (type == "buyItem")
            {
                loc8.Add($"amount:{p2}");
                loc8.Add($"buyNum:{p1}");
                SendXtMessage("PokemonPlanetExt", "b8", loc8, "xml");
            }
            else if (type == "stepsWalked")
            {
                SendXtMessage("PokemonPlanetExt", "b38", loc8, "xml");
            }
            else if (type == "useItem")
            {
                loc8.Add($"itemNum:{p1}");
                SendXtMessage("PokemonPlanetExt", "b1", loc8, "xml");
            }
            else if (type == "useItem2")
            {
                loc8.Add($"i:{p1}");
                loc8.Add($"p:{p2}");
                SendXtMessage("PokemonPlanetExt", "b11", loc8, "xml");
            }
            else if (type == "acceptEvolve")
            {
                SendXtMessage("PokemonPlanetExt", "b18", loc8, "xml");
            }
            else if (type == "declineEvolve")
            {
                SendXtMessage("PokemonPlanetExt", "b19", loc8, "xml");
            }
            else if (type == "declineBattle")
            {
                SendXtMessage("PokemonPlanetExt", "b14", loc8, "xml");
            }
            else if (type == "getHM")
            {
                loc8.Add($"hmNum:{p1}");
                SendXtMessage("PokemonPlanetExt", "b22", loc8, "xml");
            }
            else if (type == "getItem")
            {
                loc8.Add($"itemName:{p1}");
                SendXtMessage("PokemonPlanetExt", "b23", loc8, "xml");
            }
            else if (type == "addCash")
            {
                loc8.Add($"amount:{p1}");
                SendXtMessage("PokemonPlanetExt", "b30", loc8, "xml");
            }
            else if (type == "declineTrade")
            {
                SendXtMessage("PokemonPlanetExt", "b17", loc8, "xml");
            }
            else if (type == "declineClanInvite")
            {
                SendXtMessage("PokemonPlanetExt",
                    ObjectSerilizer.CalcMd5("declineClanInvitekzf76adngjfdgh12m7mdlbfi9proa15gjqp0sd3mo1lk7w90cd" +
                                       Username), loc8, "xml");
            }
            else if (type == "choosePokemon")
            {
                loc8.Add($"pokemon:{p1}");
                SendXtMessage("PokemonPlanetExt", "b7", loc8, "xml");
            }
            else if (type == "reorderStoragePokemon")
            {
                loc8.Add($"t:{p3}");
                loc8.Add($"num2:{p2}");
                loc8.Add($"num1:{p1}");
                SendXtMessage("PokemonPlanetExt", "b6", loc8, "xml");
            }
            else if (type == "saveData")
            {
                SendXtMessage("PokemonPlanetExt", "b26", null, "xml", false);
            }
            else if (type == "updateXYZ")
            {
                loc8.Add(PlayerX);
                loc8.Add(PlayerY);
                loc8.Add(EncryptedTileX);
                loc8.Add(EncryptedTileY);
                loc8.Add(TileZ);
                loc8.Add(MapName);
                SendXtMessage("PokemonPlanetExt", "r8", loc8, "str", false);
            }
            else if (type == "r")
            {
                SendXtMessage("PokemonPlanetExt", "r", null, "str", false);
            }
            else if (type == "asf8n2fs")
            {
                loc8.Add(p1); // mouse X
                loc8.Add(p2); // mouse Y
                loc8.Add(p3); // Timer
                loc8.Add(p4); // Id
                SendXtMessage("PokemonPlanetExt", "b68", loc8, "str", false);
            }
            else if (type == "asf8n2fa")
            {
                loc8.Add(p1); // key
                loc8.Add(p2); // Timer
                loc8.Add(p3); // Id
                SendXtMessage("PokemonPlanetExt", "b69", loc8, "str", false);
            }
            else if (type == "sendMovement")
            {
                loc8.Add(p1);
                if (IsBiking)
                {
                    loc8.Add("b");
                }
                else if (IsSurfing)
                {
                    if (_mapMovementSpeed >= 16)
                    {
                        loc8.Add("z");
                    }
                    else
                    {
                        loc8.Add("s");
                    }
                }

                SendXtMessage("PokemonPlanetExt", "m", loc8, "str", false);
            }
            else if (type == "updateMount")
            {
                if (p1 == "")
                    p1 = "0";
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b191", loc8, "str", false);
            }
            else if (type == "sendFishAnimation")
            {
                loc8.Add(_lastMovement.AsChar());
                SendXtMessage("PokemonPlanetExt", "f", loc8, "str", false);
            }
            else if (type == "sendMineAnimation")
            {
                loc8.Add(_lastMovement.AsChar());
                SendXtMessage("PokemonPlanetExt", "f2", loc8, "str", false);
            }
            else if (type == "sendStopMineAnimation")
            {
                loc8.Add(_lastMovement.AsChar());
                SendXtMessage("PokemonPlanetExt", "f3", loc8, "str", false);
            }
            else if (type == "battleMove")
            {
                loc8.Add(p1);
                if (p2 != "")
                    loc8.Add(p2);
                else
                    loc8.Add("z");
                if (p3 != "")
                    loc8.Add(p3);
                else
                    loc8.Add("z");
                SendXtMessage("PokemonPlanetExt", "b76", loc8, "str");
            }
            else if (type == "getStartingInfo")
            {
                Console.WriteLine("@@getstartinginfo");
                loc8.Add(HashPassword);
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b61", loc8, "str");
            }
            else if (type == "pmsg")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b66", loc8, "str");
            }
            else if (type == "clanMessage")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b67", loc8, "str");
            }
            else if (type == "pm")
            {
                loc8.Add(p1);
                loc8.Add(p2);
                SendXtMessage("PokemonPlanetExt", "r36", loc8, "str");
            }
            else if (type == "removePlayer")
            {
                loc8.Add(Username);
                if (p1 != "") loc8.Add(p1.ToLowerInvariant());
                SendXtMessage("PokemonPlanetExt", "b74", loc8, "str");
            }
            else if (type == "sendAddPlayer")
            {
                loc8.Add(PlayerX);
                loc8.Add(PlayerY);
                loc8.Add(_lastMovement.AsString());
                loc8.Add(_moveType);
                loc8.Add(_mapInstance != -1 ? MapName + $"({_mapInstance})" : MapName);
                loc8.Add(IsFishing ? "1" : "0");
                loc8.Add(_mount == "" ? "0" : _mount);
                SendXtMessage("PokemonPlanetExt", "b55", loc8, "str");
            }
            else if (type == "sendAddPlayerTarget")
            {
                loc8.Add(PlayerX);
                loc8.Add(PlayerY);
                loc8.Add(_lastMovement.AsString());
                loc8.Add(_moveType);
                loc8.Add(p1.ToLowerInvariant());
                loc8.Add(p2);
                loc8.Add(IsFishing ? "1" : "0");
                loc8.Add(_mount == "" ? "0" : _mount);
                SendXtMessage("PokemonPlanetExt", "b56", loc8, "str");
            }
            else if (type == "removeMoney")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "r11", loc8, "str");
            }
            else if (type == "reorderPokemon")
            {
                loc8.Add(p1);
                loc8.Add(p2);
                SendXtMessage("PokemonPlanetExt", "b2", loc8, "str");
            }
            else if (type == "escapeBattle")
            {
                SendXtMessage("PokemonPlanetExt", "b77", loc8, "str");
            }
            else if (type == "wildBattle")
            {
                loc8.Add(MapName);
                if (p1 == "")
                    loc8.Add(_moveType);
                else
                    loc8.Add(p1);
                loc8.Add(EncryptedMap);
                //loc8.Add("1"); collisionArray[player.tileY][player.tileX] == undefined
                SendXtMessage("PokemonPlanetExt", "b78", loc8, "str");
            }
            else if (type == "trainerBattle")
            {
                loc8.Add(p1); // trainer id
                loc8.Add(p2); // trainer name
                loc8.Add(MapName);
                loc8.Add(EncryptedMap);
                SendXtMessage("PokemonPlanetExt", "b79", loc8, "str");
            }
            else if (type == "switchPokemon")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b80", loc8, "str");
            }
            else if (type == "endBattleDisconnect")
            {
                SendXtMessage("PokemonPlanetExt", "b81", loc8, "str");
            }
            else if (type == "endBattleDisconnect2")
            {
                SendXtMessage("PokemonPlanetExt", "b82", loc8, "str");
            }
            else if (type == "fish")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b70", loc8, "str");
            }
            else if (type == "mine")
            {
                loc8.Add(p1);
                loc8.Add(p2);
                loc8.Add(p3);
                SendXtMessage("PokemonPlanetExt", "b163", loc8, "str");
            }
            else if (type == "goodHook")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b122", loc8, "str");
            }
            else if (type == "equipItem")
            {
                loc8.Add(p1);
                loc8.Add(p2);
                SendXtMessage("PokemonPlanetExt", "b58", loc8, "str");
            }
            else if (type == "unequipItem")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b59", loc8, "str");
            }
            else if (type == "portablePcHeal")
            {
                SendXtMessage("PokemonPlanetExt", "b131", loc8, "str");
            }
            else if (type == "updateFollowPokemon")
            {
                if (Team is null || Team.Count <= 0) return;
                if (Team[0].IsShiny)
                    loc8.Add($"{Team[0].Id + _shinyDifference}");
                else
                    loc8.Add($"{Team[0].Id}");
                SendXtMessage("PokemonPlanetExt", "b75", loc8, "str");
            }
            else if (type == "createCharacter")
            {
                loc8.Add(_characterCreation.Body);
                loc8.Add(_characterCreation.Eyes);
                loc8.Add(_characterCreation.Hair);
                loc8.Add(_characterCreation.Pants);
                loc8.Add(_characterCreation.Shirt);
                loc8.Add(_characterCreation.HairColorR.ToString());
                loc8.Add(_characterCreation.HairColorG.ToString());
                loc8.Add(_characterCreation.HairColorB.ToString());
                loc8.Add(_characterCreation.EyesColorR.ToString());
                loc8.Add(_characterCreation.EyesColorG.ToString());
                loc8.Add(_characterCreation.EyesColorB.ToString());
                loc8.Add(_characterCreation.ShirtColorR.ToString());
                loc8.Add(_characterCreation.ShirtColorG.ToString());
                loc8.Add(_characterCreation.ShirtColorB.ToString());
                loc8.Add(_characterCreation.PantsColorR.ToString());
                loc8.Add(_characterCreation.PantsColorG.ToString());
                loc8.Add(_characterCreation.PantsColorB.ToString());
                loc8.Add(_characterCreation.Face);
                SendXtMessage("PokemonPlanetExt", "b71", loc8, "str");
            }
            else if (type == "eliteBuy")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b182", loc8, "str");
            }
            else if (type == "acceptQuest")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b183", loc8, "str");
            }
            else if (type == "completeQuest")
            {
                loc8.Add(p1);
                SendXtMessage("PokemonPlanetExt", "b184", loc8, "str");
            }
            else if (type == "openChest")
            {
                loc8.Add(p1);
                loc8.Add(p2);
                SendXtMessage("PokemonPlanetExt", "b186", loc8, "str");
            }
        }
        private const int _shinyDifference = 721;
        public bool StopFishing()
        {
            if (!IsFishing) return false;
            _fishingTimeout.Set(Rand.Next(1500, 2000));
            IsFishing = false;
            fishingExecutionPlan?.Dispose();
            return !IsFishing;
        }

        public bool StopMining(bool fromMoving = true)
        {
            if (!IsMinning) return false;
            _miningTimeout.Set(Rand.Next(1000, 1500));
            IsMinning = false;
            miningExecutionPlan?.Dispose();
            if (fromMoving)
                GetTimeStamp("sendStopMineAnimation");
            return !IsMinning;
        }

        public bool ChangePokemon(int changeTo)
        {
            if (IsInBattle && IsConnected && IsMapLoaded)
            {
                _battleTimeout.Set();
                PerformingAction?.Invoke(Actions.SWAPPING_POKEMON | Actions.IN_BATTLE);
                SendAttack("0", "switchPokemon", changeTo.ToString());
                return true;
            }

            return false;
        }

        public bool GetHM(int num)
        {
            bool result = false;

            switch (num)
            {
                case 1 when !HasItemName("HM01 - Cut"):
                    result = true;
                    break;
                case 2 when !HasItemName("HM02 - Fly"):
                    result = true;
                    break;
                case 3 when !HasItemName("HM03 - Surf"):
                    result = true;
                    break;
                case 5 when !HasItemName("HM05 - Flash"):
                    result = true;
                    break;
                default:
                    LogMessage?.Invoke($"Was unable to get HM0{num}.");
                    break;
            }

            if (result)
                GetTimeStamp("getHM", num.ToString());

            return result;
        }

        public bool IsAnyMinableRocks()
        {
            return MiningObjects.Count(rock => !rock.IsMined) > 0;
        }

        public int CountMinableRocks()
        {
            return MiningObjects.Count(rock => !rock.IsMined);
        }

        public bool IsMinable(int x, int y) => IsGoldMember ? MiningObjects.Any(r => r.X == x && r.Y == y && !r.IsMined) :
            MiningObjects.Any(r => r.X == x && r.Y == y && !r.IsGoldMember && !r.IsMined);

        private void SendFishing(string rod)
        {
            int delay = 2100 - Fishing.FishingLevel * 10;
            _fishingTimeout.Set(Rand.Next(3000, 3500) + delay);
            fishingExecutionPlan = ExecutionPlan.Repeat(delay, () => GetTimeStamp("fish", rod));
            GetTimeStamp("sendFishAnimation");
            IsFishing = true;
        }

        private void SendMine(int x, int y, string axe)
        {
            int delay = 2500 - Mining.MiningLevel * 12;
            _miningTimeout.Set(Rand.Next(3000, 3500) + delay);
            miningExecutionPlan = ExecutionPlan.Repeat(delay, () => GetTimeStamp("mine", axe, x.ToString(), y.ToString()));
            IsMinning = true;
            LogMessage?.Invoke($"Trying to mine the rock at (X:{x}, Y:{y})");
            GetTimeStamp("sendMineAnimation");
        }

        private void SendAttack(string battleMove, string battleCmd = "", string extraParam = "")
        {
            _isBusy = true;
            GetTimeStamp("battleMove", battleMove, battleCmd, extraParam);
        }

        public bool Run()
        {
            if (IsInBattle && !ActiveBattle.IsDungeonBattle)
            {
                GetTimeStamp("escapeBattle");
                _battleTimeout.Set(Rand.Next(2000, 2500));
                return true;
            }

            return false;
        }

        public void SendAcceptEvolution()
        {
            GetTimeStamp("acceptEvolve");
        }

        public void SendCancelEvolution()
        {
            GetTimeStamp("declineEvolve");
        }

        public void UseAttack(int move)
        {
            SendAttack(move.ToString());
            _battleTimeout.Set(Rand.Next(1500, 2000));
        }

        public bool HasPokemonInTeam(string pokemonName)
        {
            return FindFirstPokemonInTeam(pokemonName) != null;
        }

        public Pokemon FindFirstPokemonInTeam(string pokemonName)
        {
            return Team.FirstOrDefault(p => p.Name.Equals(pokemonName, StringComparison.InvariantCultureIgnoreCase));
        }

        public InventoryItem GetItemFromName(string itemName)
        {
            return Items.FirstOrDefault(i =>
                i.Name.Equals(itemName,
                    StringComparison.InvariantCultureIgnoreCase) && i.Quantity > 0);
        }

        public InventoryItem GetItemByUid(int itmUid) => Items.FirstOrDefault(i => i.Uid == itmUid && i.Quantity > 0);

        private void SendWildBattle(bool isSurf = false)
        {
            if (IsInBattle)
                return;

            _movements.Clear();
            Shop = null;
            OpenedShop = null;

            _isBusy = true;

            if (isSurf)
                GetTimeStamp("wildBattle", "surf");
            else
                GetTimeStamp("wildBattle");
        }

        public bool StartWildBattle()
        {
            if (IsInBattle)
                return false;

            IsTrainerBattle = false;
            _movementTimeout.Set(Rand.Next(1000, 1500));

            SendWildBattle();
            return true;
        }

        public bool StartSurfWildBattle()
        {
            if (IsInBattle)
                return false;

            IsTrainerBattle = false;
            _movementTimeout.Set(Rand.Next(1000, 1500));

            SendWildBattle(true);
            return true;
        }

        public bool StartTrainerBattle(string id, string trainerName)
        {
            if (IsInBattle) return false;

            IsTrainerBattle = true;
            _npcBattler = new Npc(new[] { id, trainerName, "1", "0" });
            _npcBattleTimeout.Set(Rand.Next(1000, 2000) + 1 * 250);
            //GetTimeStamp("trainerBattle", id, trainerName);
            return true;
        }

        public void TalkToNpc(Npc npc)
        {
            npc.CanBattle = false;
            PerformingAction?.Invoke(Actions.ACTION_KEY);
            GetTimeStamp("trainerBattle", npc.Id.ToString(), npc.Name);
            _dialogTimeout.Set();
        }

        public bool UseItem(string name = "", int pokemonUid = 0)
        {
            if (!(pokemonUid >= 0 && pokemonUid <= 6) || !HasItemName(name))
            {
                return false;
            }

            var item = GetItemFromName(name);
            if (item == null || item.Quantity == 0)
            {
                return false;
            }
            if (pokemonUid == 0) // simple use
            {
                if (!_itemUseTimeout.IsActive && !IsInBattle && item.IsNormallyUsable())
                {
                    PerformingAction?.Invoke(Actions.USING_ITEM);
                    _isBusy = true;
                    GetTimeStamp("useItem2", name, "0");
                    _itemUseTimeout.Set();
                    return true;
                }
                if (!_battleTimeout.IsActive && IsInBattle && item.IsUsableInBattle())
                {
                    PerformingAction?.Invoke(Actions.USING_ITEM | Actions.IN_BATTLE);
                    SendAttack("0", "i", item.Uid.ToString());
                    _battleTimeout.Set();
                    return true;
                }
            }
            else
            {
                if (!_itemUseTimeout.IsActive && !IsInBattle && item.IsNormallyUsable())
                {
                    PerformingAction?.Invoke(Actions.USING_ITEM | Actions.USING_ON_POKEMON);
                    _isBusy = true;
                    GetTimeStamp("useItem2", name, (pokemonUid - 1).ToString());
                    _itemUseTimeout.Set(Rand.Next(1000, 1500));
                    return true;
                }
            }
            return false;
        }

        public bool TakeItemFromPokemon(int pokemonUid)
        {
            if (pokemonUid < 1 || pokemonUid > Team.Count)
            {
                return false;
            }
            if (!_itemUseTimeout.IsActive && Team[pokemonUid - 1].ItemHeld != "")
            {
                PerformingAction?.Invoke(Actions.USING_ITEM | Actions.USING_ON_POKEMON);
                SendTakeItem(pokemonUid - 1);
                _itemUseTimeout.Set();
                return true;
            }
            return false;
        }

        public bool GiveItemToPokemon(int pokemonUid, int itemId)
        {
            if (pokemonUid < 1 || pokemonUid > Team.Count)
            {
                return false;
            }
            var item = GetItemByUid(itemId);
            if (item == null || item.Quantity == 0)
            {
                return false;
            }
            if (!_itemUseTimeout.IsActive && !IsInBattle && item.IsEquipAble(Team[pokemonUid - 1].Name))
            {
                PerformingAction?.Invoke(Actions.USING_ITEM | Actions.USING_ON_POKEMON);
                SendGiveItem(pokemonUid - 1, itemId);
                _itemUseTimeout.Set();
                return true;
            }
            return false;
        }

        public void SendTakeItem(int pokemonUid)
        {
            GetTimeStamp("unequipItem", pokemonUid.ToString());
        }

        public void SendGiveItem(int pokemonUid, int itemUid)
        {
            GetTimeStamp("equipItem", pokemonUid.ToString(), itemUid.ToString());
        }

        private void SendMovement(string tempDir)
        {
            Shop = null;
            OpenedShop = null;
            IsTrainerBattle = false;
            StopMining();
            StopFishing();
            GetTimeStamp("sendMovement", tempDir);
        }

        public bool BuyMerchantItem(string merchantid)
        {
            PerformingAction?.Invoke(Actions.USING_MOVE);
            GetTimeStamp("acceptMerchantItem", merchantid);
            _dialogTimeout.Set();
            return true;
        }

        private void CheckForBattle()
        {
            if (movingForBattle)
            {
                bool startBattle = false;

                if (Team[0].AbilityNo == 1 || Team[0].AbilityNo == 73 || Team[0].AbilityNo == 95)
                {
                    if (Rand.Next(1, 14) == 7)
                    {
                        startBattle = true;
                    }
                }
                else if (Team[0].AbilityNo == 35 || Team[0].AbilityNo == 71)
                {
                    if (Rand.Next(1, 9) <= 2)
                    {
                        startBattle = true;
                    }
                }
                else if (Team[0].AbilityNo == 99)
                {
                    if (Rand.Next(1, 90) <= 15)
                    {
                        startBattle = true;
                    }
                }
                else if (Rand.Next(1, 9) == 7)
                {
                    startBattle = true;
                }
                else if (Team[0].AbilityNo == 1 || Team[0].AbilityNo == 73 || Team[0].AbilityNo == 95)
                {
                    if (Rand.Next(1, 27) == 7)
                    {
                        startBattle = true;
                    }
                }
                else if (Team[0].AbilityNo == 35 || Team[0].AbilityNo == 71)
                {
                    if (Rand.Next(1, 9) == 7)
                    {
                        startBattle = true;
                    }
                }
                else if (Team[0].AbilityNo == 99)
                {
                    if (Rand.Next(1, 180) <= 15)
                    {
                        startBattle = true;
                    }
                }
                else if (Rand.Next(1, 18) == 7)
                {
                    startBattle = true;
                }

                if (startBattle)
                {
                    if (IsSurfing)
                        StartSurfWildBattle();
                    else
                        StartWildBattle();
                }
            }
        }

        public bool CheckMapExits(int x, int y)
        {
            if (EncryptedstepsWalked == ObjectSerilizer.CalcMd5(_stepsWalked + _kg1 + Username))
            {
                _stepsWalked++;
                EncryptedstepsWalked = ObjectSerilizer.CalcMd5(_stepsWalked + _kg1 + Username);
                if (_stepsWalked >= 256)
                {
                    GetTimeStamp("stepsWalked");
                    _stepsWalked = 0;
                    EncryptedstepsWalked =
                        ObjectSerilizer.CalcMd5(_stepsWalked + _kg1 + Username);
                }
            }

            return false;
        }

        public void Move(Direction direction, string reason)
        {
            movingForBattle = reason == "battle" || reason.Contains("surf");
            if (reason.Contains("surf") && !IsSurfing)
            {
                SetMount("", true);
            }
            _movements.Add(direction);
        }

        public void ClearPath()
        {
            _movements.Clear();
        }

        public bool HasItemName(string itemName)
        {
            itemName = itemName.ToLowerInvariant();
            foreach (var item in Items)
                if (item.Name.ToLowerInvariant() == itemName && item.Quantity >= 1)
                    return true;
            return false;
        }

        public bool PokemonUidHasMove(int pokemonUid, string moveName)
        {
            return Team.FirstOrDefault(p => p.Uid == pokemonUid)?.Moves.Any(m =>
                       m.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? false;
        }

        public bool HasMove(string moveName)
        {
            return Team.Any(p =>
                p.Moves.Any(m => m.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        public int GetMovePosition(int pokemonUid, string moveName)
        {
            return Team[pokemonUid].Moves
                       .FirstOrDefault(m =>
                           m.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false)?.Position ??
                   -1;
        }

        public void Update()
        {
            _gameConnection.Update();

            if (!IsAuthenticated) return;

            _loadingTimeout.Update();
            _movementTimeout.Update();
            _battleTimeout.Update();
            _swapTimeout.Update();
            _itemUseTimeout.Update();
            _miningTimeout.Update();
            _fishingTimeout.Update();
            _dialogTimeout.Update();
            _refreshPCTimeout.Update();

            SendRegularPing();
            UpdateMovement();
            UpdatePlayers();
            UpdateNpcBattle();
        }

        private void SendRegularPing()
        {
            /*
             * _pingToServer = ExecutionPlan.Repeat(30000, () => PingServer());
             * _saveData = ExecutionPlan.Repeat(1800000, () => GetTimeStamp("saveData"));
             */
            if ((DateTime.UtcNow - _lastSentPing).TotalMilliseconds >= 30000)
            {
                _lastSentPing = DateTime.UtcNow;
                SendXtMessage("PokemonPlanetExt", "p", null, "str");
            }
            if ((DateTime.UtcNow - _lastSavedData).TotalMilliseconds >= 2400000)
            {
                _lastSavedData = DateTime.UtcNow;
                GetTimeStamp("saveData");
            }
            if ((DateTime.UtcNow - _lastUpdatePos).TotalMilliseconds >= 8000)
            {
                _lastUpdatePos = DateTime.UtcNow;
                UpdatePosition();
            }
        }

        private void UpdateMovement()
        {
            if (!IsMapLoaded) return;

            if (!_movementTimeout.IsActive && _movements.Count > 0)
            {
                HasEncounteredRarePokemon = false;
                var dir = _movements[0];
                _movements.RemoveAt(0);

                if (ApplyMovement(dir))
                {
                    if (_mapMovements < 64)
                        _mapMovements += _mapMovementSpeed;
                    else
                        _mapMovements = 0;

                    if (IsBiking)
                    {
                        _mapMovementSpeed = 16 * _movementSpeedMod;
                        if (_mount == "")
                        {
                            SetMount("Bike");
                        }
                    }
                    else if (IsSurfing && HasItemName("Surfboard"))
                    {
                        _mapMovementSpeed = 16 * _movementSpeedMod;
                    }
                    else
                    {
                        _mapMovementSpeed = 8 * _movementSpeedMod;
                    }

                    SendMovement(dir.AsChar());
                    _lastMovement = dir;
                    CheckMapExits(PlayerX, PlayerY);
                    _movementTimeout.Set(IsBiking ? 125 : 250);
                    CheckForBattle();
                }
            }
        }

        private bool ApplyMovement(Direction direction)
        {
            int destinationX = PlayerX;
            int destinationY = PlayerY;
            if (EncryptedTileX == ObjectSerilizer.CalcMd5(destinationX + _kg1 + Username) && EncryptedTileY == ObjectSerilizer.CalcMd5(destinationY + _kg1 + Username))
            {
                direction.ApplyToCoordinates(ref destinationX, ref destinationY);
                PlayerX = destinationX;
                PlayerY = destinationY;
                PerformingAction?.Invoke(Actions.MOVING_UP << (int)direction);
                PlayerPositionUpdated?.Invoke();
                EncryptedTileX = ObjectSerilizer.CalcMd5(destinationX + _kg1 + Username);
                EncryptedTileY = ObjectSerilizer.CalcMd5(destinationY + _kg1 + Username);
                return true;
            }
            return false;
        }

        private void UpdatePlayers()
        {
            if (_updatePlayers < DateTime.UtcNow)
            {
                foreach (string playerName in Players.Keys.ToArray())
                {
                    if (Players[playerName].IsExpired())
                    {
                        PlayerRemoved?.Invoke(Players[playerName]);
                        if (!_removedPlayers.ContainsKey(playerName))
                            _removedPlayers.Add(playerName, Players[playerName]);
                        Players.Remove(playerName);
                    }
                }
                _updatePlayers = DateTime.UtcNow.AddSeconds(5);
            }
        }

        private void UpdateNpcBattle()
        {
            if (_npcBattler == null || _npcBattleTimeout.Update()) return;
            TalkToNpc(_npcBattler);
            _npcBattler = null;
        }

        public bool SwapPokemon(int pokemon1, int pokemon2)
        {
            if (IsInBattle || pokemon1 < 0 || pokemon2 < 0 || pokemon1 > Team.Count || pokemon2 > Team.Count ||
                pokemon1 == pokemon2) return false;
            if (_swapTimeout.IsActive is false)
            {
                PerformingAction?.Invoke(Actions.SWAPPING_POKEMON);
                SendSwapPokemons(pokemon1 - 1, pokemon2);
                _swapTimeout.Set();
                return true;
            }

            return false;
        }

        private void SendSwapPokemons(int pokemon1, int pokemon2)
        {
            _isBusy = true;
            GetTimeStamp("reorderPokemon", pokemon1.ToString(), pokemon2.ToString());
        }

        public void SendMouseLogs(int x, int y, string id)
        {
            GetTimeStamp("asf8n2fs", x.ToString(), y.ToString(), GetTimer().ToString(), id);
        }

        public void SendKeyLog(string key, string id)
        {
            GetTimeStamp("asf8n2fa", key, GetTimer().ToString(), id);
        }

        public void MineRock(int x, int y, string axe)
        {
            _lastRock = MiningObjects.Find(p => p.X == x && p.Y == y);
            PerformingAction?.Invoke(Actions.ACTION_KEY);
            SendMine(x, y, axe);
        }

        public bool FishWith(string rod)
        {
            if (!IsInBattle && _updatedMap && rod != "" && HasItemName(rod))
            {
                PerformingAction?.Invoke(Actions.ACTION_KEY);
                SendFishing(rod);
                return true;
            }

            if (!HasItemName(rod))
            {
                LogMessage?.Invoke(
                    $"Please make sure you have {rod} in you're inventory. If you know you have then relog and try again.");
                return false;
            }
            return false;
        }

        public bool ChoosePokemon(string name)
        {
            if (Team.Count > 0)
                return false;
            PerformingAction?.Invoke(Actions.USING_MOVE);
            GetTimeStamp("choosePokemon", name.ToLowerInvariant());
            return true;
        }

        public bool WithdrawPokemonFromPC(int box, int boxId)
        {
            if (PCPokemon[box].Count <= 0 || boxId < 1 || boxId > PCPokemon.Count)
                return false;
            PerformingAction?.Invoke(Actions.USING_MOVE);
            PCPokemon[box].RemoveAt(boxId - 1);
            GetTimeStamp("reorderStoragePokemon", "-1", boxId.ToString(), box.ToString());
            _refreshPCTimeout?.Set();
            return true;
        }

        public bool DepositePokemonToPC(int box, int index)
        {
            if (index < 1 || index > Team.Count)
                return false;
            PerformingAction?.Invoke(Actions.USING_MOVE);
            if (Team[index - 1] != null)
                PCPokemon[box].Add(Team[index - 1]);
            GetTimeStamp("reorderStoragePokemon", (index - 1).ToString(), "0", box.ToString());
            _refreshPCTimeout.Set();
            return true;
        }

        public void SetMount(string mount_name, bool isSurf = false)
        {
            _mount = mount_name;
            
            if (_mount == "" && isSurf)
            {
                _mount = "surf";
            }

            if (_mount != "")
                _moveType = isSurf ? "surf" : "bike";
            else
                _moveType = "";

            GetTimeStamp("updateMount", _mount);
        }

        public void RemoveMoney(int amount)
        {
            Money -= amount;
            SendRemoveMoney(amount);
            PlayerDataUpdated?.Invoke();
            _dialogTimeout.Set();
        }

        private void SendRemoveMoney(int amount)
        {
            GetTimeStamp("removeMoney", amount.ToString());
        }

        public void TradeAddMoney(int amount)
        {
            if (Money > amount)
            {
                SendTradeAddMoney(amount);
                _dialogTimeout.Set();
            }
        }

        private void SendTradeAddMoney(int amount)
        {
            GetTimeStamp("addCash", amount.ToString());
        }

        public void GetItem(string name)
        {
            if (!HasItemName(name))
            {
                SendGetItem(name);
                _itemUseTimeout.Set();
            }
        }

        private void SendGetItem(string name)
        {
            GetTimeStamp("getItem", name);
        }

        public void AcceptQuest(int id)
        {
            GetTimeStamp("acceptQuest", id.ToString());
            _dialogTimeout.Set();
        }

        public void CompleteQuest(int id)
        {
            GetTimeStamp("completeQuest", id.ToString());
            _dialogTimeout.Set();
        }
    }
}
