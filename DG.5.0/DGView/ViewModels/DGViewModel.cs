using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DGCore.DGVList;
using DGCore.Helpers;
using DGCore.Sql;
using DGCore.UserSettings;
using DGView.Helpers;
using DGView.Views;
using WpfSpLib.Common;
using WpfSpLib.Controls;

namespace DGView.ViewModels
{
    public partial class DGViewModel: Component, INotifyPropertyChanged, IUserSettingSupport<DGV>
    {
        #region ==========  Static section  ===================
        public static DataGridView CreateDataGrid(MwiContainer host, string title)
        {
            var dgView = new DataGridView();
            var child = new MwiChild
            {
                Title = title,
                Content = dgView,
                Height = Math.Max(200.0, Window.GetWindow(host).ActualHeight * 2 / 3),
                MaxWidth = Math.Max(200.0, Window.GetWindow(host).ActualWidth * 2 / 3)
            };
            var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 3) };
            timer.Tick += OnDispatcherTimerTick;
            timer.Start();

            // fixed bug 91 at 2025-03-15: Check color converter: +50%:50:50
            // var b = new Binding { Path = new PropertyPath("ActualThemeColor"), Source = host, Converter = ColorHslBrush.Instance, ConverterParameter = "+45%:+0%:+0%" };
            var b = new Binding { Path = new PropertyPath("ActualThemeColor"), Source = host, Converter = ColorHslBrush.Instance, ConverterParameter = "+25%:+0%:+0%" };
            child.SetBinding(MwiChild.ThemeColorProperty, b);

            host.Children.Add(child);

            void OnDispatcherTimerTick(object sender, EventArgs e)
            {
                var timer2 = (DispatcherTimer)sender;
                timer2.Stop();
                timer2.Tick -= OnDispatcherTimerTick;
                child.MaxWidth = 3000;
            }

