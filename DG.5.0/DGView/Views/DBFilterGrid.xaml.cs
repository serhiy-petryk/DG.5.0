using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DGCore.Filters;
using DGView.Helpers;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for FilterGrid.xaml
    /// </summary>
    public partial class DBFilterGrid : UserControl, INotifyPropertyChanged
    {
        public DGCore.Filters.FilterList FilterList { get; private set; }

        private ICollection _dataSource;

        public DBFilterGrid()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Bind(DGCore.Filters.FilterList filterList, ICollection dataSource)
        {
            FilterList = filterList;
            _dataSource = dataSource;
            RefreshUI();
        }

        #region =========  Event handlers  ===========
        private void OnFilterEditPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var cell = (DataGridCell)sender;
            var filterLine = cell.DataContext as DGCore.Filters.FilterLineBase;
            if (!(bool)CanConvertStringTo.Instance.Convert(filterLine.PropertyType, null, null, null))
                return;

            var view = new FilterLineView(filterLine);
            var container = this.GetVisualParents().OfType<MwiContainer>().FirstOrDefault();
            var geometry = (Geometry)Application.Current.Resources["FilterGeometry"];
            var transforms = WpfSpLib.Helpers.ControlHelper.GetActualLayoutTransforms(container);
            var height = Math.Max(200, Window.GetWindow(this).ActualHeight * 2 / 3 / transforms.Value.M22);
            Helpers.Misc.OpenMwiDialog(container, view, "Filter Setup", geometry, (child, adorner) =>
            {
                child.Height = height;
                child.Theme = container?.ActualTheme;
                child.ThemeColor = container?.ActualThemeColor;
            });
            RefreshUI();
        }
        #endregion

        #region ============  INotifyPropertyChanged  ============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void RefreshUI()
        {
            OnPropertiesChanged(nameof(FilterList));
            foreach (var o in FilterList)
                o.OnPropertiesChanged(nameof(FilterLineBase.FilterTextOrDescription), nameof(FilterLineBase.HasFilter), nameof(FilterLineBase.Error));
        }
        #endregion
    }
}
