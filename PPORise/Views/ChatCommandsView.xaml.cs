using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PPOBot;
using PPOProtocol;
using Brush = System.Windows.Media.Brush; //ReSharper converted
using Brushes = System.Windows.Media.Brushes; //ReSharper converted

namespace PPORise
{
    /// <summary>
    /// Interaction logic for ChatCommandsView.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class ChatCommandsView : UserControl
    {
        private readonly TabItem _englishChatTab;
        private readonly TabItem _tradingChatTab;
        private readonly TabItem _localChatTab;
        private readonly TabItem _nonEnglishChatTab;
        private readonly TabItem _clanChatTab;
        private readonly TabItem _commandsTab;
        private readonly BotClient _bot;
        public ChatCommandsView(BotClient bot)
        {
            var bc = new BrushConverter();
            _bot = bot;
            InitializeComponent();

            _englishChatTab = new TabItem
            {
                Header = "English",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            TabControl.Items.Add(_englishChatTab);
            _tradingChatTab = new TabItem
            {
                Header = "Trading",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            TabControl.Items.Add(_tradingChatTab);
            _localChatTab = new TabItem
            {
                Header = "Local",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            TabControl.Items.Add(_localChatTab);
            _nonEnglishChatTab = new TabItem
            {
                Header = "Non English",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            TabControl.Items.Add(_nonEnglishChatTab);
            _clanChatTab = new TabItem
            {
                Header = "Clan",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            TabControl.Items.Add(_clanChatTab);
            _commandsTab = new TabItem
            {
                Header = "Commands",
                Content = new ChatPanel(),
                Background = (Brush) bc.ConvertFrom("#FF2C2F33"),
                Foreground = (Brush) bc.ConvertFrom("#FF99AAB5")
            };
            var richTextBox = (_commandsTab.Content as ChatPanel)?.ChatBox;
            var test = new TextRange(richTextBox?.Document.ContentEnd, richTextBox?.Document.ContentEnd)
            {
                Text = "Type \"/commands\" to know about commands.\n> "
            };
            test.ApplyPropertyValue(ForegroundProperty, Brushes.Aqua);
            TabControl.Items.Add(_commandsTab);

            _lastMessageSent = DateTime.UtcNow;
            _lastTradeMessageSent = DateTime.UtcNow;
        }

        private void InputChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (string.IsNullOrEmpty(InputChatBox.Text)) return;
                if (Equals(TabControl.SelectedItem as TabItem, _commandsTab))
                {
                    var commandSt = InputChatBox.Text;
                    if (string.IsNullOrEmpty(commandSt)) return;
                    ProcessCommands(commandSt);
                    InputChatBox?.Clear();
                    return;
                }

                if (!Equals(TabControl.SelectedItem as TabItem, _commandsTab) && InputChatBox.Text.Contains("/"))
                {
                    var result =
                        MessageBox.Show("Your message contains a command prefix and this message is here to make sure you are not posting a command in one of the general chats.\n" +
                                        "Are you sure you want to proceed?",
                            "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                    if (result == MessageBoxResult.Yes)
                    {
                        SendChatMessage(InputChatBox?.Text);
                        InputChatBox?.Clear();
                    }
                    else
                    {
                        InputChatBox?.Clear();
                        return;
                    }
                }

                if (_bot.Game != null && _bot.Game.IsMapLoaded)
                {
                    if (InputChatBox != null && InputChatBox.Text.StartsWith("@", StringComparison.Ordinal))
                    {
                        SendPrivateMessage(InputChatBox.Text);
                        InputChatBox?.Clear();
                        return;
                    }

                    if (Equals(TabControl.SelectedItem as TabItem, _englishChatTab))
                        SendChatMessage("!" + InputChatBox?.Text);
                    else if (Equals(TabControl.SelectedItem as TabItem, _localChatTab))
                        SendChatMessage("*" + InputChatBox?.Text);
                    else if (Equals(TabControl.SelectedItem as TabItem, _nonEnglishChatTab))
                        SendChatMessage("&" + InputChatBox?.Text);
                    else if (Equals(TabControl.SelectedItem as TabItem, _tradingChatTab))
                        SendChatMessage("$" + InputChatBox?.Text);
                    else if (Equals(TabControl.SelectedItem as TabItem, _clanChatTab))
                        SendChatMessage("%" + InputChatBox?.Text);
                    InputChatBox?.Clear();
                }
            }
        }
        private void SendPrivateMessage(string text)
        {
            Dispatcher.InvokeAsync(delegate {
                text = text.Substring(1);
                if (text.Length <= 0) return;
                var toName = text.Substring(0, text.IndexOf(" ", StringComparison.Ordinal));
                toName = toName.Replace("-", "");
                var index = text.IndexOf(" ", StringComparison.Ordinal);
                text = text.Substring(index, text.Length - index);
                lock (_bot)
                {
                    _bot.Game.GetTimeStamp("pm", toName, text);
                }

                var msg = text;
                foreach (TabItem tab in TabControl.Items)
                {
                    if (tab != null && !Equals(tab, _commandsTab))
                        AddChatMessage(((ChatPanel)tab.Content).ChatBox, message: $@"[To:{toName}]:{msg}", color: Brushes.Yellow);
                }
            });
        }
        DateTime _lastMessageSent;
        DateTime _lastTradeMessageSent;
        private void SendChatMessage(string message)
        {
            lock (_bot)
            {
                var isTradeChat = false;
                var isClanMsg = false;
                if (message.Length <= 0) return;
                if (_bot.Game is null)
                    return;
                if (!_bot.Game.IsMapLoaded)
                    return;
                var tempMsg = message;
                if (tempMsg.IndexOf("%", StringComparison.Ordinal) == 0)
                {
                    tempMsg = tempMsg.Substring(1, tempMsg.Length - 1);
                    isClanMsg = true;
                }
                else if (tempMsg.IndexOf("!", StringComparison.Ordinal) == 0)
                {
                    tempMsg = tempMsg.Substring(1, tempMsg.Length - 1);
                    tempMsg = "<g>" + tempMsg;
                }
                else if (tempMsg.IndexOf("*", StringComparison.Ordinal) == 0)
                {
                    tempMsg = tempMsg.Substring(1, tempMsg.Length - 1);
                    lock (_bot)
                    {
                        tempMsg = "<l>" + $"<{_bot.Game.MapName}>" + tempMsg;
                    }

                    //isLocalChat = true;
                }
                else if (tempMsg.IndexOf("$", StringComparison.Ordinal) == 0)
                {
                    tempMsg = tempMsg.Substring(1, tempMsg.Length - 1);
                    tempMsg = "<t>" + tempMsg;
                    isTradeChat = true;
                }
                else if (tempMsg.IndexOf("&", StringComparison.Ordinal) == 0)
                {
                    tempMsg = tempMsg.Substring(1, tempMsg.Length - 1);
                    tempMsg = "<n>" + tempMsg;
                }
                else
                {
                    tempMsg = "<g>" + tempMsg;
                }
                if (isClanMsg)
                {
                    if (!string.IsNullOrEmpty(_bot.Game.Clan))
                    {
                        _bot.Game.GetTimeStamp("clanMessage", tempMsg);
                        _lastMessageSent = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }
                    else
                    {
                        MessageBox.Show("You're not in a clan. If you're sure that you are in a clan please relog.", "No Clan", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                if (isTradeChat)
                {
                    if (_lastTradeMessageSent < DateTime.UtcNow)
                    {
                        _lastMessageSent = DateTime.UtcNow.AddSeconds(2);
                        _lastTradeMessageSent = DateTime.UtcNow.AddSeconds(60);
                        _bot.Game.GetTimeStamp("pmsg", tempMsg);
                    }
                }
                else if (_lastMessageSent < DateTime.UtcNow)
                {
                    _lastMessageSent = DateTime.UtcNow.AddSeconds(2);
                    _bot.Game.GetTimeStamp("pmsg", tempMsg);
                }
            }
        }
        private void ProcessSpecialChatMessages(string[] data, string msgObj) => Dispatcher.InvokeAsync(delegate
        {
            var rmsg = data[3];
            var matches = new Regex(@"#(?:[0-9a-fA-F]{3}){1,2}").Matches(rmsg); //finding color code :D
            var colorCode = "";
            if (matches.Count > 1)
                colorCode = matches[matches.Count - 1].Value;
            else if (matches.Count > 0)
                colorCode = matches[0].Value;

            if (!string.IsNullOrEmpty(colorCode) && !colorCode.StartsWith("#", StringComparison.Ordinal))
                colorCode = "#" + colorCode;
            rmsg = Regex.Replace(rmsg, @"\<(.*?)\>", "");
            if (msgObj.Contains("<font color='#00FF00'>"))
            {
                return;
            }
            foreach (TabItem tab in TabControl.Items)
            {
                if (tab != null && !Equals(tab, _commandsTab))
                {
                    if (!string.IsNullOrWhiteSpace(colorCode))
                        AddChatMessage(((ChatPanel)tab.Content)?.ChatBox, $"{rmsg}", (Brush)new BrushConverter().ConvertFromString(colorCode));
                    else
                        AddChatMessage(((ChatPanel)tab.Content)?.ChatBox, $"{rmsg}", Brushes.Gold);
                }
            }
        });

        public void ProcessPrivateMessages(string[] data)
        {
            Dispatcher.InvokeAsync(delegate {
                var from = data[3];
                //var memberTypeChat = data[4].Substring(0, 3);

                var msg = data[4].Substring(3, data[4].Length - 3);
                foreach (TabItem tab in TabControl.Items)
                {
                    if (tab != null && !Equals(tab, _commandsTab))
                    {
                        AddChatMessage(((ChatPanel)tab.Content)?.ChatBox, message: $@"[From:{from}]:{msg}", color: Brushes.Yellow);
                    }
                }

                PlayNotification();

            });
        }
        private void PlayNotification()
        {
            var window = Window.GetWindow(this);
            if (window != null && (!window.IsActive || !IsVisible))
            {
                var handle = new WindowInteropHelper(window).Handle;
                FlashWindowHelper.Flash(handle);

                if (File.Exists("Assets/message.wav"))
                {
                    using (var player = new SoundPlayer("Assets/message.wav"))
                    {
                        player.Play();
                    }
                }
            }
        }
        public void ChatMessage_Receieved(string msgObj, bool isClan)
        {
            var data = msgObj.Split('`');

            if (msgObj.Contains("r17"))
            {
                ProcessSpecialChatMessages(data, msgObj);
                return;
            }
            var msg = data[3];

            var typeMatches = new Regex(@"<([a-zA-Z])>").Matches(msg);
            var type = typeMatches[typeMatches.Count - 1].Value;
            msg = Regex.Replace(msg, @"<[a-zA-Z]>", "");

            var fromMap = "";

            var userName = data[4];

            if (type == "<l>")
            {
                var mapRegex = new Regex(@"\<(.*?)\>");
                var mapMatches = mapRegex.Matches(msg);
                foreach (Match mapMatch in mapMatches)
                {
                    if (mapMatch.Success)
                        fromMap = mapMatch.Groups[0].Value;
                }

                msg = msg.Replace(fromMap, "").Replace("<", "").Replace(">", "");
            }

            if (isClan)
                type = "<cl>";

            UpdateChatMessage(type, userName, msg, fromMap);
        }

        private void UpdateChatMessage(string type, string tempUsername, string msg, string fromMap)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (_bot)
                {
                    if (_bot.Game is null || !_bot.Game.IsMapLoaded) return;

                    if (type == "<g>")
                    {
                        AddChatMessage((_englishChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                    }
                    else if (type == "<l>" && string.Equals(fromMap.ToLowerInvariant(),
                                 _bot.Game.MapName.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        AddChatMessage((_localChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                    }
                    else if (type == "<t>")
                    {
                        AddChatMessage((_tradingChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                    }
                    else if (type == "<n>")
                    {
                        AddChatMessage((_nonEnglishChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                    }
                    else if (type == "<cl>")
                    {
                        AddChatMessage((_clanChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                    }
                    else
                        AddChatMessage((_localChatTab.Content as ChatPanel)?.ChatBox, $"{tempUsername}: {msg}",
                            Brushes.Aqua);
                }
            });
        }
        private void AddChatMessage(RichTextBox richTextBox, string message, Brush color = null)
        {
            string text;
            if (Equals(richTextBox, ((ChatPanel)_commandsTab.Content).ChatBox))
            {
                text = message;
            }
            else
            {
                text = "[" + DateTime.Now.ToLongTimeString() + "] " + message;
            }

            MainWindow.AppendLineToRichTextBox(richTextBox, text, color);

            if (Equals(richTextBox, ((ChatPanel) _commandsTab.Content).ChatBox))
            {
                var test2 = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd)
                {
                    Text = "> "
                };
                test2.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Aqua);
                if (richTextBox.Selection.IsEmpty)
                {
                    richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                    richTextBox.ScrollToEnd();
                }
            }
        }
        private void ProcessCommands(string command)
        {
            var commandSt = command.Substring(1);
            var commandArg = commandSt.Split();
            Dispatcher.InvokeAsync(delegate {
                try
                {
                    var rtCommand = ((ChatPanel) _commandsTab.Content).ChatBox;
                    AddChatMessage(rtCommand, $"{command}");
                    command = commandSt;
                    switch (commandArg[0].ToLowerInvariant())
                    {
                        case "commands":
                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                "Commands:\n/tp2 X Y MapName\t--Teleports to a map.\n/tp2 MapName,X,Y\t--Teleports to a map.\n/startBattle\t--Starts a wild battle of grass.\n" +
                                "/startSurfBattle\t--Starts a wild battle of surf(not fishing).\n/switchPokemon Pokemon_index\t--Changes active Pokemon to another specific Pokemon while wild battle.\n" +
                                "/useMove moveIndex/blank to use specific move or just type this to use first move.\n/run\t--Runs away from the current wild battle.\n/startFishing rod_name\t--Starts Fishing with specific rod.\n" +
                                "/stopFishing\t--Stops Fishing if fishing has been started.\n/useItem item_name or blank\t--Uses an specific item or uses first item if it is blank in battle.\n/getPokeMoves index_of_Pokemon\t--This will printout you're specific Pokemon's Moves.\n" +
                                "/swapPokemon Pokemon1,Pokemon2\t Swaps Pokemon.\n/getMiningRocks\t--Counts total minable rocks of current map.\n/startTrainerBattle index,name" +
                                "/useItemOn Item Name,Pokemon_Index\t--Uses the specified item on the specified pokémon.\n/takeItemFromPokemon Pokemon_index\t--Takes the held item from the specified pokemon.\n/giveItemToPokemon Item Name,Pokemon_index\t--Gives the specified item on the specified pokemon.\n" +
                                "/openShop\t--Opens the Pokemart shop.\n/buyItem ItemName,amount\t--Buys the specified item from the opened shop.\n/pokemon\t--Prints out the Pokemon names that can be found in your current map.\n/setMount mount_name\n/setSurfMount mount_name" +
                                "\n/countColoredRocks rock_color\t--Counts all rocks which color is specific.\n/findClosestRock\t--Finds the closests rock and prints out the cell.\n/moveLeft\t--Moves the player left.\n/moveRight\t--Moves the player right.\n/mineRock axe_name,x,y" +
                                "\n/moveDown\t--Moves the player down.\n/moveUp\t--Moves the player up.\n/version\t--Prints out the version of the current bot.\n/moveToCell X,Y\t--Moves the player to specific coordinate\n/getHM 1-5\t--Get specific HM without moving the player.\n/removeMoney \t--Ammount you want to remove." +
                                "\n/useBike\t--Will use bike while moving.\n/buyEliteTokens amount\t--Buys elite tokens.\n/getStarter starter_name\t--Gets a starter.\n/withdrawPokemonFromPC box_index,pokemonBox_index\t--Withdraws a pokemon from the PC.\n/depositePokemonToPC box_index,team_index\t--Deposites a pokemon to PC");
                            break;
                        case "withdrawpokemonfrompc":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        if (!command.Contains(","))
                                            return;
                                        var checkbox = int.TryParse(command.Split(',')[0], out var box);
                                        var check = int.TryParse(command.Split(',')[1], out var index);
                                        if (check && checkbox)
                                            _bot.Game.WithdrawPokemonFromPC(box, index);
                                    }
                                }
                            }
                            break;
                        case "depositepokemontopc":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        if (!command.Contains(","))
                                            return;
                                        var checkbox = int.TryParse(command.Split(',')[0], out var box);
                                        var check = int.TryParse(command.Split(',')[1], out var index);
                                        if (check && checkbox)
                                            _bot.Game.DepositePokemonToPC(box, index);
                                    }
                                }
                            }
                            break;
                        case "getstarter":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var name = command;

                                        _bot.Game.ChoosePokemon(name);

                                    }
                                }
                            }
                            break;
                        case "removemoney":
                            lock(_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var amount = Convert.ToInt32(command);
                                        _bot.Game.RemoveMoney(amount);
                                    }
                                }
                            }
                            break;
                        case "buyelitetokens":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var amount = Convert.ToInt32(command);

                                        _bot.Game.GetTimeStamp("eliteBuy", amount.ToString());

                                    }
                                }
                            }
                            break;
                        case "movetocell":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var x = Convert.ToInt32(command.Split(',')[0]);
                                        var y = Convert.ToInt32(command.Split(',')[1]);

                                        _bot.MoveToCell(x, y, "");
                                    }
                                }
                            }

                            break;
                        case "usebike":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        if (_bot.Game._moveType == "bike")
                                            _bot.Game.SetMount("Bike");
                                        else
                                            _bot.Game.SetMount("");
                                    }
                                }
                            }

                            break;
                        case "setmount":                           
                        case "setsurfmount":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var name = command;

                                        _bot.Game.SetMount(name, commandArg[0].ToLowerInvariant() == "setsurfmount");

                                    }
                                }
                            }
                            break;
                        case "gethm":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded && !_bot.Game.IsInBattle)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var hmNum = Convert.ToInt16(command);

                                        _bot.Game.GetHM(hmNum);

                                    }
                                }
                            }

                            break;
                        case "buymerchantitem":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        command = command.Replace(commandArg[0] + " ", "");
                                        var name = command;
                                        _bot.Game.buyMerchantItem(name);

                                    }
                                }
                            }

                            break;
                        case "tp2":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    command = command.Replace(commandArg[0] + " ", "");
                                    var wholeCommand = command.Contains(",") ? command.Split(',') : command.Split();
                                    if (wholeCommand.Length > 3)
                                    {
                                        var loc3 = 3;
                                        while (loc3 < wholeCommand.Length)
                                        {
                                            wholeCommand[2] = wholeCommand[2] + (" " + wholeCommand[loc3]);
                                            loc3 = loc3 + 1;
                                        }

                                        if (command.Contains(","))
                                        {
                                            foreach (var s in wholeCommand)
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox, s);
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "Something is wrong with the command 'tp2'. Please recheck the command.");
                                            return;
                                        }
                                    }

                                    var mapName = command.Contains(",") ? wholeCommand[0] : wholeCommand[2];
                                    var x = command.Contains(",")
                                        ? Convert.ToInt32(wholeCommand[1])
                                        : Convert.ToInt32(wholeCommand[0]);
                                    var y = command.Contains(",")
                                        ? Convert.ToInt32(wholeCommand[2])
                                        : Convert.ToInt32(wholeCommand[1]);
                                    _bot.Game.LoadMap(true, mapName, x, y);
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "startbattle":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && !_bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        _bot.Game.StartWildBattle();
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're not in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        @"Please make sure you're logged in/you're not in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                }
                            }

                            break;
                        case "startsurfbattle":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && !_bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        _bot.Game.StartSurfWildBattle();
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're not in battle/you got at least 1 Pokemon/you're current map is loaded.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        @"Please make sure you're logged in/you're not in battle/you got at least 1 Pokemon/you're current map is loaded.");
                                }
                            }
                            break;
                        case "starttrainerbattle":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && !_bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        var splitted = commandArg[1].Split(',');
                                        _bot.Game.StartTrainerBattle(splitted[0], splitted[1]);
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're not in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        @"Please make sure you're logged in/you're not in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                }
                            }
                            break;
                        case "usemove":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && _bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        if (commandArg.Length > 1)
                                        {
                                            var moveIndex = Convert.ToInt32(commandArg[1]);
                                            if (moveIndex < 1 || moveIndex > 4)
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "Please give a valid move Uid.");
                                            }

                                            if (!_bot.AI.UseMove(moveIndex))
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "Something went wrong while using the move.");
                                            }
                                        }
                                        else if (!_bot.AI.UseMove(1))
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "Something went wrong while using the move.");
                                        }

                                    }
                                    else
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're must be in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                }
                            }

                            break;
                        case "switchpokemon":

                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && _bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        if (commandArg.Length > 0)
                                        {
                                            _bot.AI.SendPokemon(Convert.ToInt32(commandArg[1]));
                                        }
                                        else if (commandArg.Length == 0)
                                        {
                                            _bot.AI.SendPokemon(0);
                                        }
                                        else
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "Something went wrong while using the move. Please make sure you gave correct move index.");
                                    }
                                    else
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're must be in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                }
                            }

                            break;
                        case "run":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && _bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0)
                                    {
                                        _bot.Game.Run();
                                    }
                                    else
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            @"Please make sure you're logged in/you're must be in battle/you got atleast 1 Pokemon/you're current map is loaded.");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "startfishing":
                            lock (_bot)
                            {
                                if (_bot.Game is null is false)
                                {
                                    if (commandArg.Length > 0)
                                    {
                                        if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && !_bot.Game.IsInBattle &&
                                            _bot.Game.Team.Count > 0 && !_bot.Game.IsFishing)
                                        {
                                            if (commandArg.Length > 1)
                                            {
                                                var loc3 = 2;
                                                while (loc3 < commandArg.Length)
                                                {
                                                    commandArg[1] = commandArg[1] + (" " + commandArg[loc3]);
                                                    loc3 = loc3 + 1;
                                                }

                                                _bot.Game.FishWith(commandArg[1]);
                                            }
                                            else
                                                AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                                    "Please provide a valid rod name.");
                                        }
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "stopfishing":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded && _bot.Game.IsInBattle &&
                                        _bot.Game.Team.Count > 0 && _bot.Game.IsFishing)
                                    {
                                        _bot.Game.StopFishing();
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "useitem":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle && _bot.Game.IsConnected && _bot.Game.IsMapLoaded)
                                    {
                                        if (commandArg.Length > 1)
                                        {
                                            var loc3 = 2;
                                            while (loc3 < commandArg.Length)
                                            {
                                                commandArg[1] = commandArg[1] + (" " + commandArg[loc3]);
                                                loc3 = loc3 + 1;
                                            }

                                            _bot.Game.UseItem(commandArg[1]);
                                        }
                                        else
                                            _bot.Game.UseItem();
                                    }
                                    else if (_bot.Game.IsConnected && _bot.Game.IsMapLoaded)
                                    {
                                        if (commandArg.Length > 1)
                                        {
                                            var loc3 = 2;
                                            while (loc3 < commandArg.Length)
                                            {
                                                commandArg[1] = commandArg[1] + (" " + commandArg[loc3]);
                                                loc3 = loc3 + 1;
                                            }

                                            _bot.Game.UseItem(commandArg[1]);
                                        }
                                        else if (commandArg.Length >= 1)
                                        {
                                            _bot.Game.UseItem(commandArg[1]);
                                        }
                                        else
                                            _bot.Game.UseItem();
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "getactivepoke":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle && _bot.Game.IsConnected && _bot.Game.IsMapLoaded)
                                    {
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            $"Active Pokemon: {_bot.AI.ActivePokemon.Name}");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "getpokemoves":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    lock (_bot)
                                    {
                                        if (commandArg.Length > 1)
                                        {
                                            int index = int.Parse(commandArg[1]);

                                            if (index < 1 || index > _bot.Game.Team.Count)
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "Please provide a valid index number of you're team eg. First Pokemon '/getPokeMoves 1'");
                                                return;
                                            }

                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                $"Moves of {_bot.Game.Team[index - 1].Name}:");
                                            foreach (var move in _bot.Game.Team[index - 1].Moves)
                                            {
                                                if (move != null && move.Name != null && move.Data != null)
                                                {
                                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                        "Move: " + move.Name +
                                                        $" - Power: {(move.Data.Power == -1 ? 0 : move.Data.Power)} - Acc: {move.Data.Accuracy}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "Please provide a valid index number of you're team eg. First Pokemon '/getPokeMoves 1'");
                                        }
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "swappokemon":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (!_bot.Game.IsInBattle && command.Contains(","))
                                    {
                                        var commandPer = command.Replace("swapPokemon ", "").Split(',');
                                        _bot.Game.SwapPokemon(int.Parse(commandPer[0]), int.Parse(commandPer[1]));
                                    }
                                    else
                                        AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                            "Please make sure you're not in a battle and the example of swaping Pokemon is '/swapPokemon 1,2'");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "getminingrocks":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.MiningObjects != null)
                                    {
                                        if (_bot.Game.IsAnyMinableRocks())
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                $"There is total {_bot.Game.CountMinableRocks()} rocks which can be mined.");
                                        }
                                        else
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "There is no mineable rock in your current map");
                                        }
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "openshop":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        if (_bot.Game.OpenedShop == null)
                                        {
                                            if (_bot.Game.MapName.ToLowerInvariant().Contains("mart") || _bot.Game.MapName.ToLowerInvariant().Contains("plateau"))
                                            {
                                                _bot.Game.OpenShop();
                                            }
                                            else
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "You cannot open shop in your current map. You must be in a Pokemart.");
                                            }
                                        }
                                        else
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "A shop is already opened.");
                                        }
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "buyitem":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        if (command.Contains(","))
                                        {
                                            command = command.Replace(commandArg[0] + " ", "");
                                            var buyCommandStrings = command.Split(',');
                                            if (buyCommandStrings.Length == 2)
                                            {
                                                int amount = Convert.ToInt32(buyCommandStrings[1]);
                                                ShopItem item = _bot.Game.OpenedShop.ShopItems.FirstOrDefault(i =>
                                                    i.Name.Equals(buyCommandStrings[0],
                                                        StringComparison.InvariantCultureIgnoreCase));
                                                if (item != null)
                                                {
                                                    _bot.Game.BuyItem(item.Uid.GetValueOrDefault(), amount);
                                                }
                                                else
                                                {
                                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                        "error: buyItem: the item '" + buyCommandStrings[0] +
                                                        "' does not exist in the opened shop.");
                                                }
                                            }
                                            else
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "You have to use 'buyitem' like: '/buyItem Item Name,amount");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "useitemon":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        if (command.Contains(","))
                                        {
                                            command = command.Replace(commandArg[0] + " ", "");
                                            var buyCommandStrings = command.Split(',');
                                            if (buyCommandStrings.Length == 2)
                                            {
                                                var itemName = buyCommandStrings[0];
                                                var pokemonIndex = Convert.ToInt32(buyCommandStrings[1]);
                                                if (pokemonIndex < 1 || pokemonIndex > _bot.Game.Team.Count)
                                                {
                                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                        "error: useItemOnPokemon: tried to retrieve the non-existing pokemon " +
                                                        pokemonIndex + ".");
                                                    return;
                                                }

                                                itemName = itemName.ToUpperInvariant();
                                                InventoryItem item =
                                                    _bot.Game.GetItemFromName(itemName.ToUpperInvariant());

                                                if (item != null && item.Quantity > 0)
                                                {
                                                    if (!_bot.Game.IsInBattle && !item.IsEquipAble())
                                                    {
                                                        _bot.Game.UseItem(item.Name, pokemonIndex);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                            "Please login first to use this command or wait for some seconds until it loads all game data.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "giveitemtopokemon":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        if (command.Contains(","))
                                        {
                                            command = command.Replace(commandArg[0] + " ", "");
                                            var buyCommandStrings = command.Split(',');
                                            if (buyCommandStrings.Length == 2)
                                            {
                                                var itemName = buyCommandStrings[0];
                                                var pokemonIndex = Convert.ToInt32(buyCommandStrings[1]);
                                                if (pokemonIndex < 1 || pokemonIndex > _bot.Game.Team.Count)
                                                {
                                                    AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                        "error: useItemOnPokemon: tried to retrieve the non-existing pokemon " +
                                                        pokemonIndex + ".");
                                                    return;
                                                }

                                                InventoryItem item = _bot.Game.GetItemFromName(itemName);

                                                if (item != null && item.Quantity > 0)
                                                {
                                                    if (!_bot.Game.IsInBattle && item.IsEquipAble())
                                                    {
                                                        _bot.Game.GiveItemToPokemon(pokemonIndex, item.Uid);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                            "Please login first to use this command or wait for some seconds until it loads all game data.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "takeitemfrompokemon":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        try
                                        {
                                            command = command.Replace(commandArg[0] + " ", "");
                                            var pokemonIndex = Convert.ToInt32(command);
                                            if (pokemonIndex < 1 || pokemonIndex > _bot.Game.Team.Count)
                                            {
                                                AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                    "error: takeItemFromPokemon: tried to retrieve the non-existing pokemon " +
                                                    pokemonIndex + ".");
                                                return;
                                            }

                                            _bot.Game.TakeItemFromPokemon(pokemonIndex);
                                        }
                                        catch (Exception)
                                        {
                                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                                "Something went wrong while taking item from the Pokemon. Try 'takeItemFromPokemon' like: /takeItemFromPokemon pokemonIndex");
                                        }
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                            "Please login first to use this command or wait for some seconds until it loads all game data.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }

                            break;
                        case "pokemon":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsMapLoaded)
                                    {
                                        _bot.Game.GetTimeStamp("command", command.Trim());
                                    }
                                    else
                                    {
                                        AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                            "Please login first to use this command or wait for some seconds until it loads all game data.");
                                    }
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "countcoloredrocks":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.MiningObjects is null || _bot.Game.MiningObjects.Count > 0 is false)
                                    {
                                        AddChatMessage(rtCommand, "Sorry, there is no rock around your map.");
                                    }

                                    var totalColoredRocks = _bot.Game.MiningObjects.Count(rock =>
                                        string.Equals(rock.Color, commandArg[1],
                                            StringComparison.InvariantCultureIgnoreCase));
                                    AddChatMessage(rtCommand,
                                        totalColoredRocks > 0 is false
                                            ? $"There is no {commandArg[1]} colored rock."
                                            : $"There is total {totalColoredRocks}, {commandArg[1]} colored rocks.");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "findclosestrock":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.MiningObjects is null || _bot.Game.MiningObjects.Count > 0 is false)
                                    {
                                        AddChatMessage(rtCommand, "Sorry, there is no rock around your map.");
                                        return;
                                    }

                                    var closeRock = _bot.MiningAI.FindClosestRock();
                                    AddChatMessage(rtCommand,
                                        closeRock is null
                                            ? "Error: did not find any close rock."
                                            : $"The closest unmined rock is at ({closeRock.X}, {closeRock.Y}).");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "minerock":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.MiningObjects is null || _bot.Game.MiningObjects.Count > 0 is false)
                                    {
                                        AddChatMessage(rtCommand, "Sorry, there is no rock around your map.");
                                        return;
                                    }
                                    var arr = command.Replace(commandArg[0] + " ", "").Split(',');
                                    if (arr.Length != 3)
                                    {
                                        AddChatMessage(rtCommand, "Sorry, make sure you've entered x,y and the axe name.");
                                        return;
                                    }
                                    _bot.Game.MineRock(int.Parse(arr[1]), int.Parse(arr[2]), arr[0]); 
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "moveleft":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle || !_bot.Game.IsMapLoaded) return;
                                    _bot.Game.Move(Direction.Left, "");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "moveright":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle || !_bot.Game.IsMapLoaded) return;
                                    _bot.Game.Move(Direction.Right, "");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "movedown":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle || !_bot.Game.IsMapLoaded) return;
                                    _bot.Game.Move(Direction.Down, "");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "moveup":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    if (_bot.Game.IsInBattle || !_bot.Game.IsMapLoaded) return;
                                    _bot.Game.Move(Direction.Up, "");
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        case "version":
                            AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                       $"The version of your current bot is {App.Version}.");
                            break;
                        case "watch":
                            lock (_bot)
                            {
                                if (_bot.Game != null)
                                {
                                    _bot.Game.GetTimeStamp("command", command.Trim());
                                }
                                else
                                {
                                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox,
                                        "Please login first to use this command or wait for some seconds until it loads all game data.");
                                }
                            }
                            break;
                        default:
                            AddChatMessage(((ChatPanel) _commandsTab.Content).ChatBox,
                                $"The command \"{command}\", doesn't not exist.", Brushes.OrangeRed);
                            break;
                    }
                }
                catch (Exception e)
                {
                    AddChatMessage(((ChatPanel)_commandsTab.Content).ChatBox, e.Message + " You should try the command again but perfectly. Type '/commands' for more information.");
                }
            });
        }
    }
    //Helper Class
    //Special Class to find out text lines
    public static class Extentions
    {
        public static long TotalLines(this string s)
        {
            long count = 1;
            int position = 0;
            while ((position = s.IndexOf(Environment.NewLine, position)) != -1)
            {
                count++;
                position++;
            }
            return count;
        }
    }
}
