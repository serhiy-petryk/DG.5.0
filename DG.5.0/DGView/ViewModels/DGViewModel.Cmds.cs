using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DGCore.Helpers;
using DGView.Controls.Printing;
using DGView.Helpers;
using DGView.Views;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.ViewModels
{
    public partial class DGViewModel
    {
        public RelayCommand CmdEditSetting { get; private set; }
        public RelayCommand CmdSetSetting { get; private set; }
        public RelayCommand CmdSaveSetting { get; private set; }
        public RelayCommand CmdRowDisplayMode { get; private set; }
        public RelayCommand CmdSetGroupLevel { get; private set; }
        public RelayCommand CmdSetSortAsc { get; private set; }
        public RelayCommand CmdSetSortDesc { get; private set; }
        public RelayCommand CmdClearSortings { get; private set; }
        public RelayCommand CmdSetFilterOnValue { get; private set; }
        public RelayCommand CmdClearFilterOnValue { get; private set; }
        public RelayCommand CmdSearch { get; private set; }
        public RelayCommand CmdPrint { get; private set; }
        public RelayCommand CmdClone { get; private set; }
        public RelayCommand CmdRequery { get; private set; }
        public RelayCommand CmdSaveAsExcelFile { get; private set; }
        public RelayCommand CmdSaveAsTextFile { get; private set; }

        public string Title => DGControl.GetVisualParents().OfType<MwiChild>().First().Title;

        private void InitCommands()
        {
            CmdEditSetting = new RelayCommand(cmdEditSetting);
            CmdSetSetting = new RelayCommand(cmdSetSetting);
            CmdSaveSetting = new RelayCommand(cmdSaveSetting);
            CmdRowDisplayMode = new RelayCommand(cmdRowDisplayMode);
            CmdSetGroupLevel = new RelayCommand(cmdSetGroupLevel);

            CmdSetSortAsc = new RelayCommand(cmdSetSortAsc);
            CmdSetSortDesc = new RelayCommand(cmdSetSortDesc);
            CmdClearSortings = new RelayCommand(cmdClearSortings);

            CmdSetFilterOnValue = new RelayCommand(cmdSetFilterOnValue);
            CmdClearFilterOnValue = new RelayCommand(cmdClearFilterOnValue);

            CmdSearch = new RelayCommand(cmdSearch);
            CmdClone = new RelayCommand(cmdClone);
            CmdRequery = new RelayCommand(cmdRequery);

            CmdPrint = new RelayCommand(cmdPrint);
            CmdSaveAsExcelFile = new RelayCommand(cmdSaveAsExcelFile);
            CmdSaveAsTextFile = new RelayCommand(cmdSaveAsTextFile);
        }

        private void cmdEditSetting(object p)
        {
            var dgView = DGControl.GetVisualParents().OfType<DataGridView>().FirstOrDefault();
            var geometry = (Geometry) dgView.Resources["SettingsGeometry"];
            Misc.OpenDGDialog(DGControl, new DGEditSettingsView(this), "Edit setting", geometry);
        }
        internal void cmdSetSetting(object p)
        {
            var newSetting = (string)p;
            DGControl.EnableColumnVirtualization = true; // prevent data binding error: BindingExpression:Path=Background; DataItem=null; target element is 'DataGridColumnHeader' (Name=''); target property is 'Background' (type 'Brush')
            DGCore.UserSettings.UserSettingsUtils.Init(this, newSetting);
            LastAppliedLayoutName = newSetting;
        }
        private void cmdSaveSetting(object p)
        {
            var dgView = DGControl.GetVisualParents().OfType<DataGridView>().FirstOrDefault();
            var geometry = (Geometry)dgView.Resources["SaveGeometry"];
            Misc.OpenDGDialog(DGControl, new DGSaveSettingView(this, LastAppliedLayoutName), "Save setting", geometry);
        }
        private void cmdRowDisplayMode(object p)
        {
            var rowViewMode = (DGCore.Common.Enums.DGRowViewMode)Enum.Parse(typeof(DGCore.Common.Enums.DGRowViewMode), (string)p);
            RowViewMode = rowViewMode;
        }
        private void cmdSetGroupLevel(object p)
        {
            var i = (int?)p;
            Data.A_SetGroupLevel(i.HasValue ? Math.Abs(i.Value) : (int?)null, (i ?? 0) >= 0);
            SetColumnVisibility();
        }
        private void cmdSetSortAsc(object p)
        {
            if (IsSetFilterOnValueOrSortingEnable)
            {
                _lastCurrentCellInfo = DGControl.SelectedCells[0];
                Data.A_ApplySorting(_lastCurrentCellInfo.Column.SortMemberPath, _lastCurrentCellInfo.Item, ListSortDirection.Ascending);
            }
        }
        private void cmdSetSortDesc(object p)
        {
            if (IsSetFilterOnValueOrSortingEnable)
            {
                _lastCurrentCellInfo = DGControl.SelectedCells[0];
                Data.A_ApplySorting(_lastCurrentCellInfo.Column.SortMemberPath, _lastCurrentCellInfo.Item, ListSortDirection.Descending);
            }
        }
        private void cmdClearSortings(object p)
        {
            if (DGControl.SelectedCells.Count != 1) return;
            var cell = DGControl.SelectedCells[0];
            if (!cell.IsValid || cell.Item == null || string.IsNullOrEmpty(cell.Column.SortMemberPath)) return;

            _lastCurrentCellInfo = cell;
            Data.A_RemoveSorting(cell.Column.SortMemberPath, cell.Item);
        }
        private void cmdSetFilterOnValue(object p)
        {
            if (IsSetFilterOnValueOrSortingEnable)
            {
                _lastCurrentCellInfo = DGControl.SelectedCells[0];
                var pd = Data.Properties[_lastCurrentCellInfo.Column.SortMemberPath];
                var value = pd.GetValue(_lastCurrentCellInfo.Item);
                Data.A_SetByValueFilter(_lastCurrentCellInfo.Column.SortMemberPath, value);
            }
            OnPropertiesChanged(nameof(IsClearFilterOnValueEnable));
        }
        private void cmdClearFilterOnValue(object p)
        {
            if (DGControl.SelectedCells.Count == 1)
                _lastCurrentCellInfo = DGControl.SelectedCells[0];
            Data.A_ClearByValueFilter();
            OnPropertiesChanged(nameof(IsClearFilterOnValueEnable));
        }

        private DGFindTextView _findTextView;
        private void cmdSearch(object p)
        {
            var mwiChild = DGControl.GetVisualParents().OfType<MwiChild>().FirstOrDefault();
            if (mwiChild == null) return;

            if (_findTextView == null)
                _findTextView = new DGFindTextView(mwiChild, this);

            _findTextView.ToggleVisibility();
        }
        private void cmdClone(object p)
        {
            var mwiChild = DGControl.GetVisualParents().OfType<MwiChild>().FirstOrDefault();
            if (mwiChild == null) return;

            var dgView = CreateDataGrid(mwiChild.MwiContainer, Title);
            var settings = GetSettings();
            dgView.ViewModel.Bind(Data.UnderlyingData, LayoutId, StartUpParameters, LastAppliedLayoutName, settings);
        }
        private void cmdRequery(object p)
        {
            Data.RequeryData();
        }

        private void cmdPrint(object p)
        {
            using (var generator = new DGDirectRenderingPrintContentGenerator(this))
                new PrintPreviewWindow(generator) {Owner = Window.GetWindow(DGControl)}.ShowDialog();
        }

        private void cmdSaveAsExcelFile(object p)
        {
            DGHelper.GetSelectedArea(DGControl, out var items, out var columns);
            var columnHelpers = DGHelper.GetColumnHelpers(columns, Properties, null);
            var filename = $"DGV_{LayoutId}.{ExcelApp.GetDefaultExtension()}";
            var groupColumnNames = Data.Groups.Select(g => g.PropertyDescriptor.Name).ToList();
            SaveData.SaveAndOpenDataToXlsFile(filename, Title,
                Data.GetSubheaders_ExcelAndPrint(StartUpParameters, LastAppliedLayoutName), items, columnHelpers,
                groupColumnNames);
        }

        private void cmdSaveAsTextFile(object p)
        {
            DGHelper.GetSelectedArea(DGControl, out var items, out var columns);
            var columnHelpers = DGHelper.GetColumnHelpers(columns, Properties, null);
            var filename = $"DGV_{LayoutId}.txt";
            SaveData.SaveAndOpenDataToTextFile(filename, items, columnHelpers);
        }
    }
}
