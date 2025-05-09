// ToDo:
// +1. Clone values
// 2. Copy & paste for grid
// - (difficult to remove event handlers after disposed and/or get access to mwiChild in FilterLineView) 3. MwiChild + Disposable => close
// 4. Button => monochrome or flat style

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DGCore.Filters;
using DGView.Helpers;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for FilterLineView.xaml
    /// </summary>
    public partial class FilterLineView : UserControl
    {
        #region ========  Static section =============
        internal static void OnFilterEditPreviewMouseDown(DataGridCell cell)
        {
            // var cell = (DataGridCell)sender;
            var filterLine = cell.DataContext as FilterLineBase;
            if (!(bool)CanConvertStringTo.Instance.Convert(filterLine.PropertyType, null, null, null))
                return;

            var view = new FilterLineView(filterLine);
            var container = cell.GetVisualParents<MwiContainer>().FirstOrDefault();
            var geometry = (Geometry)Application.Current.Resources["FilterGeometry"];
            var transforms = WpfSpLib.Helpers.ControlHelper.GetActualLayoutTransforms(container);
            var height = Math.Max(200, Window.GetWindow(cell).ActualHeight * 2 / 3 / transforms.Value.M22);
            Misc.OpenMwiDialog(container, view, geometry, (child, adorner) =>
            {
                child.Height = height;
                child.Theme = container?.ActualTheme;
                child.ThemeColor = container?.ActualThemeColor;
                child.SetResourceReference(MwiChild.TitleProperty, "Loc:FilterLineView.Title");
                var parentDataGrid = cell.GetVisualParents<DataGrid>().FirstOrDefault();
                var b = new Binding { Path = new PropertyPath("Background"), Source = parentDataGrid, Converter = ColorHslBrush.Instance, ConverterParameter = "+10%:+0%" };
                child.SetBinding(MwiChild.ThemeColorProperty, b);
            });
        }
        #endregion

        public FilterLineBase FilterLine { get; }
        public FilterLineSubitemCollection Clone_FilterLines { get; }
        public bool Clone_Not { get; set; }

        private MwiChild ParentWindow => this.GetVisualParents<MwiChild>().FirstOrDefault();

        public FilterLineView(FilterLineBase filterLine)
        {
            InitializeComponent();
            DataContext = this;
            FilterLine = filterLine;
            Clone_FilterLines = (FilterLineSubitemCollection)filterLine.Items.Clone();
            Clone_Not = FilterLine.Not;
        }

        #region ==========  Event handlers  ==========
        private void DataGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // To prevent error: ''DeferRefresh' is not allowed during an AddNew or EditItem transaction.'
            ((DataGrid)sender).CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterLine.Items.Clear();
            foreach (var item in Clone_FilterLines.Where(a => a.IsValid))
            // foreach (var item in Clone_FilterLines)
                FilterLine.Items.Add(item);
            FilterLine.Not = Clone_Not;
            CloseButton_OnClick(sender, e);
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clone_FilterLines.Clear();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterLine.OnPropertiesChanged(nameof(FilterLineBase.FilterTextOrDescription), nameof(FilterLineBase.HasFilter));
            ParentWindow?.CmdClose.Execute(null);
        }
        #endregion

        private void DataGrid_OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e) =>
            DGHelper.DataGrid_OnRowEditEnding((DataGrid)sender, e);

        private void DataGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = (DataGrid)sender;

            var inputHitTest =
                dataGrid.InputHitTest(e.GetPosition((DataGrid)sender)) as DependencyObject;

            if (inputHitTest != null)
            {
                var dataGridCell = inputHitTest as DataGridCell;
                if (dataGridCell != null &&
                    dataGrid.Equals(dataGridCell.GetVisualParents<DataGrid>().FirstOrDefault()))
                {
                    if (dataGridCell.IsReadOnly) return;
                    ComboBox comboBox;
                    if (IsDirectHitOnEditComponent(dataGridCell, e, out comboBox))
                    {
//                        if (_suppressComboAutoDropDown != null) return;

                        dataGrid.CurrentCell = new DataGridCellInfo(dataGridCell);
                        dataGrid.BeginEdit();
                        //check again, as we move to  the edit  template
                        if (IsDirectHitOnEditComponent(dataGridCell, e, out comboBox))
                        {
                            //_suppressComboAutoDropDown = dataGrid;
                            //comboBox.DropDownClosed += ComboBoxOnDropDownClosed;
                            comboBox.IsDropDownOpen = true;
                        }

                        e.Handled = true;
                    }
                }
            }
        }

        private static bool IsDirectHitOnEditComponent<TControl>(ContentControl contentControl, MouseEventArgs mouseButtonEventArgs, out TControl control)
            where TControl : Control
        {
            control = contentControl.Content as TControl;
            if (control == null) return false;

            var frameworkElement = VisualTreeHelper.GetChild(contentControl, 0) as FrameworkElement;
            if (frameworkElement == null) return false;

            var transformToAncestor = (MatrixTransform)control.TransformToAncestor(frameworkElement);
            var rect = new Rect(
                new Point(transformToAncestor.Value.OffsetX, transformToAncestor.Value.OffsetY),
                new Size(control.ActualWidth, control.ActualHeight));

            return rect.Contains(mouseButtonEventArgs.GetPosition(frameworkElement));
        }

        private bool flag = false;
        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is DataGridCell cell1)
            {
                Debug.Print($"DataGridFocus: {e.OriginalSource.GetType().Name} {cell1.Column.Header} {flag} {DateTime.Now.TimeOfDay}");
            }
            else
                Debug.Print($"DataGridFocus: {e.OriginalSource.GetType().Name} {flag} {DateTime.Now.TimeOfDay}");
            //return;*/

            if (flag) return;

            if (e.OriginalSource is DataGridRowHeader rowHeader)
            {
                flag = true;
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.Dispatcher.BeginInvoke(new Action(() => {
                    flag = false;
                }), DispatcherPriority.SystemIdle);
                e.Handled = true;
            }

            // return;
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource is DataGridCell cell)
            {
                flag = true;
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.Dispatcher.BeginInvoke(new Action(() => {
                    Debug.Print($"DataGridCell_Selected: {DateTime.Now.TimeOfDay}");
                    var a1 = cell.GetVisualChildren<ComboBox>().ToArray();
                    grd.BeginEdit(e);
                    /*if (a1.Length > 0)
                    {
                        grd.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            a1[0].IsDropDownOpen = true;
                        }), DispatcherPriority.ApplicationIdle);

                    }*/
                    flag = false;
                }), DispatcherPriority.SystemIdle);

                // e.Handled = true;
            }
        }

        private void DataGrid_CellGotFocus(object sender, RoutedEventArgs e)
        {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);

                Control control = GetFirstChildByType<Control>(e.OriginalSource as DataGridCell);
                if (control != null)
                {
                    control.Focus();
                }
            }
        }

        private T GetFirstChildByType<T>(DependencyObject prop) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild((prop), i) as DependencyObject;
                if (child == null)
                    continue;

                T castedProp = child as T;
                if (castedProp != null)
                    return castedProp;

                castedProp = GetFirstChildByType<T>(child);

                if (castedProp != null)
                    return castedProp;
            }
            return null;
        }
    }
}
