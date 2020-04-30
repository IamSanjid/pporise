using Microsoft.Win32;
using PPOBot;
using PPOProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

// ReSharper disable once CheckNamespace
namespace PPORise
{
    #region BLURWINDOW
    internal enum AccentState
    {
        // ReSharper disable once InconsistentNaming
        ACCENT_DISABLED = 0,// ReSharper disable once InconsistentNaming
        ACCENT_ENABLE_GRADIENT = 1,// ReSharper disable once InconsistentNaming
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,// ReSharper disable once InconsistentNaming
        ACCENT_ENABLE_BLURBEHIND = 3,// ReSharper disable once InconsistentNaming
        ACCENT_INVALID_STATE = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        // ReSharper disable once InconsistentNaming
        WCA_ACCENT_POLICY = 19
        // ...
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public static class StringExtention //lol...
    {
        public static string Clear(this string value)
        {
            if (value == null) return null;
            value = "";
            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        #region Custom Window Things
        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
        private void PPORiseWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
                
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void PPORiseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
        }
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                btnMaximizeIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.WindowMaximize;
                btnMaximizeTT.Text = "Maximize";
            }
            else
            {
                WindowState = WindowState.Maximized;
                btnMaximizeIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.WindowRestore;
                btnMaximizeTT.Text = "Restore Down";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion
        public BotClient Bot { get; }

        public TeamView Team { get; }
        public ChatCommandsView ChatCommandsView { get; }
        public InventoryView Inventory { get; }
        public BattleStatView BattleStat { get; }
        public PlayersView Players { get; }
        private FileLogger FileLog { get; }
        private List<TabView> _views = new List<TabView>();

        DateTime _refreshPlayers;
        int _refreshPlayersDelay;

        private struct TabView
        {
            public ContentControl Content { get; set; }
            public ListViewItem Button { get; set; }
        }
        public MainWindow()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            Thread.CurrentThread.Name = "UI Thread";
            Bot = new BotClient();
            Bot.ConnectionOpened += Bot_ConnectionOpened;
            Bot.ConnectionClosed += Bot_ConnectionClosed;
            Bot.WebSuccessfullyLoggedIn += OnWebSuccessfullyLoggedIn;
            Bot.AutoReconnector.StateChanged += Bot_AutoReconnectorStateChanged;
            Bot.PokemonEvolver.StateChanged += Bot_PokemonEvolverStateChanged;
            Bot.LogMessage += Bot_LogMessage;
            Bot.ColoredLogMessage += Bot_ColoredLogMessage;
            Bot.ClientChanged += Bot_ClientChanged;
            Bot.StateChanged += Bot_StateChanged;

            InitializeComponent();

            Bot.PokemonEvolver.IsEnabled = Bot.Settings.AutoEvolve;
            Bot.AutoReconnector.IsEnabled = Bot.Settings.AutoReconnect;

            FileLog = new FileLogger();

            _refreshPlayers = DateTime.UtcNow;
            _refreshPlayersDelay = 5000;

            Team = new TeamView(Bot, this);
            ChatCommandsView = new ChatCommandsView(Bot);
            Inventory = new InventoryView(Bot, this);
            BattleStat = new BattleStatView(this);
            Players = new PlayersView(Bot, this);


            AddView(Team, TeamContent, TeamTab, true);
            AddView(ChatCommandsView, ChatCommandsContent, ChatTab);
            AddView(Inventory, InventoryContent, InventoryTab);
            AddView(BattleStat, BattleStatsContent, BattleStatTab);
            AddView(Players, PlayersContent, PlayersTab);

            App.InitializeVersion();

            SetTitle(null);
            LogMessage("Running " + App.Name + " by " + App.Author + ", version " + App.Version);
            if (App.IsBeta)
            {
                LogMessage("This is a BETA version. Bugs, crashes and bans might occur.");
                LogMessage("Join the Discord chat for the latest information and to report issues.");
            }

            if (!string.IsNullOrEmpty(Bot.Settings.LastScript) && File.Exists(Bot.Settings.LastScript))
            {
                LogMessage($"Reloadable Script: {Bot.Settings.LastScript}.\tPress Ctrl+R to reload.");
                ReloadPopup.IsEnabled = true;
                ReloadPopup.Content = "Reload " + Path.GetFileName(Bot.Settings.LastScript) + "\tCtrl+R";
            }
            else
            {
                ReloadPopup.IsEnabled = false;
                ReloadPopup.Content = "Reload Script";
                Bot.Settings.LastScript.Clear(); //lol
            }

            Task.Run(() => UpdateClients());
        }

