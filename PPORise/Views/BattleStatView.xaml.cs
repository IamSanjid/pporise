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
    /// Interaction logic for BattleStatView.xaml
    /// </summary>
    public partial class BattleStatView : UserControl
    {
        public MainWindow MainWindow { get; }

        public BattleStatView(MainWindow mWin)
        {
            InitializeComponent();
            MainWindow = mWin;
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T) current;
                }

                current = VisualTreeHelper.GetParent(current);
            } while (current != null);

            return null;
        }
        private void ItemHeader_OnClick(object sender, RoutedEventArgs e)
        {
            EnemiesListView.Items.Refresh();
            UpdateColumnWidths(EnemiesListView.View as GridView);
        }
        private void EnemiesListView_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var listViewItem =
                        FindAnchestor<ListViewItem>((DependencyObject) e.OriginalSource);
                    var view = FindAnchestor<ScrollBar>((DependencyObject) e.OriginalSource);
                    var gridView = FindAnchestor<GridViewColumnHeader>((DependencyObject)e.OriginalSource);
                    if (listViewItem is null && view is null && gridView is null)
                        MainWindow.DragMove();
                }
            });
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