            return dgView;
        }

        #endregion
        public DataGrid DGControl { get; }
        public IDGVList Data { get; private set; }
        public PropertyDescriptorCollection Properties => Data.Properties;

        public DGViewModel(DataGrid dataGrid)
        {
            DGControl = dataGrid;
            InitCommands();
        }

        public void Bind(DataSourceBase ds, string layoutID, string startUpParameters, string startUpLayoutName, DGV settings)
        {
            LayoutId = layoutID;
            DGCore.Misc.DependentObjectManager.Bind(ds, this);

            Task.Factory.StartNew(() =>
            {
                StartUpParameters = startUpParameters;
                LastAppliedLayoutName = startUpLayoutName;

                // Not need! DGCore.Misc.DependentObjectManager.Bind(ds, this); // Register object    
                var listType = typeof(DGVList<>).MakeGenericType(ds.ItemType);
                var dataSource = (IDGVList)Activator.CreateInstance(listType, ds, (Func<DGColumnHelper[]>)GetAllValidColumnHelpers);
                Data = dataSource;
                /* Different scenarios for ResetBinding
                 Data.FnResetBinding = () =>
                {
                    DGControl.UpdateAllBindings();
                    // DGControl.Items.Refresh();
                };*/
                Unwire();
                Wire();

                Task.Factory.StartNew(() => Data.UnderlyingData.GetData(false));

                DGControl.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    DGControl.ItemsSource = (IEnumerable)Data;
                    GenerateColumns();
                    if (settings != null)
                        ((IUserSettingSupport<DGV>) this).ApplySetting(settings);
                    else
                        UserSettingsUtils.Init(this, startUpLayoutName);
                
                    // Apply saved in database column settings for format
                    GenerateColumns();
                }));
            });
        }

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region ===========  IComponent  ==============
        protected override void Dispose(bool disposing)
        {
            Unwire();
            Data.UnderlyingData.DataLoadingCancelFlag = true;
            _dataRecordsTimer.Stop();
            Data.Dispose(); // DGVList

            base.Dispose(disposing);

            Data = null;

            _findTextView?.Dispose();
            _findTextView = null;
            _lastCurrentCellInfo = new DataGridCellInfo();
            GroupItemCountColumn = null;
            _groupColumns.Clear();
            _columns.Clear();

            // Keyboard.ClearFocus();
        }
        #endregion

        #region =========  DataStateChanged  ============
        private void Wire()
        {
          Data.DataStateChanged += DataSource_DataStateChanged;
          _dataRecordsTimer.Tick += OnDataRecordsTimerTick;
        }

        private void Unwire()
        {
            if (Data != null)
                Data.DataStateChanged -= DataSource_DataStateChanged;
            _dataRecordsTimer.Tick -= OnDataRecordsTimerTick;
        }

        private Stopwatch _dataLoadedTimer;
        private readonly DispatcherTimer _dataRecordsTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(250)};
        private int? _dataLoadedTime;
        private int? _dataNavigationTime;

        private void DataSource_DataStateChanged(object sender, DataSourceBase.SqlDataEventArgs e)
        {
            // Execute in main thread
            DGControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (e.EventKind)
                {
                    case DataSourceBase.DataEventKind.Clear:
                        if (DGControl.Visibility != Visibility.Collapsed)
                            DGControl.Visibility = Visibility.Collapsed;
                        _dataLoadedTime = null;
                        _dataLoadedTimer = new Stopwatch();
                        _dataLoadedTimer.Start();
                        _dataRecordsTimer.Start();
                        break;
                    case DataSourceBase.DataEventKind.Loading:
                        if (DGControl.Visibility != Visibility.Collapsed)
                            DGControl.Visibility = Visibility.Collapsed;
                        break;
                    case DataSourceBase.DataEventKind.Loaded:
                        if (DGControl.Visibility != Visibility.Collapsed)
                            DGControl.Visibility = Visibility.Collapsed;
                        _dataLoadedTimer.Stop();
                        _dataRecordsTimer.Stop();
                        _dataLoadedTime = Convert.ToInt32(_dataLoadedTimer.ElapsedMilliseconds);
                        Data.RefreshData();
                        break;
                    case DataSourceBase.DataEventKind.BeforeRefresh:
                        break;
                    case DataSourceBase.DataEventKind.Refreshed:
                        if (DGControl.Visibility != Visibility.Visible)
                            DGControl.Visibility = Visibility.Visible;
                        _dataNavigationTime = null;
                        if (Data!= null && !(Keyboard.FocusedElement is TextBox)) // QuickFilter
                        {
                            // Restore last active cell
                            var lastActiveItem = _lastCurrentCellInfo.Item;
                            var lastActiveColumn = _lastCurrentCellInfo.Column;
                            if (lastActiveItem != null && DGControl.Items.IndexOf(lastActiveItem) == -1) // FilterOnValue off for Group item
                                lastActiveItem = null;
                            if (lastActiveItem == null && ((IList)Data).Count > 0)
                            {
                                lastActiveItem = ((IList)Data)[0];
                                lastActiveColumn = DGControl.Columns.Where(c => c.Visibility == Visibility.Visible)
                                    .OrderBy(c => c.DisplayIndex).FirstOrDefault();
                            }

                            if (lastActiveItem != null && lastActiveColumn != null)
                            {
                                var newItem = new DataGridCellInfo(lastActiveItem, lastActiveColumn);
                                if (newItem.IsValid)
                                {
                                    var dataNavigationSW = new Stopwatch();
                                    dataNavigationSW.Start();
                                    DGControl.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        DGControl.ScrollIntoView(lastActiveItem, lastActiveColumn);
                                        DGControl.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            if (!DGControl.SelectedCells.Contains(newItem)) // Prevent the error
                                                DGControl.SelectedCells.Add(newItem);
                                            var activeCell = DGHelper.GetDataGridCell(newItem);
                                            activeCell?.Focus(); // Show/'cursor navigation' the active cell

                                            _dataNavigationTime = Convert.ToInt32(dataNavigationSW.ElapsedMilliseconds);
                                            dataNavigationSW.Stop();
                                            OnPropertiesChanged(nameof(StatusTextOfLeftLabel));
                                            // Clear DataLoadedTime
                                            _dataLoadedTime = null;
                                        }), DispatcherPriority.Render); // Highlight the focused cell
                                    }), DispatcherPriority.Normal); // Restore the horizontal scroll bar
                                }
                            }
                        }

                        _lastCurrentCellInfo = new DataGridCellInfo();
                        break;
                }

                DataStatus = e.EventKind;
                // DoEventsHelper.DoEvents();
            }));

        }
        #endregion
        private void OnDataRecordsTimerTick(object sender, EventArgs e)
        {
          OnPropertiesChanged(nameof(StatusTextOfLeftLabel));
        }
    }
}
