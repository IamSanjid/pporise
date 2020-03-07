using PPOBot;
using PPOProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PPORise
{
    /// <summary>
    /// Interaction logic for PlayersView.xaml
    /// </summary>
    public partial class PlayersView : UserControl
    {
        private GridViewColumnHeader _lastColumn;
        private ListSortDirection _lastDirection;

        private BotClient _bot;
        private MainWindow MainWindow { get; }

        public PlayersView(BotClient bot, MainWindow mWin)
        {
            InitializeComponent();

            _bot = bot;
            MainWindow = mWin;
        }

        class PlayerInfosView
        {
            public int Distance { get; set; }
            public string Name { get; set; }
            public string Position { get; set; }
            //public string Status { get; set; }
            public string Follower { get; set; }
            //public string Guild { get; set; }
            public string LastSeen { get; set; }
        }

        public void RefreshView()
        {
            lock (_bot)
            {
                if (_bot.Game != null && _bot.Game.IsMapLoaded && _bot.Game.Players != null)
                {
                    IEnumerable<PlayerInfos> playersList = _bot.Game.Players.Values.OrderBy(e => e.Added);
                    List<PlayerInfosView> listToDisplay = new List<PlayerInfosView>();
                    foreach (PlayerInfos player in playersList)
                    {
                        string petName = "";
                        if (PokemonNamesManager.Instance.Names.Length > player.PokemonPetId)
                        {
                            petName = PokemonNamesManager.Instance.Names[player.PokemonPetId];
                            if (player.IsPokemonPetShiny)
                            {
                                petName = "(s)" + petName;
                            }
                        }
                        listToDisplay.Add(new PlayerInfosView
                        {
                            Distance = _bot.Game.DistanceTo(player.PosX, player.PosY),
                            Name = player.Name,
                            Position = "(" + player.PosX + ", " + player.PosY + ")",
                            //Status = player.IsAfk ? "AFK" : (player.IsInBattle ? "BATTLE" : ""),
                            Follower = petName,
                            //Guild = player.GuildId.ToString(),
                            LastSeen = (DateTime.UtcNow - player.Updated).Seconds.ToString() + "s"
                        });
                    }
                    int selected = PlayerListView.SelectedIndex;
                    PlayerListView.ItemsSource = listToDisplay;
                    PlayerListView.Items.Refresh();
                    PlayerListView.SelectedIndex = selected;
                    
                    if (PlayerListView?.View is GridView gridView)
                    {
                        // ... and update its column widths
                        UpdateColumnWidths(gridView);
                    }
                }
            }
        }

        public static void UpdateColumnWidths(GridView gridView)
        {
            // For each column...
            foreach (var column in gridView.Columns)
            {
                // If this is an "auto width" column...
                if (double.IsNaN(column.Width))
                {
                    // Set its Width back to NaN to auto-size again
                    column.Width = 0;
                    column.Width = double.NaN;
                }
            }
        }

        public void ClearList()
        {
            PlayerListView.ItemsSource = null;
        }

        private void GridViewHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);

            ListSortDirection direction = ListSortDirection.Ascending;
            if (column == _lastColumn && direction == _lastDirection)
            {
                direction = ListSortDirection.Descending;
            }

            PlayerListView.Items.SortDescriptions.Clear();
            PlayerListView.Items.SortDescriptions.Add(new SortDescription((string)column.Content, direction));

            _lastColumn = column;
            _lastDirection = direction;
            if (PlayerListView?.View is GridView gridView)
            {
                // ... and update its column widths
                UpdateColumnWidths(gridView);
            }
        }

        private void PlayerListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    ListViewItem listViewItem =
                        FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                    var view = FindAnchestor<ScrollBar>((DependencyObject)e.OriginalSource);
                    var gridView = FindAnchestor<GridViewColumnHeader>((DependencyObject)e.OriginalSource);
                    if (listViewItem is null && view is null && gridView is null)
                        MainWindow.DragMove();
                }
            });
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

    }
}
