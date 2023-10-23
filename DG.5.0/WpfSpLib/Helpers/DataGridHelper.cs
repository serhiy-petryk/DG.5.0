using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfSpLib.Helpers
{
    public static class DataGridHelper
    {
        public static void DataGrid_OnSorting(DataGrid dataGrid, DataGridSortingEventArgs e)
        {
            if (e.Column.SortDirection == ListSortDirection.Descending)
            {
                var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                var sd = view.SortDescriptions.OfType<SortDescription>().FirstOrDefault(d => d.PropertyName == e.Column.SortMemberPath);
                view.SortDescriptions.Remove(sd);
                e.Column.SortDirection = null;
                e.Handled = true;
            }
        }
    }
}
