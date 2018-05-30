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
using PPOBot;
using PPOProtocol;

namespace PPORise.Views
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private readonly BotClient _bot;
        private InventoryItem _selectedItem;
        public MainWindow MainWindow { get; set; }
        public InventoryView(BotClient bot)
        {
            _bot = bot;
            InitializeComponent();
        }
        private void UseItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem is null) return;
            lock (_bot)
            {
                if (_selectedItem.Name.Contains("TM") || _selectedItem.Name.Contains("HM"))
                    _bot.C_LogMessage("Cant's use TM from Inventory view. Try to use it from Teamview.", Brushes.OrangeRed);
                else
                {
                    if (!_selectedItem.IsEquipAble())
                        _bot.Game.UseItem(_selectedItem.Name);
                    else
                        _bot.C_LogMessage("This item can't be used like this way.", Brushes.OrangeRed);
                }
            }
        }
        private void ItemsListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            lock (_bot)
            {
                if (ItemsListView.SelectedItems.Count > 0)
                    _selectedItem = (InventoryItem)ItemsListView.SelectedItems[0];
                else
                    _selectedItem = null;

                if (_bot.Game is null) return;
                if (_selectedItem is null) return;

                var useItem = new MenuItem {Header = "Use Item"};
                var contextMenu = new ContextMenu();

                useItem.Click += UseItem_Click;
                contextMenu.Items.Add(useItem);
                ItemsListView.ContextMenu = contextMenu;
            }
        }

        private void ItemsListView_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
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

        private void ItemHeader_OnClick(object sender, RoutedEventArgs e)
        {
            ItemsListView.Items.Refresh();
            UpdateColumnWidths(ItemsListView.View as GridView);
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
    }
}