        private void Bot_ConnectionOpened()
        {
            Dispatcher.InvokeAsync(delegate
            {
                SetTitle(Bot.Account.Name);
                LoginButton.IsEnabled = true;
                LoginButton2.Content = "Logout";
                LoginButton2.IsEnabled = true;
                LoginButtonIcon.Kind = PackIconKind.Logout;
                LogMessage("Connected to the game server.", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                LogMessage("Sending authentication to the server....", Brushes.ForestGreen);
            });
        }

        private void Bot_ConnectionClosed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LoginButton.IsEnabled = true;
                LoginButton2.Content = "Login";
                LoginButton2.IsEnabled = true;
                LoginButtonIcon.Kind = PackIconKind.Login;
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.Red;
            });
        }
        private void Bot_LoggingFailed(Exception ex)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(@"Logging failed. Please check your password and username again or your Internet connection or please use another Http proxy (if you have used one).", Brushes.OrangeRed);
                LoginButton.IsEnabled = true;
                LoginButton2.Content = "Login";
                LoginButton2.IsEnabled = true;
                LoginButtonIcon.Kind = PackIconKind.Login;
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.Red;
            });
        }
        private void Client_LoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Loaded Game Data successfully!", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                StatusText.Text = "Online";
                StatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#28d659");
            });
        }
        private void Bot_ColoredLogMessage(string message, Brush color)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message, color);
            });
        }
        private void SetTitle(string username)
        {
            Title = username == null ? "" : username + " - ";
            Title += App.Name + " " + App.Version;
#if DEBUG
            Title += " (debug)";
#endif
            MainTitle.Text = Title;
        }
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.InvokeAsync(() => HandleUnhandledException(e.Exception.InnerException));
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                if (ex != null)
                {
                    File.WriteAllText("crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt",
                        App.Name + @" " + App.Version + @" crash report: " + Environment.NewLine + ex);
                }
                MessageBox.Show(App.Name + " encountered a fatal error. The application will now terminate." + Environment.NewLine +
                    "An error file has been created next to the application.", App.Name + " - Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch
            {
                //ignore
            }
        }
        private void UpdateClients()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.Update();
                }
                Bot.Update();
            }
            Task.Delay(1).ContinueWith((previous) => UpdateClients());
        }
        private void LogMessage(string message, Brush color)
        {
            /*var test = new TextRange(MessageTextBox.Document.ContentEnd, MessageTextBox.Document.ContentEnd)
            {
                Text = "[" + DateTime.Now.ToLongTimeString() + "] " + message + '\r'
            };

            // Coloring there.
            test.ApplyPropertyValue(TextElement.ForegroundProperty, color);*/
            var text = "[" + DateTime.Now.ToLongTimeString() + "] " + message;
            AppendLineToRichTextBox(MessageTextBox, text, color);
            FileLog.Append(text);
            //MessageTextBox.ScrollToEnd();
        }

        public static void AppendLineToRichTextBox(RichTextBox richTextBox, string message, Brush color = null)
        {
            if (color is null)
                color = Brushes.Aqua;

            Paragraph para;

            para = richTextBox.Document.Blocks.LastBlock as Paragraph;

            para.Inlines.Add(new Run(message)
            {
                Foreground = color
            });

            richTextBox.Document.Blocks.Add(para);
            // adding extra line...
            richTextBox.Document.Blocks.Add(new Paragraph() { Margin = new Thickness(0) });

            var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            if (range.Text.Length > 12000)
            {
                var text = range.Text;
                text = text.Substring(text.Length - 10000, 10000);
                var index = text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                if (index != -1)
                {
                    text = text.Substring(index + Environment.NewLine.Length);
                }
                var remove_lines = richTextBox.Document.Blocks.Count - (int)text.TotalLines();
                for (var i = remove_lines; i >= 0; i--)
                {
                    richTextBox.Document.Blocks.Remove(richTextBox.Document.Blocks.ElementAt(i));
                }
            }

            if (richTextBox.Selection.IsEmpty)
            {
                richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                richTextBox.ScrollToEnd();
            }
        }

        private void LogMessage(string message)
        {
            var bc = new BrushConverter();
            LogMessage(message, (Brush)bc.ConvertFrom("#FF99AAB5"));
        }
        private void LogMessage(string format, params object[] args)
        {
            LogMessage(string.Format(format, args));
        }
        private void AutoEvolveSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = true;
            }
        }
        private void AutoEvolveSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = false;
            }
        }
        private void Bot_PokemonEvolverStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoEvolve = value;
                if (AutoEvolveSwitch.IsChecked == value) return;
                AutoEvolveSwitch.IsChecked = value;
            });
        }
        private void AutoReconnectSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = true;
            }
        }
        private void AutoReconnectSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = false;

            }
        }
        private void Bot_AutoReconnectorStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoReconnect = value;
                if (AutoReconnectSwitch.IsChecked == value) return;
                AutoReconnectSwitch.IsChecked = value;
            });
        }
        private void OpenLoginWindow()
        {
            var login = new LoginWindow(Bot) { Owner = this };
            var result = login.ShowDialog();
            if (result != true)
            {
                return;
            }
            LoginButton.IsEnabled = false;
            LoginButton2.Content = "Logout";
            LoginButton2.IsEnabled = false;
            Login(login);
        }
        /// <summary>
        /// Login to the server manually.
        /// </summary>
        /// <param name="login"></param>
        private void Login(LoginWindow login)
        {
            try
            {               
                lock(Bot)
                {
                    var account = new Account(login.Username) { Password = login.Password };

                    if (Bot.AccountManager.Accounts.ContainsKey(login.Username))
                    {
                        account.HashPassword = Bot.AccountManager.Accounts[login.Username].HashPassword;
                        account.ID = Bot.AccountManager.Accounts[login.Username].ID;
                        account.Username = Bot.AccountManager.Accounts[login.Username].Username;
                    }

                    if (string.IsNullOrEmpty(account.ID) || string.IsNullOrEmpty(account.HashPassword) || string.IsNullOrEmpty(account.Username))
                        LogMessage("Connecting and logging in to the web server....", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                    else
                        LogMessage("Connecting to the server....", (Brush)new BrushConverter().ConvertFrom("#28d659"));

                    if (login.HasProxy && login.ProxyPort > 0)
                    {
                        account.Socks.Version = (SocksVersion)login.ProxyVersion;
                        account.Socks.Host = login.ProxyHost;
                        account.Socks.Port = login.ProxyPort;
                        account.Socks.Username = login.ProxyUsername;
                        account.Socks.Password = login.ProxyPassword;
                    }
                    if (login.HasHttpProxy && login.HttpProxyPort > 0)
                    {
                        account.HttpProxy.Host = login.HttpProxyHost;
                        account.HttpProxy.Port = login.HttpProxyPort;
                    }
                    Bot.Login(account, login.SaveIdAndHashPassword);
                }
            }
            catch (Exception e)
            {
                LoginButton.IsEnabled = true;
                LoginButton2.Content = "Login";
                LoginButton2.IsEnabled = true;
                Msg("Error", "", e);
            }
        }
        private void Bot_StateChanged(BotClient.State state)
        {
            Dispatcher.InvokeAsync(delegate
            {
                string stateText;
                if (state == BotClient.State.Started)
                {
                    stateText = "started";
                    StartScriptButtonIcon.Kind = PackIconKind.Pause;
                }
                else if (state == BotClient.State.Paused)
                {
                    stateText = "paused";
                    StartScriptButtonIcon.Kind = PackIconKind.Play;
                }
                else
                {
                    stateText = "stopped";
                    StartScriptButtonIcon.Kind = PackIconKind.Play;
                }
                if (stateText == "started")
                { LogMessage("Bot " + stateText, (Brush)new BrushConverter().ConvertFrom("#28d659")); }
                else
                { LogMessage("Bot " + stateText, Brushes.OrangeRed); }
            });
        }

        private void Bot_LogMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message);
            });
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool shouldLogin = false;
            lock (Bot)
            {
                if (Bot.Game == null || !Bot.Game.IsConnected)
                {
                    shouldLogin = true;
                }
                else
                {
                    Logout();
                }
            }
            if (shouldLogin)
            {
                OpenLoginWindow();
            }
        }
        private void Logout()
        {
            LogMessage("Logging out...", Brushes.OrangeRed);
            lock (Bot)
            {
                Bot.LogoutApi(false);
            }
        }
        private void AddView(UserControl view, ContentControl content, ListViewItem button, bool visible = false)
        {
            _views.Add(new TabView
            {
                Content = content,
                Button = button
            });
            content.Content = view;
            if (visible)
            {
                content.Visibility = Visibility.Visible;
            }
            else
            {
                content.Visibility = Visibility.Collapsed;
            }
            button.Selected += ViewButton_Click;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var view in _views)
            {
                if (Equals(view.Button, sender))
                {
                    view.Content.Visibility = Visibility.Visible;
                    _refreshPlayersDelay = Equals(view.Content, PlayersContent) ? 200 : 5000;
                }
                else
                {
                    view.Content.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void Bot_ClientChanged()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.LoggedIn += Client_LoggedIn;
                    Bot.Game.LoggingError += Bot_LoggingFailed;
                    Bot.Game.PlayerDataUpdated += Game_PlayerDataUpdated;
                    Bot.Game.InventoryUpdated += Client_InventoryUpdated;
                    Bot.Game.MapUpdated += Game_MapUpdated;
                    Bot.Game.TeamUpdated += Client_PokemonsUpdated;
                    Bot.Game.BattleStarted += Client_BattleStarted;
                    Bot.Game.BattleEnded += Client_BattleEnded;
                    Bot.Game.PlayerPositionUpdated += Game_PlayerPositionUpdated;
                    Bot.Game.BattleMessage += Client_BattleMessage;
                    Bot.Game.EnemyUpdated += Battle_EnemyUpdated;
                    Bot.Game.SuccessfullyAuthenticated += Client_SuccessfullyAuthenticated;
                    Bot.Game.AuthenticationFailed += Client_AuthenticationFailed;
                    Bot.Game.ChatMessage += ChatCommandsView.ChatMessage_Receieved;
                    Bot.Game.PrivateChat += ChatCommandsView.ProcessPrivateMessages;
                    Bot.Game.ShopOpened += Client_ShopOpened;
                    Bot.Game.SystemMessage += Client_SystemMessage;
                    Bot.Game.PlayerAdded += Client_PlayerAdded;
                    Bot.Game.PlayerUpdated += Client_PlayerUpdated;
                    Bot.Game.PlayerRemoved += Client_PlayerRemoved;
                }
            }
            Dispatcher.InvokeAsync(delegate
            {
                if (Bot.Game != null)
                {
                    FileLog.OpenFile(Bot.Account.Name);
                }
                else
                {
                    FileLog.CloseFile();
                }
            });
        }

        private void Client_SystemMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage($"[System] {message}", (Brush)new BrushConverter().ConvertFrom("#28d659"));
            });
        }

        private void Client_AuthenticationFailed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(
                    "Something went wrong while trying to log in to the server. " +
                    "Make sure you have been logged out from the web browser or website and re-check your username and password. " +
                    "Or may be it is your connection problem like slow Internet or proxy is slow.", Brushes.OrangeRed);
            });
        }

        private void Client_SuccessfullyAuthenticated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Successfully authenticated.",
                    (Brush)new BrushConverter().ConvertFrom("#28d659"));
            });
        }

        private void OnWebSuccessfullyLoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Successfully connected and logged in to the web server.",
                    (Brush)new BrushConverter().ConvertFrom("#28d659"));
                LogMessage("Connecting to the game server....", (Brush)new BrushConverter().ConvertFrom("#28d659"));
            });
        }

        private void Client_InventoryUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<InventoryItem> items;
                lock (Bot)
                {
                    items = Bot.Game.Items.ToArray();
                }
                Inventory.ItemsListView.ItemsSource = items;
                Inventory.ItemsListView.Items.Refresh();
                InventoryView.UpdateColumnWidths((GridView)Inventory.ItemsListView.View);
            });
        }

        private void Client_ShopOpened(Shop shop)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        var content = new StringBuilder();
                        content.Append("Shop opened:");
                        foreach (var item in shop.ShopItems)
                        {
                            content.AppendLine();
                            content.Append(item.Name);
                            content.Append($" (${ item.Price })");
                        }

                        LogMessage(content.ToString(), Brushes.White);
                    }
                }
            });
        }

        private void Client_BattleStarted()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "In Battle";
                StatusText.Foreground = Brushes.DodgerBlue;
            });
        }

        private void Client_BattleEnded(int battleStatus)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        var hasWon = (battleStatus & 0b100) > 0;
                        var hasLost = (battleStatus & 0b010) > 0;
                        var hasEnded = (battleStatus & 0b001) > 0;
                        if (hasEnded)
                            LogMessage("The battle has ended.", Brushes.Aqua);
                        else if (hasLost)
                            LogMessage("You've lost the battle!", Brushes.Aqua);
                        else if (hasWon)
                            LogMessage("You've won the battle!", Brushes.Aqua);

                        StatusText.Text = "Online";
                        StatusText.Foreground = (Brush) new BrushConverter().ConvertFrom("#28d659");
                    }
                }
            });
        }

        private void Battle_EnemyUpdated(IList<WildPokemon> opponents, int selectedPokemon)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (opponents is null || opponents.Count > 0 is false || Bot.Game is null) return;
                    BattleStat.EnemiesListView.ItemsSource = opponents;
                    BattleStat.EnemiesListView.Items.Refresh();
                    BattleStatView.UpdateColumnWidths((GridView)BattleStat.EnemiesListView.View);
                }
            });
        }

        private void Client_PlayerAdded(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerUpdated(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerRemoved(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_BattleMessage(string obj)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (obj != "")
                        LogMessage(FirstCharToUpper(obj), Brushes.Aqua);
                }
            });
        }

        private void Client_PokemonsUpdated(bool isSorted)
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        if (isSorted)
                        {
                            Bot.Game.Team = Bot.Game.Team.OrderBy(pk => pk.Uid).ToList();
                            var pokemons = Bot.Game.Team.ToArray();
                            Team.PokemonsListView.ItemsSource = pokemons;
                            Team.PokemonsListView.Items.Refresh();
                        }
                        else
                        {
                            IList<Pokemon> team;
                            for (var i = 0; i < Bot.Game.Team.Count; i++)
                            {
                                Bot.Game.Team[i].Uid = i + 1;
                            }

                            Bot.Game.Team = Bot.Game.Team.OrderBy(p => p.Uid).ToList();
                            team = Bot.Game.Team.ToArray();
                            Team.PokemonsListView.ItemsSource = team;
                            Team.PokemonsListView.Items.Refresh();
                        }
                        TeamView.UpdateColumnWidths((GridView)Team.PokemonsListView.View);

                    }
                }
            });
        }

        private void Game_PlayerPositionUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null && Bot.Game.IsMapLoaded)
                    {
                        MapNameText.Text = Bot.Game.MapName;
                        PlayerPositionText.Text = $@"({Bot.Game.PlayerX}, {Bot.Game.PlayerY})";
                    }
                }
            });
        }

        private void Game_MapUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null && Bot.Game.IsMapLoaded)
                    {
                        MapNameText.Text = Bot.Game.MapName;
                        PlayerPositionText.Text = $@"({Bot.Game.PlayerX}, {Bot.Game.PlayerY})";
                    }
                }
            });
        }

        private void Game_PlayerDataUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        MoneyText.Text = $@"{Bot.Game.Money}$";
                        CreditText.Text = $@"{Bot.Game.Credits}";
                        MoneyTooltip.Text = $@"Money: {Bot.Game.Money}$";
                        Money1Tooltip.Text = MoneyTooltip.Text;
                    }
                }
            });
            
        }

        private void LoadScriptButton_OnClick(object sender, RoutedEventArgs e)
        {
            LoadScript();
        }

        private void LoadScript(string filePath = null)
        {
            if (filePath == null)
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = App.Name + " Scripts|*.lua;*.txt|All Files|*.*"
                };
                var result = openDialog.ShowDialog();

                if (!(result.HasValue && result.Value))
                    return;

                filePath = openDialog.FileName;
            }

            try
            {
                Bot.Settings.LastScript = filePath;
                ReloadPopup.Content = "Reload " + Path.GetFileName(filePath) + "\tCtrl+R";
                ReloadPopup.IsEnabled = true;
                Bot.LoadScript(filePath);
                Dispatcher.InvokeAsync(() =>
                {
                    LogMessage("Script \"{0}\" by \"{1}\" successfully loaded", Bot.Script.Name, Bot.Script.Author);
                    if (Bot.Script != null)
                    {
                        Bot.Script.FlashBotWindow += FlashWindow;
                    }
                });
            }
            catch (Exception ex)
            {
                var filename = Path.GetFileName(filePath);
#if DEBUG
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex);
#else
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex.Message);
#endif
            }
        }

        private void FlashWindow()
        {
            var window = GetWindow(this);
            var handle = new WindowInteropHelper(window ?? throw new InvalidOperationException()).Handle;
            FlashWindowHelper.Flash(handle);
        }

        private void MainTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                new AboutWindow { Owner = this }.ShowDialog();
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                if (Bot.Running == BotClient.State.Stopped)
                {
                    Bot.Start();
                }
                else if (Bot.Running == BotClient.State.Started || Bot.Running == BotClient.State.Paused)
                {
                    Bot.Pause();
                }
            }
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }
        private static void Msg(string title = "", string message = "", Exception ex = null, MessageBoxImage icon = MessageBoxImage.None)
        {
            if (ex is null is false)
            {
                if (ex != null) MessageBox.Show(ex.Message, @"Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
        }

        private void ReloadHotKey_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Bot.Settings.LastScript))
            {
                LoadScript(Bot.Settings.LastScript);
            }
        }

        private void ReLoadScriptButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Bot.Settings.LastScript))
            {
                LoadScript(Bot.Settings.LastScript);
            }
        }

        private void ButtonOpenMenu_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonCloseMenu.Visibility = Visibility.Visible;
            ButtonOpenMenu.Visibility = Visibility.Collapsed;
            LoginButton.Visibility = Visibility.Visible;
            PopupBox.Visibility = Visibility.Collapsed;
            LoadScriptButton.Visibility = Visibility.Visible;
            ReLoadScriptButton.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
            MessageTextBox.CaretPosition = MessageTextBox.Document.ContentEnd;
            MessageTextBox.ScrollToEnd();
        }

        private void ButtonCloseMenu_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonCloseMenu.Visibility = Visibility.Collapsed;
            ButtonOpenMenu.Visibility = Visibility.Visible;
            LoginButton.Visibility = Visibility.Collapsed;
            PopupBox.Visibility = Visibility.Visible;
            LoadScriptButton.Visibility = Visibility.Collapsed;
            ReLoadScriptButton.Visibility = Visibility.Collapsed;
            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Collapsed;
            MessageTextBox.CaretPosition = MessageTextBox.Document.ContentEnd;
            MessageTextBox.ScrollToEnd();
        }
        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                var file = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (file != null)
                {
                    LoadScript(file[0]);
                }
            }
        }

        private void AboutButton_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow{Owner = this}.ShowDialog();
        }
    }
}
