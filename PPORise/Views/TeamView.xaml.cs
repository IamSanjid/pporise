using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PPOBot;
using PPOProtocol;
// ReSharper disable once CheckNamespace
namespace PPORise
{
    // ReSharper disable once RedundantExtendsListEntry
    public partial class TeamView : UserControl
    {
        private readonly BotClient _bot;
        private Point _startPoint;
        private Pokemon _selectedPokemon;
        public MainWindow MainWindow { get; set; }
        public TeamView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Dispatcher.InvokeAsync(delegate
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Get the dragged ListViewItem
                    ListViewItem listViewItem =
                        FindAnchestor<ListViewItem>((DependencyObject) e.OriginalSource);

                    if (listViewItem != null)
                    {
                        // Find the data behind the ListViewItem
                        Pokemon pokemon = (Pokemon)PokemonsListView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                        // Initialize the drag & drop operation
                        DataObject dragData = new DataObject("Pokemon", pokemon ?? throw new InvalidOperationException());
                        DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                    }
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
        private void List_Drop(object sender, DragEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (e.Data.GetDataPresent("Pokemon"))
                {
                    var sourcePokemon = (Pokemon)e.Data.GetData("Pokemon");

                    var listViewItem =
                        FindAnchestor<ListViewItem>((DependencyObject) e.OriginalSource);

                    if (listViewItem != null)
                    {
                        // Find the data behind the ListViewItem
                        var destinationPokemon =
                            (Pokemon) PokemonsListView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                        lock (_bot)
                        {
                            if (_bot.Game != null)
                            {
                                _bot.Game.SwapPokemon(sourcePokemon.Uid, destinationPokemon.Uid);
                            }
                        }
                    }
                }
            });
        }

        private void List_DragEnter(object sender, DragEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (!e.Data.GetDataPresent("Pokemon") || sender == e.Source)
                {
                    e.Effects = DragDropEffects.None;
                }
            });
        }
        private void List_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (PokemonsListView.SelectedItems.Count > 0)
                {
                    _selectedPokemon = (Pokemon)PokemonsListView.SelectedItem;
                }
                else
                {
                    _selectedPokemon = null;
                }
            });
        }
        private void List_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_selectedPokemon is null) return;
            Dispatcher.InvokeAsync(delegate
            {
                Pokemon pokemon = _selectedPokemon;
                lock (_bot)
                {
                    if (_bot.Game is null) return;
                    if (_bot.Game.IsConnected is false) return; // I love to use 'is' keyword :D
                    ContextMenu contextMenu = new ContextMenu();
                    if (!string.IsNullOrEmpty(pokemon.ItemHeld))
                    {
                        MenuItem takeItem = new MenuItem();
                        takeItem.Header = "Take " + pokemon.ItemHeld;
                        takeItem.Click += MenuItemTakeItem_Click;
                        contextMenu.Items.Add(takeItem);
                    }

                    if (_bot.Game.Items.Count > 0)
                    {
                        MenuItem giveItem = new MenuItem();
                        MenuItem useItem = new MenuItem();
                        giveItem.Header = "Give item";
                        useItem.Header = "Use item";
                        _bot.Game.Items
                            .Where(i => i.IsEquipAble())
                            .OrderBy(i => i.Name)
                            .ToList()
                            .ForEach(i => giveItem.Items.Add(i.Name));
                        _bot.Game.Items
                            .Where(i => !i.IsEquipAble())
                            .OrderBy(i => i.Name)
                            .ToList()
                            .ForEach(i => useItem.Items.Add(i.Name));

                        useItem.Click += UseItem_Click;
                        giveItem.Click += MenuItemGiveItem_Click;
                        if (_bot.Game.Items
                                .Where(i => i.IsEquipAble())
                                .OrderBy(i => i.Name)
                                .ToList().Count > 0)
                        {
                            contextMenu.Items.Add(giveItem);
                        }

                        contextMenu.Items.Add(useItem);
                    }

                    PokemonsListView.ContextMenu = contextMenu;
                }
            });
        }
        private void MenuItemGiveItem_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (PokemonsListView.SelectedItems.Count == 0)
                    return;

                Pokemon pokemon = (Pokemon) PokemonsListView.SelectedItems[0];
                string itemName = ((MenuItem) e.OriginalSource).Header.ToString();
                lock (_bot)
                {
                    InventoryItem item = _bot.Game.Items.Find(i => i.Name == itemName);
                    if (item != null)
                        _bot.Game.GiveItemToPokemon(pokemon.Uid, item.Uid);
                }
            });
        }

        private void MenuItemTakeItem_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate {
                if (PokemonsListView.SelectedItems.Count == 0)
                    return;

                var pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
                lock (_bot)
                {
                    _bot.Game.TakeItemFromPokemon(pokemon.Uid);
                }
            });
        }
        private void UseItem_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate {
                if (PokemonsListView.SelectedItems.Count == 0)
                    return;

                var pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
                var itemName = ((MenuItem)e.OriginalSource).Header.ToString();
                lock (_bot)
                {
                    var item = _bot.Game.Items.Find(i => i.Name == itemName);
                    if (item != null)
                        _bot.Game.UseItem(item.Name, pokemon.Uid);
                }
            });
        }
        private void ItemHeader_OnClick(object sender, RoutedEventArgs e)
        {
            PokemonsListView.Items.Refresh();
            UpdateColumnWidths(PokemonsListView.View as GridView);
        }
        // Technique for updating column widths of a ListView's GridView manually
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
        // Handler for the ListView's TargetUpdated event
        private void ListViewTargetUpdated(object sender, DataTransferEventArgs e)
        {
            // Get a reference to the ListView's GridView...
            var listView = sender as ListView;
            if (listView?.View is GridView gridView)
            {
                // ... and update its column widths
                UpdateColumnWidths(gridView);
            }
        }
        private void PokemonsListView_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            var listViewItem =
                FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
            var gridHeader = FindAnchestor<GridViewColumnHeader>((DependencyObject) e.OriginalSource);
            var view = FindAnchestor<ScrollBar>((DependencyObject)e.OriginalSource);
            if (listViewItem is null && view is null && gridHeader is null)
                MainWindow.DragMove();
        }
    }
}
