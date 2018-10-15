using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PPOProtocol
{
    public class GameClient
    {
        public const string RareLengendaryPattern = "has encountered a";
        private int _avatarType;
        private double _mapMovements;
        private double _movementSpeedMod;
        private double _mapMovementSpeed;

        private readonly Connection _connection;
        private readonly GameConnection _gameConnection;
        private string _dir = "down";
        private bool _inited;
        private MiningObject _lastRock;
        private int _mapInstance = -1;
        public string _moveType = "";
        private readonly string _url = @"http://pokemon-planet.com/game582.swf";
        private bool movingForBattle;

        private List<Direction> _movements = new List<Direction>();
        private CharacterCreation _characterCreation { get; set; }

        private bool _needToLoadR;
        private ExecutionPlan _pingToServer;

        public string[] PokemonCaught { get; private set; }
        private ExecutionPlan _saveData;
        private ExecutionPlan _updatePositionTimeout;
        private ExecutionPlan _checkForLoggingTimeout;

        private int _stepsWalked;

        private readonly ProtocolTimeout _loadingTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _swapTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _itemTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _fishingTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _battleTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _movementTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _miningTimeout = new ProtocolTimeout();

        private bool _updatedMap { get; set; } = false;

        public bool Battle;
        public bool CanMove = true;
        public int Credits;
        public string EncryptedPacketCount;
        public string EncryptedstepsWalked;
        public Battle LastBattle;
        public bool HasEncounteredRarePokemon;

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

        public int PacketCount = -1;
        public Random Rand = new Random();

        public DateTime Timer;
        public Shop OpenedShop { get; private set; }
        private Shop Shop { get; set; }
        public string MemberType { get; private set; }
        public int MemberTime { get; private set; }
        public string Clan { get; private set; }
        public GameClient(GameConnection connection)
        {
            if (connection.GameVersion != null)
                _url = "http://pokemon-planet.com/" + connection.GameVersion;
            _connection = connection;
            _gameConnection = connection;
            _connection.PacketReceived += Connection_PacketReceived;
            connection.LoggedIn += HttpConnection_LoggedIn;
            connection.LoggingError += HttpConnection_LoggingError;

            _connection.LogMessage += Connection_LogMessage;
            _connection.Disconnected += Connection_Disconnected;
            _connection.Connected += Connection_Connected;
            _connection.JoinedRoom += Connection_JoinedRoom;
            _connection.SuccessfullyAuthenticated += Connection_SuccessfullyAuthenticated;
            Items = new List<InventoryItem>();
            MiningObjects = new List<MiningObject>();
            EliteChests = new List<EliteChest>();
            Team = new List<Pokemon>();
            WildPokemons = new List<Pokemon>();
            TempMap = "";
            LoggedInToWebsite = false;
            PokemonCaught = new string[900];
        }
        public int PlayerX { get; private set; }
        public int PlayerY { get; private set; }
        public List<InventoryItem> Items { get; private set; }
        public List<Pokemon> Team { get; set; }
        public IList<Pokemon> WildPokemons { get; }
        private int LastUpdateX { get; set; }
        private int LastUpdateY { get; set; }
        public string EncryptedTileX { get; private set; }
        public string EncryptedTileY { get; private set; }

        public string MapName { get; private set; }
        private string TempMap { get; set; }
        public string EncryptedMap { get; private set; }
        public bool IsInBattle => Battle;
        public string Username { get; private set; }
        public string HashPassword { get; private set; }
        public string Id { get; private set; }
        public bool LoggedInToWebsite { get; private set; }
        public bool IsConnected => _connection.IsConnected && _connection != null;

        public bool IsInactive =>
            _movements.Count == 0 
            && !_loadingTimeout.IsActive
            && !_movementTimeout.IsActive
            && !_battleTimeout.IsActive
            && !_swapTimeout.IsActive
            && !_fishingTimeout.IsActive
            && !_itemTimeout.IsActive
            && !_miningTimeout.IsActive;

        public bool IsMapLoaded => _isMapLoaded();

        public event Action Connected;
        public event Action<Exception> Disconnected;
        public event Action<string> LogMessage;
        public event Action<string> ChatMessage;
        public event Action<string, int, int> TeleportationOccuring;
        public event Action PlayerDataUpdated;
        public event Action InventoryUpdated;
        public event Action MapUpdated;
        public event Action<bool> TeamUpdated;
        public event Action PlayerPositionUpdated;
        public event Action SuccessfullyAuthenticated;
        public event Action AuthenticationFailed;
        public event Action BattleStarted;
        public event Action BattleEnded;
        public event Action<string> BattleMessage;
        public event Action<IList<Pokemon>, int> EnemyUpdated;
        public event Action Evolving;
        public event Action<string, int> LearningMove;
        public event Action<Exception> LoggingError;
        public event Action LoggedIn;
        public event Action<string[]> PrivateChat;
        public event Action<string> SystemMessage;
        public event Action<Shop> ShopOpened;
        public event Action<MiningObject> RockRestored;
        public event Action<MiningObject> RockDepleted;



        public void Logout()
        {
            Close();
            _connection.Logout();
        }

        private void CheckForLoggingIn()
        {
            if (!IsMapLoaded)
            {
                Logout();
                AuthenticationFailed?.Invoke();
            }
        }
        private void Connection_SuccessfullyAuthenticated()
        {
            SuccessfullyAuthenticated?.Invoke();
            _checkForLoggingTimeout = ExecutionPlan.Delay(20000, () => CheckForLoggingIn());
        }

        private void HttpConnection_LoggingError(Exception obj)
        {
            LoggingError?.Invoke(obj);
        }

        public int GetTimer()
        {
            var time = DateTime.Now - Timer;
            return (int)time.TotalMilliseconds;
        }

        private void Connection_JoinedRoom()
        {
            LogMessage?.Invoke("Loading Game Data...");
            _checkForLoggingTimeout?.Abort();
            _checkForLoggingTimeout = null;
            _checkForLoggingTimeout = ExecutionPlan.Delay(180000, () => CheckForLoggingIn());
            GetTimeStamp("getStartingInfo");
        }

        private void Connection_Connected()
        {
            Connected?.Invoke();
        }

        private void Connection_Disconnected(Exception obj)
        {
            Close();
            Disconnected?.Invoke(obj);
            LoggedInToWebsite = false;
        }

        private void Connection_LogMessage(string obj)
        {
            LogMessage?.Invoke(obj);
        }

        private void HttpConnection_LoggedIn()
        {
            Username = _gameConnection.Username; //Username is needed coz of some stupid encryption string.
            Id = _gameConnection.Id;
            HashPassword = _gameConnection.HashPassword;
            LoggedInToWebsite = true;
            EncryptedPacketCount = Connection.CalcMd5(EncryptedPacketCount + kg1 + Username);
            EncryptedstepsWalked = Connection.CalcMd5(_stepsWalked + kg1 + Username);
        }

        private void UpdatePosition()
        {
            if (!(LastUpdateX == PlayerY && LastUpdateY == PlayerY))
            {
                LastUpdateX = PlayerX;
                LastUpdateY = PlayerY;
                GetTimeStamp("updateXY");
            }
        }

        private async void Connection_PacketReceived(string obj)
        {
            try
            {
                if (obj.Length > 1)
                {
                    if (obj.StartsWith("`"))
                    {
                        obj = obj.Substring(1);
                        var data = obj.Split('`');
                        var type = data[0];
                        var action = data[1];
#if DEBUG
                        Console.WriteLine("Got Data From server:");
                        Console.WriteLine($"Main: {obj} Type: {type} Action: {action}");
#endif
                        switch (type)
                        {
                            case "xt":
                                switch (action)
                                {
                                    case "pmsg":
                                        ProcessChatMessage(obj);
                                        break;
                                    case "r17":
                                        ProcessChatMessage(obj);
                                        break;
                                    case "r36":
                                        PrivateChat?.Invoke(data);
                                        break;
                                    case "r10":
                                        _inited = true;
                                        await HandleGetStartingInfo(data);
                                        break;
                                    case "r44":
                                        Money = Convert.ToInt32(data[3]);
                                        var ii = Items.Find(i => i.Name == data[4]);
                                        ii.Quntity += 1;
                                        Items = Items.OrderBy(i => i.Uid).ToList();
                                        InventoryUpdated?.Invoke();
                                        break;
                                    case "b85":
                                        var var1 = data[3].Split(',');
                                        var var2 = data[4].Split(',');
                                        var iss = 0;
                                        while (iss < var1.Length)
                                        {
                                            var i1 = Items.Find(i => i.Name == var1[iss]);
                                            i1.Quntity -= 1;
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
                                                i1.Quntity += 1;
                                            }
                                            iss++;
                                        }

                                        Items = Items.OrderBy(i => i.Uid).ToList();
                                        InventoryUpdated?.Invoke();
                                        break;
                                    case "b86":
                                        var itmData = data[3].Split(',');
                                        var inventoryItem = Items.Find(i => i.Name == itmData[0]);
                                        inventoryItem.Quntity = inventoryItem.Quntity + Convert.ToInt32(itmData[1]);
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
                                            loc8 = loc8 + 1;
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
                                        LetOtherToSee(data);
                                        break;
                                    case "b2":
                                        var to = Team[Convert.ToInt32(data[3])];
                                        var from = Team[Convert.ToInt32(data[4])];
                                        Team[Convert.ToInt32(data[4])] = to;
                                        Team[Convert.ToInt32(data[3])] = from;
                                        if (_swapTimeout.IsActive)
                                        {
                                            _swapTimeout.Set(Rand.Next(500, 1000));
                                        }
                                        TeamUpdated?.Invoke(false);
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
                                            Items[Items.IndexOf(Items.Find(i => i.Name == itm.Name))].Quntity += 1;
                                        }
                                        Team[pokeIndex].ItemHeld = "";
                                        if (_swapTimeout.IsActive)
                                        {
                                            _swapTimeout.Set(Rand.Next(500, 1000));
                                        }
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
                                        HandleWildBattle(data);
                                        break;
                                    case "w2":
                                        HandleWildBattle(data, true);
                                        break;
                                    case "c":
                                        HandleWildBattleUpdate(data);
                                        break;
                                    case "bl":
                                        TempMap = data[3];
                                        EncryptedMap = data[4];
                                        PlayerX = Convert.ToInt32(data[5]);
                                        PlayerY = Convert.ToInt32(data[6]);
                                        break;
                                    case "b87":
                                        Items.Find(i => i.Name == data[3].Split(',')[0]).Quntity -=
                                            Convert.ToInt32(data[3].Split(',')[1]);
                                        if (Items.Find(i => i.Name == data[3].Split(',')[0]).Quntity <= 0)
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
                                        Battle = true;
                                        CanMove = false;
                                        IsFishing = false;
                                        _battleTimeout.Set(Rand.Next(4000, 6000));
                                        GetTimeStamp("goodHook", "1");
                                        StopFishing();
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
                                        LogMessage?.Invoke("You've been permanently banned from the game.");
                                        Logout();
                                        _checkForLoggingTimeout?.Abort();
                                        _checkForLoggingTimeout = null;
                                        break;
                                    case "b177":
                                        LogMessage?.Invoke("Client out of date.");
                                        Logout();
                                        _checkForLoggingTimeout?.Abort();
                                        _checkForLoggingTimeout = null;
                                        break;
                                    case "b179":
                                        ExecutionPlan.Delay(Rand.Next(2000, 5000),
                                                    () => GetTimeStamp("declineBattle"));
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
                                }

                                break;
                        }
                    }
                    else if (obj.StartsWith("<"))
                    {
                        //XML packets....
                        //if (!obj.EndsWith("\0")) return;
                        obj = obj.Replace("\0", "");
#if DEBUG
                        Console.WriteLine("Got Data From server:");
                        Console.WriteLine($"Main: {obj}");
#endif
                        ParseXml(obj);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
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
            var key = Items.Find(x => x.Name.ToLowerInvariant() == "treasure key");
            if (key != null)
            {
                if (key.Quntity > 1)
                {
                    var index = Items.IndexOf(key);
                    Items[index].Quntity -= 1;
                }
                else
                {
                    Items.Remove(key);
                }
            }
            var newItm = new InventoryItem(items[0]);
            newItm.Uid = Items.LastOrDefault().Uid + 1;
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
                    if (tokenItem.Quntity > 200)
                    {
                        var index = Items.IndexOf(tokenItem);
                        Items[index].Quntity = Items[index].Quntity - 200;
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
            StopMining();
        }

        private void HandleMiningRockDepleted(string[] resObj)
        {
            var x = Convert.ToInt32(resObj[3]);
            var y = Convert.ToInt32(resObj[4]);
            if (_lastRock != null)
                if (x == _lastRock.X && y == _lastRock.Y)
                    _lastRock.IsMined = true;
            if (MiningObjects.Count > 0)
            {
                MiningObjects.Find(r => r.X == x && r.Y == y).IsMined = true;
                RockDepleted?.Invoke(MiningObjects.Find(r => r.X == x && r.Y == y));
                _miningTimeout.Set();
            }
        }
        private void HandleMiningRockRestored(string[] resObj)
        {
            var x = Convert.ToInt32(resObj[3]);
            var y = Convert.ToInt32(resObj[4]);
            if (_lastRock != null)
                if (x == _lastRock.X && y == _lastRock.Y)
                    _lastRock.IsMined = false;
            if (MiningObjects.Count > 0)
            {
                MiningObjects.Find(r => r.X == x && r.Y == y).IsMined = false;
                MiningObjects.Find(r => r.X == x && r.Y == y).IsGoldMember = resObj[5] == "1";
                RockRestored?.Invoke(MiningObjects.Find(r => r.X == x && r.Y == y));
                _miningTimeout.Set();
            }
        }

        private void ProcessChatMessage(string packet)
        {
            //-----------------Stupid way to check chat messages xD------------------------//

            HasEncounteredRarePokemon = (packet.ToLowerInvariant().Contains(RareLengendaryPattern.ToLowerInvariant()) &&
                packet.ToLowerInvariant().Contains(Username.ToLowerInvariant())) || (packet.ToLowerInvariant().Contains("you have encountered a"));

            if (packet.ToLowerInvariant().Contains("error with fishing rod location in inventory") ||
                packet.ToLowerInvariant().Contains("can't fish again yet"))
            {
                IsFishing = false;
                StopFishing();
            }

            if (packet.ToLowerInvariant().Contains("you need at least") &&
                packet.ToLowerInvariant().Contains("to fish in these waters."))
            {
                IsFishing = false;
                StopFishing();
            }

            if (packet.ToLowerInvariant().Contains("you are not in a battle."))
            {
                Battle = false;
                _battleTimeout.Set(Rand.Next(1000, 1500));
            }

            if (packet.ToLowerInvariant().Contains("rock has already been mined") || packet.ToLowerInvariant().Contains("you mined a") ||
                packet.ToLowerInvariant().Contains("mine again yet") || RemoveUnknownSymbolsFromString(packet)
                    .ToLowerInvariant().Contains("you can't mine") || packet.ToLowerInvariant().Contains("you need a higher mining level to mine that"))
            {
                _miningTimeout.Set(Rand.Next(2500, 3500));

                if (MiningObjects.Count > 0 && _lastRock != null)
                {
                    _lastRock.IsMined = true;

                    MiningObjects.Find(r => r.X == _lastRock.X && r.Y == _lastRock.Y)
                        .IsMined = true;
                    RockDepleted?.Invoke(MiningObjects.Find(r => r.X == _lastRock.X && r.Y == _lastRock.Y));
                }
                StopMining();
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
            ChatMessage?.Invoke(packet);
        }
        public void WaitWhileInBattle()
        {
            _battleTimeout.Set(Rand.Next(2000, 2500));
        }
        //I just hate Xml idk why....
        private void ParseXml(string packet)
        {
            try
            {
                var xml = new XmlDocument();
                packet = ObjectSerilizer.DecodeEntities(packet);
                xml.LoadXml(packet);
                var node = xml.DocumentElement?.GetElementsByTagName("dataObj")[0];
                if (node != null)
                    foreach (XmlElement textNode in node)
                        if (textNode.GetAttribute("n") != "" && textNode.InnerText != "")
                        {
                            var type = textNode.GetAttribute("n");
                            switch (type)
                            {
                                case "_cmd":
                                    switch (textNode.InnerText)
                                    {
                                        case "learnMove":
                                            OnLearningMove(xml);
                                            break;
                                        case "choosePokemon":
                                            UpdateTeamThroughXml(xml);
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
                                            OnAcceptEvolve(xml);
                                            break;
                                        case "declineEvolve":
                                            CanMove = true;
                                            break;
                                        case "updateInventory":
                                            OnInventoryUpdateThroughXml(packet);
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
                                            UpdateTeamThroughXml(xml);
                                            break;
                                        case "stepsWalked":
                                            UpdateTeamThroughXml(xml);
                                            break;
                                        case "buyItem":
                                            OnInventoryUpdateThroughXml(packet);
                                            BoughtItem(xml);
                                            break;
                                        case "useItem2":
                                            OnInventoryUpdateThroughXml(packet);
                                            UpdateTeamThroughXml(xml);
                                            UsedItemMsg(xml);
                                            break;
                                    }
                                    break;
                            }
                        }
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

        private void PrintSystemMessage(string msg)
        {
            SystemMessage?.Invoke(msg);
        }
        public void LearnMove(int moveUid)
        {
            if (moveUid < 0)
                return;
            _swapTimeout.Set();
            GetTimeStamp("forgetMove", moveUid.ToString());
        }

        public void OpenShop()
        {
            if (Shop is null) return;
            OpenedShop = Shop;
            ShopOpened?.Invoke(OpenedShop);
        }//I just hate Xml idk why....
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
        //I just hate Xml idk why....

        private void UpdateTeamThroughXml(XmlDocument xml)
        {
            Team.Clear();
            var xmlDocument = XDocument.Parse(xml.InnerXml);
            var pokeElement = xmlDocument.Descendants("obj").ToList().Find(o => o.Attribute("o")?.Value != null && o.Attribute("o")?.Value == "userPokemon");
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
                else if (element.GetAttribute("o").ToLowerInvariant() == "moves")
                    ParsePokemonFromXml(element.ChildNodes, uid >= 0 ? uid : -1);
#if DEBUG
                if (uid >= 0)
                    Console.WriteLine(uid);
#endif
            }

            if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
            TeamUpdated?.Invoke(true);
        }

        private void OnInventoryUpdateThroughXml(string packet)
        {
            try
            {
                Items.Clear();
                packet = ObjectSerilizer.DecodeEntities(packet);
                var xmlDocument = XDocument.Parse(packet);
                var result = xmlDocument.Descendants("obj").ToList().Find(o => o.Attribute("o")?.Value != null && o.Attribute("o")?.Value == "inventory");
                var inveXDocument = XDocument.Parse(result.ToString());
                var invResults = inveXDocument.Descendants("obj").ToList()
                    .FindAll(el => el.Element("var")?.Value != null);
                foreach (var sXElement in invResults)
                {
                    if (sXElement.Element("var")?.Value != null)
                    {
                        var item = new InventoryItem(sXElement);
                        Items.Add(item);
                    }
                }
                Items = Items.OrderBy(itm => itm.Uid).ToList();
                _itemTimeout.Set(Rand.Next(2000, 2500));
                InventoryUpdated?.Invoke();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public void Close()
        {
            _updatePositionTimeout?.Abort();
            _pingToServer?.Abort();
            _saveData?.Abort();
            _checkForLoggingTimeout?.Abort();
        }

        private void ParsePokemonFromXml(XmlNodeList nodes, int uid = -1)
        {
            var pokemon = new Pokemon(nodes);
            if (!pokemon.Name.Contains("???") || pokemon.Nature != null)
                Team.Add(pokemon);
            if (uid != -1 && Team.Contains(pokemon))
                pokemon.Uid = uid + 1;
            else if (Team.Contains(pokemon))
                pokemon.Uid = Team.IndexOf(pokemon) + 1;
        }

        private void HandleWildBattleUpdate(string[] resObj)
        {

            try
            {
                Shop = null;
                LastBattle = null;
                OpenedShop = null;
                ActiveBattle.UpdateBattle(resObj);
                if (resObj[11]?.IndexOf("[") == 0 && resObj[11]?.IndexOf("]") != -1)
                {
                    ParseMultiPokemon(resObj[11]);
                }
                else
                {
                    Console.WriteLine(resObj[11]);
                }

                if (ActiveBattle != null)
                {
                    if (WildPokemons.Count > 0)
                    {
                        var enemy = ActiveBattle.FullWildPokemon;
                        if (!WildPokemons.Any(p =>
                            p.Name == enemy.Name && p.Ability == enemy.Ability &&
                            p.Stats == enemy.Stats && p.Level == enemy.Level && p.IV == enemy.IV))
                        {
                            WildPokemons.Add(enemy);
                        }
                    }
                    else
                    {
                        WildPokemons.Add(ActiveBattle.FullWildPokemon);
                    }
                    EnemyUpdated?.Invoke(WildPokemons, ActiveBattle.ActivePokemon);
                }

                if (resObj[3] == "W")
                {
                    Battle = false;
                    if (resObj[9] != "0") Money = Convert.ToInt32(resObj[9]);
                    EndBattle();
                }
                else if (resObj[6] == "1")
                {
                    Battle = false;
                    EndBattle();
                }

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
            var battleText = "";
            _battleTimeout.Set(Rand.Next(2000, 4000));
            while (loc2 < battleTexts.Length)
            {
                var battleOtherText = battleTexts[loc2].Split(',');

                foreach (var text in battleOtherText)
                {
                    var st = text;
                    if (text.IndexOf("\0", StringComparison.Ordinal) != -1)
                    {
                        st = text.Replace("\0\0\0", "'");
                        st = st.Replace("\0", "'");
                    }

                    if (text.IndexOf("?", StringComparison.Ordinal) != -1)
                    {
                        st = text.Replace("???", "'");
                        st = st.Replace("?", "'");
                    }

                    if (st.IndexOf(".", StringComparison.Ordinal) != -1)
                        battleText += st;
                    else if (st.IndexOf("!", StringComparison.Ordinal) != -1)
                        battleText += st.Replace("&#44", "");
                    if ((st.ToLowerInvariant().Contains("out of usable pokemon") ||
                         st.ToLowerInvariant().Contains("opponent won the battle")) && TempMap != "")
                    {
                        EndBattle(true);
                        LoadMap(false, TempMap, 19, 18, false, MapName);
                    }
                }

                BattleMessage?.Invoke(battleText);
                battleText = "";
                loc2 = loc2 + 1;
            }
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

        public void EndBattle(bool lostBattle = false)
        {
            LastBattle = ActiveBattle;
            ActiveBattle = null;
            _battleTimeout.Set(Rand.Next(2500, 4000));
            IsTrapped = false;

            GetTimeStamp("updateFollowPokemon");

            if (!lostBattle)
                GetTimeStamp("r");
            else
                _needToLoadR = true;
            Battle = false;
            HasEncounteredRarePokemon = false;
            movingForBattle = false;
            CanMove = true;
            BattleEnded?.Invoke();
        }
        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
        private void HandleWildBattle(string[] data, bool disconnect = false)
        {
            _movements.Clear();
            movingForBattle = false;
            Shop = null;
            LastBattle = null;
            OpenedShop = null;
            Battle = true;
            ActiveBattle = new Battle(data, true, disconnect, this);
            _battleTimeout.Set(Rand.Next(4000, 6000));

            CanMove = false;
            if (ActiveBattle.IsWildBattle)
            {
                BattleMessage?.Invoke("A wild " + (ActiveBattle.WildPokemon.IsShiny ? "Shiny " : "") + FirstCharToUpper(ActiveBattle.WildPokemon.Name) +
                                      " has appeared!");
            }

            if (disconnect)
            {
                BattleStarted?.Invoke();
                Battle = true;
            }
            if (data[4] == "")
            {
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
                    Battle = true;
                    CanMove = false;
                }
            }
            BattleStarted?.Invoke();
        }

        public static int DistanceBetween(int fromX, int fromY, int toX, int toY)
        {
            return Math.Abs(fromX - toX) + Math.Abs(fromY - toY);
        }

        private async void ParseAllMiningRocks(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.IndexOf("[", StringComparison.Ordinal) != -1 && loc2.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    await Task.Run(() =>
                    {
                        while (loc1 < strArrayA.Length)
                        {
                            var data = "[" + strArrayA[loc1] + "]";
                            MiningObjects.Add(ParseRock(data));
                            loc1 = loc1 + 1;
                        }
                    });
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
                if (data[4] != "")
                {
                    Shop = new Shop(data[4]);
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

                if (_avatarType > 0)
                    GetTimeStamp("sendAddPlayer");

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

        private async void ParseAllEliteChests(string loc2)
        {
            if (loc2 != "[]" && loc2 != "")
            {
                if (loc2.IndexOf("[", StringComparison.Ordinal) != -1 && loc2.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    loc2 = loc2.Substring(2, loc2.Length - 4);
                    var strArrayA = loc2.Split(new[] { "],[" }, StringSplitOptions.None);
                    var loc1 = 0;
                    await Task.Run(() =>
                    {
                        while (loc1 < strArrayA.Length)
                        {
                            var data = "[" + strArrayA[loc1] + "]";
                            EliteChests.Add(ParseChest(data));
                            loc1 = loc1 + 1;
                        }
                    });
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

        private string[] ParseArray(string tempStr2)
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
                _itemTimeout.Set();
                SendShopPokemart(itemId, quantity);
                return true;
            }

            return false;
        }

        private void SendShopPokemart(int itemId, int quantity)
        {
            GetTimeStamp("buyItem", itemId.ToString(), quantity.ToString());
        }
        //Stupid SWF handles player things like below lol :D
        private async Task HandleGetStartingInfo(string[] resObj)
        {
            Money = Convert.ToInt32(resObj[3]);
            Credits = Convert.ToInt32(resObj[4]);
            var loopNum = 0;
            //`Map,1`Pokedex,1`Potion,7`Escape Rope,2`Backpack,1`Great Ball (untradeable),10`Ultra Ball (untradeable),5`Christmas Present,3`Revive,1`Great Ball,1`)()(09a0jd
            await Task.Run(() =>
            {
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
            });
            Items = Items.OrderBy(o => o.Uid).ToList();
            _itemTimeout.Set(Rand.Next(1000, 1500));
            InventoryUpdated?.Invoke();
            PlayerX = Convert.ToInt32(resObj[loopNum + 2]);
            PlayerY = Convert.ToInt32(resObj[loopNum + 3]);
            EncryptedTileX = Connection.CalcMd5(PlayerX + kg1 + Username);
            EncryptedTileY = Connection.CalcMd5(PlayerY + kg1 + Username);
            LastUpdateX = PlayerX;
            LastUpdateY = PlayerY;
            MapName = resObj[loopNum + 4];
            MapName = MapName.Replace(" (", "").Replace(")", "");
            EncryptedMap =
                Connection.CalcMd5(MapName + "dlod02jhznpd02jdhggyambya8201201nfbmj209ahao8rh2pb" + Username);
            PlayerDataUpdated?.Invoke();
            await Task.Run(() =>
            {
                for (var _loc7 = loopNum + 5; _loc7 < 99999; ++_loc7)
                {
                    if (resObj[_loc7] != ")()(09a0jc")
                    {
                        continue;
                    } 
                    loopNum = _loc7;
                    break;
                }
            });
            _avatarType = Convert.ToInt32(resObj[loopNum + 3]);
            if (_avatarType == 0)
            {
                _characterCreation = new CharacterCreation(Rand);
                ExecutionPlan.Delay(1000, () => GetTimeStamp("createCharacter"));
            }
            MemberType = resObj[loopNum + 4];
            int.TryParse(resObj[loopNum + 5], out var time);
            MemberTime = time;
            Clan = resObj[loopNum + 6] == "0" ? "" : resObj[loopNum + 6];
            //User Pokemons
            //[7216762,150,46,46,52,41,38,72,0,0,66,39,5,10,29,1,6,15,0,23,hardy,82,82,52,108,0,10,72,58332,2142,false,26,5,Charmeleon,none,66,,0,Professor Oak,default]`[9388091,148,38,48,52,48,58,78,0,0,44,12,7,41,24,5,9,29,24,18,lax,95,33,109,36,0,1,78,54672,1163,false,25,234,Stantler,none,119,,0,Nuhash2004,default]`
            Team.Clear();
            await Task.Run(() =>
            {
                for (var loc7 = loopNum + 19; loc7 < 99999; ++ loc7)
                {
                    if (resObj[loc7] != ")()(09a0jb")
                    {
                        ParsePokemon(resObj[loc7]);
                        continue;
                    }

                    loopNum = loc7;
                    break;
                }
            });
            if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
            TeamUpdated?.Invoke(true);
            if (Team is null is false && Team.Count > 0)
                GetTimeStamp("updateFollowPokemon");
            
            await Task.Run(() =>
            {
                for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
                {
                    if (resObj[loc7] != ")()(09a0ja")
                    {
                        continue;
                    }
                    loopNum = loc7;
                    break;
                }
            });
            await Task.Run(() =>
            {
                for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
                {
                    if (resObj[loc7] != ")()(09a0jz")
                    {
                        continue;
                    }
                    loopNum = loc7;
                    break;
                }
            });

            _pingToServer = ExecutionPlan.Repeat(30000, () => PingServer());
            _saveData = ExecutionPlan.Repeat(1800000, () => GetTimeStamp("saveData"));
            if (_avatarType > 0)
                LoadMap(false, MapName);

            await Task.Run(() =>
            {
                for (var loc7 = loopNum + 1; loc7 < 99999; ++loc7)
                {
                    if (resObj[loc7] != ")()(09a0js")
                    {
                        continue;
                    }
                    loopNum = loc7;
                    break;
                }
            });

            _movementSpeedMod = 1;

            if (Convert.ToInt32(resObj[loopNum + 10]) > 0)
            {
                if (Convert.ToInt32(resObj[loopNum + 9]) == 2)
                {
                    _movementSpeedMod = 2;
                }
                else if (Convert.ToInt32(resObj[loopNum + 9]) == 0.500000)
                    _movementSpeedMod = 0.500000;
            }

            _mapMovementSpeed = 8 * _movementSpeedMod;


            Fishing = new FishingExtentions(resObj[loopNum + 11], resObj[loopNum + 12], resObj[loopNum + 13]);
            PokemonCaught = ParseStringArray(resObj[loopNum + 36]);
            Mining = new MiningExtentions(resObj[loopNum + 48], resObj[loopNum + 49], resObj[loopNum + 50]);

            LoggedIn?.Invoke();
            _checkForLoggingTimeout?.Abort();
            _checkForLoggingTimeout = null;
        }

        private void ParsePokemon(string str)
        {
            if (str != "[]" && str != "")
                if (str.IndexOf("[", StringComparison.Ordinal) != -1 && str.IndexOf("]", StringComparison.Ordinal) != -1)
                {
                    str = str.Substring(1, str.Length - 2);
                    var data = str.Split(',');
                    if (data.Length == 40)
                    {
                        var pokemon = new Pokemon(data);
                        Team.Add(pokemon);
                        Team[Team.IndexOf(pokemon)].Uid = Team.IndexOf(pokemon) + 1;
                    }
                }
        }

        private void ParseMultiPokemon(string str)
        {
            if (str != "[]" && str != "")
            {
                // ReSharper disable once StringIndexOfIsCultureSpecific.1
                // ReSharper disable once ConstantConditionalAccessQualifier
                if (str.IndexOf("[") == 0 && str?.IndexOf("]") != -1)
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
                        loc1 = loc1 + 1;
                    }

                    if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));
                    TeamUpdated?.Invoke(true);
#if DEBUG
                    return;
#endif
                }
#if DEBUG
                Console.WriteLine("ParseMultiPokemon bracket error");
#endif
            }
        }

        public bool LoadMap(bool switchingMap, string tempMap, int x = int.MinValue, int y = int.MinValue,
            bool customMap = false, string oldMapBattleLost = "")
        {
            movingForBattle = false;
            _movementTimeout.Cancel();
            _movements.Clear();

            MiningObjects.Clear();
            Shop = null;
            OpenedShop = null;
            if (x == int.MinValue)
                x = PlayerX;
            if (y == int.MinValue)
                y = PlayerY;

            if (IsFishing)
                StopFishing();
            if (IsMinning)
                StopMining();

            _loadingTimeout.Set(Rand.Next(2000, 3000));
            if (_updatePositionTimeout != null)
                _updatePositionTimeout.Abort();

            GetTimeStamp("removePlayer", oldMapBattleLost);
            _updatedMap = false;
            MapName = tempMap;
            TempMap = "";
            EncryptedMap =
                Connection.CalcMd5(MapName + "dlod02jhznpd02jdhggyambya8201201nfbmj209ahao8rh2pb" + Username);
            PlayerX = x;
            PlayerY = y;
            EncryptedTileX = Connection.CalcMd5(PlayerX + kg1 + Username);
            EncryptedTileY = Connection.CalcMd5(PlayerY + kg1 + Username);
            _updatePositionTimeout = ExecutionPlan.Repeat(10000, UpdatePosition);
            if (_needToLoadR)
            {
                GetTimeStamp("r");
                _needToLoadR = false;
            }

            return true;
        }

        private void OnInventoryUpdate(string data)
        {
            if (data.Contains(")()(09a0jd")) return;
            var n = data.Split(',');
            if (n.Length > 2)
            {
                var item = new InventoryItem(n[0], Convert.ToInt32(data[1]), n[2]);
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
        }

        private void LetOtherToSee(string[] data)
        {
            double offsetAmount = 0;
            if (_movements.Count > 0)
                offsetAmount = 64 - _mapMovements;
            if (_avatarType != 0)
                GetTimeStamp("sendAddPlayerTarget", data[3], offsetAmount.ToString());
        }

        public void SendPacket(string packet)
        {
            _connection.Send(packet);
        }

        public void SendPacket(string header, string action, object fromRoom, string message)
        {
            _connection.Send(header, action, fromRoom, message);
        }

        public void PingServer()
        {
            _connection.SendXtMessage("PokemonPlanetExt", "p", null, "str");
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
            if (type == "getStartingInfo" && _inited)
                return;
            if (type != "sendMovement" && type != "updateXY" && type != "sendFace2Goto" && type != "sendCapeGoto" &&
                type != "sendHatGoto" && type != "sendWingGoto" && type != "sendTailGoto" && type != "asf8n2fs" &&
                type != "asf8n2fa" && type != "sendMineAnimation" && type != "sendStopMineAnimation" &&
                type != "sendFishAnimation" && type != "r" && type != "saveData" && type != "loadCustomMaps")
            {
            }

            if (type == "forgetMove")
            {
                var loc8 = new ArrayList();
                loc8.Add($"moveNum:{p1}");
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add("pke:" +
                         Connection.CalcMd5(packetKey + kg2 +
                                            GetTimer()));
                loc8.Add("pk:" + packetKey);
                loc8.Add("te:" + GetTimer());
                _connection.SendXtMessage("PokemonPlanetExt", "b0", loc8, "xml");
            }
            if (type == "command")
            {
                var loc8 = new ArrayList();
                loc8.Add($"command:{p1}");
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add("pke:" +
                         Connection.CalcMd5(packetKey + kg2 +
                                            GetTimer()));
                loc8.Add("pk:" + packetKey);
                loc8.Add("te:" + GetTimer());
                _connection.SendXtMessage("PokemonPlanetExt", "b4", loc8, "xml");
            }
            else if (type == "updateMap")
            {
                var loc8 = new ArrayList();
                LastUpdateX = PlayerX;
                LastUpdateY = PlayerY;
                loc8.Add("y:" + PlayerY);
                loc8.Add("x:" + PlayerX);
                loc8.Add("map:" + p1);
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add("pke:" +
                         Connection.CalcMd5(packetKey + kg2 +
                                            GetTimer()));
                loc8.Add("pk:" + packetKey);
                loc8.Add("te:" + GetTimer());
                _connection.SendXtMessage("PokemonPlanetExt", "b5", loc8, "xml");
            }
            else if (type == "battleMove")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                if (p2 != "")
                    loc8.Add(p2);
                else
                    loc8.Add("z");
                if (p3 != "")
                    loc8.Add(p3);
                else
                    loc8.Add("z");
                _connection.SendXtMessage("PokemonPlanetExt", "b76", loc8, "str");
            }
            else if (type == "getStartingInfo")
            {
                Console.WriteLine("@@getstartinginfo");
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(HashPassword);
                loc8.Add(Id);
                //loc8.Add(Connection.CalcMd5(
                //    _url + loc8[1] + kg2 + loc8[0] + Username));
                //loc8.Add("1");
                _connection.SendXtMessage("PokemonPlanetExt", "b61", loc8, "str");
            }
            else if (type == "pmsg")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b66", loc8, "str");
            }
            else if (type == "clanMessage")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b67", loc8, "str");
            }
            else if (type == "pm")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                loc8.Add(p2);
                _connection.SendXtMessage("PokemonPlanetExt", "r36", loc8, "str");
            }
            else if (type == "saveData")
            {
                _connection.SendXtMessage("PokemonPlanetExt", "b26", null, "xml");
            }
            else if (type == "updateXY")
            {
                var loc8 = new ArrayList();
                loc8.Add(PlayerX.ToString());
                loc8.Add(PlayerY.ToString());
                loc8.Add(EncryptedTileX);
                loc8.Add(EncryptedTileY);
                loc8.Add(GetTimer().ToString());
                _connection.SendXtMessage("PokemonPlanetExt", "r8", loc8, "str");
            }
            else if (type == "removePlayer")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(Username);
                if (p1 != "") loc8.Add(p1.ToLowerInvariant());
                _connection.SendXtMessage("PokemonPlanetExt", "b74", loc8, "str");
            }
            else if (type == "sendAddPlayer")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(PlayerX);
                loc8.Add(PlayerY);
                loc8.Add(_dir);
                loc8.Add(_moveType);
                loc8.Add(_mapInstance != -1 ? MapName + $"({_mapInstance})" : MapName);
                loc8.Add(IsFishing ? "1" : "0");
                _connection.SendXtMessage("PokemonPlanetExt", "b55", loc8, "str");
            }
            else if (type == "sendAddPlayerTarget")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(PlayerX);
                loc8.Add(PlayerY);
                loc8.Add(_dir);
                loc8.Add(_moveType);
                loc8.Add(p1.ToLowerInvariant());
                loc8.Add(p2);
                loc8.Add(IsFishing ? "1" : "0");
                _connection.SendXtMessage("PokemonPlanetExt", "b56", loc8, "str");
            }
            else if (type == "r")
            {
                _connection.SendXtMessage("PokemonPlanetExt", "r", null, "str");
            }
            else if (type == "sendMovement")
            {
                var loc8 = new ArrayList();
                if (p1 == "")
                    loc8.Add(_dir[0]);
                else
                    loc8.Add(p1[0]);
                if (_moveType != "")
                {
                    if (_moveType == "bike")
                    {
                        loc8.Add("b");
                    }
                    else if (_moveType == "surf")
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
                }

                _connection.SendXtMessage("PokemonPlanetExt", "m", loc8, "str");
            }
            else if (type == "reorderPokemon")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                loc8.Add(p2);
                _connection.SendXtMessage("PokemonPlanetExt", "b2", loc8, "str");
            }
            else if (type == "choosePokemon")
            {
                var loc8 = new ArrayList();
                loc8.Add($"pokemon:{p1}");
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b7", loc8, "xml");
            }
            else if (type == "buyItem")
            {
                var loc8 = new ArrayList();
                loc8.Add($"amount:{p2}");
                loc8.Add($"buyNum:{p1}");
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b8", loc8, "xml");
            }
            else if (type == "stepsWalked")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b38", loc8, "xml");
            }
            else if (type == "escapeBattle")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                _connection.SendXtMessage("PokemonPlanetExt", "b77", loc8, "str");
            }
            else if (type == "wildBattle")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(MapName);
                if (p1 == "")
                    loc8.Add(_moveType);
                else
                    loc8.Add(p1);
                loc8.Add(EncryptedMap);
                _connection.SendXtMessage("PokemonPlanetExt", "b78", loc8, "str");
            }
            else if (type == "switchPokemon")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b80", loc8, "str");
            }
            else if (type == "endBattleDisconnect")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                _connection.SendXtMessage("PokemonPlanetExt", "b81", loc8, "str");
            }
            else if (type == "endBattleDisconnect2")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                _connection.SendXtMessage("PokemonPlanetExt", "b82", loc8, "str");
            }
            else if (type == "fish")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b70", loc8, "str");
            }
            else if (type == "sendFishAnimation")
            {
                var loc8 = new ArrayList();
                switch (_dir)
                {
                    case "up":
                        loc8.Add("u");
                        break;
                    case "down":
                        loc8.Add("d");
                        break;
                    case "right":
                        loc8.Add("r");
                        break;
                    case "left":
                        loc8.Add("l");
                        break;
                    default:
                        loc8.Add("d");
                        break;
                }
                _connection.SendXtMessage("PokemonPlanetExt", "f", loc8, "str");
            }
            else if (type == "mine")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                loc8.Add(p2);
                loc8.Add(p3);
                _connection.SendXtMessage("PokemonPlanetExt", "b163", loc8, "str");
            }
            else if (type == "sendMineAnimation")
            {
                var loc8 = new ArrayList();
                switch (_dir)
                {
                    case "up":
                        loc8.Add("u");
                        break;
                    case "down":
                        loc8.Add("d");
                        break;
                    case "right":
                        loc8.Add("r");
                        break;
                    case "left":
                        loc8.Add("l");
                        break;
                    default:
                        loc8.Add("d");
                        break;
                }
                _connection.SendXtMessage("PokemonPlanetExt", "f2", loc8, "str");
            }
            else if (type == "sendStopMineAnimation")
            {
                var loc8 = new ArrayList();
                switch (_dir)
                {
                    case "up":
                        loc8.Add("u");
                        break;
                    case "down":
                        loc8.Add("d");
                        break;
                    case "right":
                        loc8.Add("r");
                        break;
                    case "left":
                        loc8.Add("l");
                        break;
                    default:
                        loc8.Add("d");
                        break;
                }
                _connection.SendXtMessage("PokemonPlanetExt", "f3", loc8, "str");
            }
            else if (type == "goodHook")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(
                    loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b122", loc8, "str");
            }
            else if (type == "useItem")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add($"itemNum:{p1}");
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b1", loc8, "xml");
            }
            else if (type == "useItem2")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));

                loc8.Add($"i:{p1}");
                //loc8.Add(p2 != "" ? $"a:{p2}" : @"a:false");
                //loc8.Add(p3 != "" ? $"n:{p3}" : @"n:1");
                loc8.Add($"p:{p2}");
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");

                _connection.SendXtMessage("PokemonPlanetExt", "b11", loc8, "xml");
            }
            else if (type == "equipItem")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                loc8.Add(p2);
                _connection.SendXtMessage("PokemonPlanetExt", "b58", loc8, "str");
            }
            else if (type == "unequipItem")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b59", loc8, "str");
            }
            else if (type == "acceptEvolve")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b18", loc8, "xml");
            }
            else if (type == "declineEvolve")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b19", loc8, "xml");
            }
            else if (type == "declineBattle")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b14", loc8, "xml");
            }
            else if (type == "getHM")
            {
                var loc8 = new ArrayList();
                loc8.Add($"hmNum:{p1}");
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b22", loc8, "xml");
            }
            else if (type == "declineTrade")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt", "b17", loc8, "xml");
            }
            else if (type == "declineClanInvite")
            {
                var loc8 = new ArrayList();
                var packetKey = Connection.GenerateRandomString(Rand.Next(5, 20));
                loc8.Add(
                    $"pke:{Connection.CalcMd5(packetKey + kg2 + GetTimer())}");
                loc8.Add($"pk:{packetKey}");
                loc8.Add($"te:{GetTimer()}");
                _connection.SendXtMessage("PokemonPlanetExt",
                    Connection.CalcMd5("declineClanInvitekzf76adngjfdgh12m7mdlbfi9proa15gjqp0sd3mo1lk7w90cd" +
                                       Username), loc8, "xml");
            }
            else if (type == "updateFollowPokemon")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
                if (Team is null || Team.Count <= 0) return;
                if (Team[0].IsShiny)
                    loc8.Add($"{Team[0].Id + _shinyDifference}");
                else
                    loc8.Add($"{Team[0].Id}");
                _connection.SendXtMessage("PokemonPlanetExt", "b75", loc8, "str");
            }
            else if (type == "createCharacter")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
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
                _connection.SendXtMessage("PokemonPlanetExt", "b71", loc8, "str");
            }
            else if (type == "eliteBuy")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                _connection.SendXtMessage("PokemonPlanetExt", "b182", loc8, "str");
            }
            else if (type == "openChest")
            {
                var loc8 = new ArrayList();
                loc8.Add(GetTimer().ToString());
                loc8.Add(Connection.GenerateRandomString(Rand.Next(5, 20)));
                loc8.Add(Connection.CalcMd5(loc8[1] + kg2 + loc8[0]));
                loc8.Add(p1);
                loc8.Add(p2);
                _connection.SendXtMessage("PokemonPlanetExt", "b186", loc8, "str");
            }
        }
        private const int _shinyDifference = 721;
        public bool StopFishing()
        {
            IsFishing = false;
            _fishingTimeout.Cancel();
            _fishingTimeout.Set(Rand.Next(1500, 2000));
            return !IsFishing;
        }

        public bool StopMining()
        {
            //_finishingMiningTimeout.Set(Rand.Next(500, 1000));
            IsMinning = false;
            //_miningTimeout.Cancel();
            //GetTimeStamp("sendStopMineAnimation");
            return !IsMinning;
        }

        public bool ChangePokemon(int changeTo)
        {
            if (Battle && IsConnected && IsMapLoaded)
            {
                _battleTimeout.Set(Rand.Next(1000, 1100));
                GetTimeStamp("battleMove", "0", "switchPokemon", changeTo.ToString());
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
                    GetTimeStamp("getHM", num.ToString());
                    result = true;
                    break;
                case 2 when !HasItemName("HM02 - Fly"):
                    GetTimeStamp("getHM", num.ToString());
                    result = true;
                    break;
                case 3 when !HasItemName("HM03 - Surf"):
                    GetTimeStamp("getHM", num.ToString());
                    result = true;
                    break;
                case 5 when !HasItemName("HM05 - Flash"):
                    GetTimeStamp("getHM", num.ToString());
                    result = true;
                    break;
                default:
                    LogMessage?.Invoke($"Was unable to get HM0{num}.");
                    break;
            }

            return result;
        }

        public bool IsAnyMinableRocks()
        {
            return MiningObjects.Count > 0 && MiningObjects.Count(rock => !rock.IsMined) > 0;
        }

        public int CountMinableRocks()
        {
            return MiningObjects.Count(rock => !rock.IsMined);
        }

        public bool IsMinable(int x, int y) => IsGoldMember ? MiningObjects.Any(r => r.X == x && r.Y == y && !r.IsMined) :
            MiningObjects.Any(r => r.X == x && r.Y == y && !r.IsGoldMember && !r.IsMined);

        public bool SendFishing(string rod)
        {
            if (!Battle && _updatedMap && rod != "" && HasItemName(rod))
            {
                GetTimeStamp("fish", rod);
                _fishingTimeout.Set(Rand.Next(2000, 2500));
                //GetTimeStamp("sendFishAnimation");
                IsFishing = true;
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

        public bool Run()
        {
            if (Battle && !ActiveBattle.IsDungeonBattle)
            {
                GetTimeStamp("escapeBattle");
                _battleTimeout.Set(Rand.Next(1500, 2000));
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
            GetTimeStamp("battleMove", move.ToString());
            _battleTimeout.Set();
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
                    StringComparison.InvariantCultureIgnoreCase) && i.Quntity > 0);
        }

        public InventoryItem GetItemByUid(int itmUid) => Items.FirstOrDefault(i => i.Uid == itmUid && i.Quntity > 0);
        private void SendWildBattle(bool isSurf = false)
        {
            _movements.Clear();
            try
            {
                Shop = null;
                OpenedShop = null;

                _battleTimeout.Cancel();
                _battleTimeout.Set(Rand.Next(4000, 6000));
                if (Battle)
                    return;

                if (isSurf)
                    GetTimeStamp("wildBattle", "surf");
                else
                    GetTimeStamp("wildBattle");
                Battle = true;
                CanMove = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public bool StartWildBattle()
        {
            try
            {
                if (Battle)
                    return false;

                _movementTimeout.Set(Rand.Next(1000, 1500));
                _battleTimeout.Set(Rand.Next(3100, 3600));
                SendWildBattle();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public bool StartSurfWildBattle()
        {
            try
            {
                if (Battle)
                    return false;
                _movementTimeout.Set(Rand.Next(1000, 1500));
                _battleTimeout.Set(Rand.Next(3100, 3600));
                SendWildBattle(true);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public bool UseItem(string name = "", int pokeId = 0)
        {
            var item = GetItemFromName(name);
            if (IsMapLoaded && IsConnected && item != null)
            {
                if (Battle)
                {
                    _battleTimeout.Set(Rand.Next(1000, 2000));
                    GetTimeStamp("battleMove", "0", "i", (item.Uid - 1).ToString());
                    return true;
                }
                if (pokeId > 0)
                {
                    GetTimeStamp("useItem2", name, (pokeId - 1).ToString());
                    _itemTimeout.Set(Rand.Next(1000, 2000));
                    return true;
                }
                GetTimeStamp("useItem2", name, "0");
                _itemTimeout.Set(Rand.Next(1000, 2000));
                return true;
            }

            if (name == "")
            {
                if (IsMapLoaded && IsConnected)
                {
                    if (Battle && IsMapLoaded && IsConnected)
                    {
                        _battleTimeout.Set(Rand.Next(1000, 2000));
                        GetTimeStamp("battleMove", "0", "i", "0");
                        return true;
                    }
                    if (pokeId > 0)
                    {
                        GetTimeStamp("useItem2", Items.FirstOrDefault()?.Name, (pokeId - 1).ToString());
                        _itemTimeout.Set(Rand.Next(1000, 1500));
                        return true;
                    }
                    GetTimeStamp("useItem2", Items.FirstOrDefault()?.Name, "0");
                    _itemTimeout.Set(Rand.Next(1000, 2000));
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
            if (!_itemTimeout.IsActive && Team[pokemonUid - 1].ItemHeld != "")
            {
                SendTakeItem(pokemonUid - 1);
                _itemTimeout.Set();
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
            if (item == null || item.Quntity == 0)
            {
                return false;
            }
            if (!_itemTimeout.IsActive && !IsInBattle && item.IsEquipAble(Team[pokemonUid - 1].Name))
            {
                SendGiveItem(pokemonUid - 1, itemId);
                _itemTimeout.Set();
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
        public void SendMovement(string tempDir)
        {
            Shop = null;
            OpenedShop = null;
            if (Battle is true || !IsConnected) return;
            if (_mapMovements >= 64)
                _mapMovements = 0;
            else
                _mapMovements = _mapMovements + _mapMovementSpeed;


            if (_moveType == "bike")
            {
                _mapMovementSpeed = 16 * _movementSpeedMod;
            }
            else
            {
                _mapMovementSpeed = 8 * _movementSpeedMod;
            }

            _dir = tempDir;

            if (tempDir.ToLowerInvariant() == "up")
            {
                if (EncryptedTileY == Connection.CalcMd5(PlayerY + kg1 + Username))
                {
                    PlayerY--;
                    EncryptedTileY = Connection.CalcMd5(PlayerY + kg1 + Username);
                    GetTimeStamp("sendMovement", tempDir);
                    CheckForBattle();
                }
                else
                {
                    LogMessage?.Invoke("Coordinate encryption error");
                }
            }
            else if (tempDir.ToLowerInvariant() == "down")
            {
                if (EncryptedTileY == Connection.CalcMd5(PlayerY + kg1 + Username))
                {
                    PlayerY++;
                    EncryptedTileY = Connection.CalcMd5(PlayerY + kg1 + Username);
                    GetTimeStamp("sendMovement", tempDir);
                    CheckForBattle();
                }
                else
                {
                    LogMessage?.Invoke("Coordinate encryption error");
                }
            }
            else if (tempDir.ToLowerInvariant() == "left")
            {
                if (EncryptedTileX == Connection.CalcMd5(PlayerX + kg1 + Username))
                {
                    PlayerX--;
                    EncryptedTileX = Connection.CalcMd5(PlayerX + kg1 + Username);
                    GetTimeStamp("sendMovement", tempDir);
                    CheckForBattle();
                }
                else
                {
                    LogMessage?.Invoke("Coordinate encryption error");
                }
            }
            else if (tempDir.ToLowerInvariant() == "right")
            {
                if (EncryptedTileX == Connection.CalcMd5(PlayerX + kg1 + Username))
                {
                    PlayerX++;
                    EncryptedTileX = Connection.CalcMd5(PlayerX + kg1 + Username);
                    GetTimeStamp("sendMovement", tempDir);
                    CheckForBattle();
                }
                else
                {
                    LogMessage?.Invoke("Coordinate encryption error");
                }
            }

            _dir = tempDir;
            PlayerPositionUpdated?.Invoke();
        }

        private void CheckForBattle()
        {
            if (movingForBattle)
            {
                if (Team[0].AbilityNo == 1 || Team[0].AbilityNo == 73 || Team[0].AbilityNo == 95)
                {
                    if (Rand.Next(1, 14) == 7)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Team[0].AbilityNo == 35 || Team[0].AbilityNo == 71)
                {
                    if (Rand.Next(1, 9) <= 2)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Team[0].AbilityNo == 99)
                {
                    if (Rand.Next(1, 90) <= 15)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Rand.Next(1, 9) == 7)
                {
                    SendWildBattle(_moveType == "surf");
                } // end else if
                else if (Team[0].AbilityNo == 1 || Team[0].AbilityNo == 73 || Team[0].AbilityNo == 95)
                {
                    if (Rand.Next(1, 27) == 7)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Team[0].AbilityNo == 35 || Team[0].AbilityNo == 71)
                {
                    if (Rand.Next(1, 9) == 7)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Team[0].AbilityNo == 99)
                {
                    if (Rand.Next(1, 180) <= 15)
                    {
                        SendWildBattle(_moveType == "surf");
                    } // end if
                }
                else if (Rand.Next(1, 18) == 7)
                {
                    SendWildBattle(_moveType == "surf");
                } // end else if
            }
        }

        private string kg1 => _gameConnection.KG1Value;
        private string kg2 => _gameConnection.KG2Value;
        public bool CheckMapExits(int x, int y)
        {
            if (EncryptedstepsWalked == Connection.CalcMd5(_stepsWalked + kg1 + Username))
            {
                _stepsWalked++;
                EncryptedstepsWalked = Connection.CalcMd5(_stepsWalked + kg1 + Username);
                if (_stepsWalked >= 256)
                {
                    //getTimeStamp("stepsWalked", tempDir);
                    _stepsWalked = 0;
                    EncryptedstepsWalked =
                        Connection.CalcMd5(_stepsWalked + kg1 + Username);
                }
            }

            return false;
        }

        public void Move(Direction direction, bool isMovingForBattle = false, bool surfBattle = true)
        {
            movingForBattle = isMovingForBattle;
            _moveType = surfBattle ? "surf" : _moveType;
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
                if (item.Name.ToLowerInvariant() == itemName && item.Quntity >= 1)
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

        private bool _isMapLoaded()
        {
            if (MapName is null) return false;
            return _updatedMap && MapName.Length > 0;
        }
        public void Update()
        {
            _connection?.Update();
            _loadingTimeout?.Update();
            _movementTimeout?.Update();
            _battleTimeout?.Update();
            _swapTimeout?.Update();
            _itemTimeout?.Update();
            _miningTimeout?.Update();
            if (!(bool)_fishingTimeout?.Update())
            {
                IsFishing = false;
            }

            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (!IsMapLoaded) return;

            if (!_movementTimeout.IsActive && _movements.Count > 0)
            {
                var dir = _movements[0];
                _movements.RemoveAt(0);
                SendMovement(dir.AsString());
                _movementTimeout.Set(250);
            }
        }

        public bool SwapPokemon(int pokemon1, int pokemon2)
        {
            if (Battle || pokemon1 < 1 || pokemon2 < 1 || pokemon1 > Team.Count || pokemon2 > Team.Count ||
                pokemon1 == pokemon2) return false;
            if (_swapTimeout.IsActive is false)
            {
                // Damn SwapPokemon I really don't know why this works. How I did? I just test again and again till it swapped perfectly...

                if (Team.Count < 3)
                    SendSwapPokemons(pokemon1, pokemon2 - 1);
                else if (Team.Count >= 3)
                    SendSwapPokemons(pokemon1 - 1, pokemon2);

                _swapTimeout.Set();
                return true;
            }

            return false;
        }

        public bool GetStarter(string pokeName)
        {
            if (Team.Count <= 0)
            {
                SendGetStarter(pokeName.ToLowerInvariant());
                return true;
            }

            return false;
        }

        private void SendGetStarter(string pokeName)
        {
            GetTimeStamp("choosePokemon", pokeName);
        }

        private void SendSwapPokemons(int pokemon1, int pokemon2)
        {
            GetTimeStamp("reorderPokemon", pokemon1.ToString(), pokemon2.ToString());
        }
        private bool MovedLeft { get; set; } = false;
        private bool MovedRight { get; set; } = false;
        private bool MoveLeftAndRight()
        {
            _movementTimeout.Set(Rand.Next(1000, 1500));
            if (MovedLeft)
            {
                MovedLeft = false;
                SendMovement("right");
                MovedRight = true;
                return true;
            }

            if (MovedRight)
            {
                MovedLeft = true;
                SendMovement("left");
                MovedRight = false;
                return true;
            }

            MovedLeft = true;
            SendMovement("left");
            MovedRight = false;
            return true;
        }

        public void MineRock(int x, int y, string axe)
        {
            _lastRock = MiningObjects.Find(p => p.X == x && p.Y == y);
            GetTimeStamp("mine", axe, x.ToString(), y.ToString());
            //IsMinning = true;
            LogMessage?.Invoke($"Trying to mine the rock at (X:{x}, Y:{y})");
            _miningTimeout.Set(Rand.Next(2500, 3000));
            //GetTimeStamp("sendMineAnimation");
        }
    }
}