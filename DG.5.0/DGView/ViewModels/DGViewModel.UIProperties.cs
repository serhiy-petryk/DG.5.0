using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DGCore.Common;
using DGCore.Sql;

namespace DGView.ViewModels
{
    public partial class DGViewModel
    {
        #region  ==========  Static section  ============
        private static string _plusSquareGeometryString = "M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z";
        private static string _minusSquareGeometryString = "M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z M4 8a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7A.5.5 0 0 1 4 8z";
        internal static Geometry PlusSquareGeometry = Geometry.Parse(_plusSquareGeometryString);
        internal static Geometry MinusSquareGeometry = Geometry.Parse(_minusSquareGeometryString);
        #endregion

        #region ======= Status bar properties =======
        private DataSourceBase.DataEventKind _dataStatus;
        public DataSourceBase.DataEventKind DataStatus
        {
            get => _dataStatus;
            private set
            {
                _dataStatus = value;
                OnPropertiesChanged(nameof(DataStatus), nameof(IsPartiallyLoaded), nameof(StatusRowsLabel), nameof(StatusTextOfLeftLabel));
            }
        }
        public bool IsPartiallyLoaded => Data?.UnderlyingData.IsPartiallyLoaded ?? false;

        public string StatusTextOfLeftLabel
        {
            get
            {
                var sb = new StringBuilder();
                if (_dataLoadedTime.HasValue)
                    sb.Append(string.Format((string)Application.Current.Resources["Loc:DGV.Status.LoadedTime"], _dataLoadedTime));
                switch (DataStatus)
                {
                    case DataSourceBase.DataEventKind.Clear:
                        sb.Append(" " + (string)Application.Current.Resources["Loc:DGV.Status.PreparingData"]);
                        break;
                    case DataSourceBase.DataEventKind.Loading:
                        sb.Append(" " + string.Format((string)Application.Current.Resources["Loc:DGV.Status.LoadingRows"], Data.UnderlyingData.RecordCount));
                        break;
                    case DataSourceBase.DataEventKind.BeforeRefresh:
                        sb.Append(" " + string.Format((string)Application.Current.Resources["Loc:DGV.Status.DataProcessing"]));
                        break;
                    case DataSourceBase.DataEventKind.Refreshed:
                        sb.Append(" " + string.Format((string)Application.Current.Resources["Loc:DGV.Status.DataProcessed"], Data?.LastRefreshedTimeInMsecs ?? 0));
                        if (_dataNavigationTime.HasValue)
                            sb.Append(". " + string.Format((string)Application.Current.Resources["Loc:DGV.Status.NavigationTime"], _dataNavigationTime.Value));
                        break;
                }

                return sb.ToString().Trim();
            }
        }
        public string StatusRowsLabel
        {
            get
            {
                if (Data == null) return null;
                var totals = Data.UnderlyingData.IsDataReady ? Data.UnderlyingData.GetData(false).Count : 0;
                var filtered = Data.FilteredRowCount;
                if (totals == filtered)
                    return totals.ToString("N0");
                return $"{filtered:N0}/{totals:N0}";
            }
        }

        #endregion

        #region =======  RowViewMode  ========
        private Enums.DGRowViewMode _rowViewMode = Enums.DGRowViewMode.OneRow;
        public Enums.DGRowViewMode RowViewMode
        {
            get => _rowViewMode;
            set
            {
                _rowViewMode = value;
                SetCellElementStyleAndWidth();

                if (RowViewMode == Enums.DGRowViewMode.OneRow)
                    DGControl.RowHeight = DGControl.FontSize * 1.5 + 2;
                else if (!double.IsNaN(DGControl.RowHeight))
                    DGControl.RowHeight = double.NaN;

                OnPropertiesChanged(nameof(RowViewMode), nameof(RowViewModeLabel));
            }
        }
        public string RowViewModeLabel => _rowViewMode.ToString();
        #endregion

        #region =====  Quick Filter ======
        private string _quickFilterText;
        public string QuickFilterText
        {
            get => _quickFilterText;
            set
            {
                if (!Equals(_quickFilterText, value))
                {
                    _quickFilterText = value;
                    OnPropertiesChanged(nameof(QuickFilterText));
                    DGControl.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Data.A_FastFilterChanged(value);
                        SetColumnVisibility();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }
        #endregion

        private bool _isGridLinesVisible = true;
        public bool IsGridLinesVisible
        {
            get => _isGridLinesVisible;
            set
            {
                _isGridLinesVisible = value;
                OnPropertiesChanged(nameof(IsGridLinesVisible));
                ((Controls.CustomDataGrid)DGControl).RepaintRows(); // more faster than Data.ResetBindings();
            }
        }

        public bool IsGroupLevelButtonEnabled => Data != null && Data.Groups.Count > 0;

        public bool IsClearFilterOnValueEnable => Data?.FilterByValue != null && !Data.FilterByValue.IsEmpty;

        //===================
        //public string[] UserSettings => DesignerProperties.GetIsInDesignMode(this) ? new string[0] : DGCore.UserSettings.UserSettingsUtils.GetKeysFromDb(this).ToArray();
        public string[] UserSettings => new string[]{null}.Union(DGCore.UserSettings.UserSettingsUtils.GetKeysFromDb(this)).ToArray();
        public bool IsSelectSettingEnabled => UserSettings.Length > 0;

        internal string StartUpParameters { get; set; }
        public string LastAppliedLayoutName { get; private set; }

        internal DataGridCellInfo _lastCurrentCellInfo;
        internal DataGridTextColumn GroupItemCountColumn = null;
        internal List<DataGridColumn> _groupColumns = new List<DataGridColumn>();
        internal List<double> _fontFactors = new List<double>();
        internal List<DGCore.UserSettings.Column> _columns = new List<DGCore.UserSettings.Column>();
        private List<string> _frozenColumns = new List<string>();

        //========================
        private DGCore.Helpers.DGColumnHelper[] GetAllValidColumnHelpers() => DGControl.Columns
            .Where(c => !string.IsNullOrEmpty(c.SortMemberPath) && c.Visibility == Visibility.Visible).Select(c =>
                new DGCore.Helpers.DGColumnHelper(Properties[c.SortMemberPath], c.DisplayIndex,
                    _columns.FirstOrDefault(column =>
                        string.Equals(column.Id, c.SortMemberPath, StringComparison.OrdinalIgnoreCase))?.Format_Actual))
            .ToArray();
    }
}
