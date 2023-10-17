using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DGCore.UserSettings;
using DGView.ViewModels;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for SaveSettingView.xaml
    /// </summary>
    public partial class DGSaveSettingView : UserControl
    {
        private string _quickFilterText;
        public string QuickFilterText
        {
            get => _quickFilterText;
            set
            {
                if (!Equals(_quickFilterText, value))
                {
                    _quickFilterText = value;
                    SetFilter();
                }
            }
        }

        private DGViewModel _viewModel;
        private List<UserSettingsDbObject> DataSource => (List<UserSettingsDbObject>) DataGrid.ItemsSource;

        public DGSaveSettingView(DGViewModel viewModel, string lastAppliedLayoutName)
        {
            InitializeComponent();
            DataContext = this;
            _viewModel = viewModel;
            _quickFilterText = null;

            var oo = UserSettingsUtils.GetUserSettingDbObjects(viewModel);
            DataGrid.ItemsSource = oo;
            DataGrid.SelectedItem = DataGrid.Items.OfType<object>().FirstOrDefault();
            NewSettingName.Text = lastAppliedLayoutName;
            Dispatcher.BeginInvoke(new Action(() => { NewSettingName.Focus(); }), DispatcherPriority.Background);
            Unloaded += OnUnloaded;

            CmdDeleteRow = new RelayCommand(cmdDeleteRow, o => DataGrid.SelectedItems.Count == 1);
            CmdSaveChanges = new RelayCommand(cmdSaveChanges, o => DataSource.Any(o1 => o1.IsDeleted));
            CmdSaveNewSetting = new RelayCommand(cmdSaveNewSetting, o => !string.IsNullOrEmpty(NewSettingName.Text));
            CmdSetSetting = new RelayCommand(cmdSetSetting, o => DataGrid.SelectedItems.Count == 1);
        }

        private void SetFilter()
        {
            var view = CollectionViewSource.GetDefaultView(DataSource);
            view.Filter += Filter;
            DataGrid.SelectedItem = DataGrid.Items.OfType<object>().FirstOrDefault();
        }

        private bool Filter(object obj)
        {
            if (obj is UserSettingsDbObject o)
                return Helpers.Misc.SetFilter(o.SettingId, QuickFilterText);

            return true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.IsElementDisposing())
            {
                Unloaded -= OnUnloaded;
                _viewModel = null;
            }
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e) => DataGridHelper.DataGrid_OnSorting((DataGrid)sender, e);

        #region =========  Commands  ===========
        public RelayCommand CmdDeleteRow { get; }
        public RelayCommand CmdSaveChanges { get; }
        public RelayCommand CmdSaveNewSetting { get; }
        public RelayCommand CmdSetSetting { get; }
        private void cmdDeleteRow(object p)
        {
            if (DataGrid.SelectedItems.Count == 1)
            {
                var item = (UserSettingsDbObject)DataGrid.SelectedItems[0];
                item.IsDeleted = !item.IsDeleted;
                item.OnPropertiesChanged("IsDeleted");
            }
        }
        private void cmdSaveChanges(object p)
        {
            var result = UserSettingsUtils.SaveChangedSettings(DataSource, _viewModel);
            if (result == 0)
                new DialogBox(DialogBox.DialogBoxKind.Warning) { Message = "Не було змінено жодного налаштування", Buttons = new[] { "OK" } }.ShowDialog();
            else if (result < 0)
                new DialogBox(DialogBox.DialogBoxKind.Error) { Message = "Помилка! Було спроба записати налаштування, для якого Ви не маєте права це робити", Buttons = new[] { "OK" } }.ShowDialog();
            else
            {
                new DialogBox(DialogBox.DialogBoxKind.Info) { Message = $"Було записано {result} змінених налаштуваннь", Buttons = new[] { "OK" } }.ShowDialog();
                var oo = UserSettingsUtils.GetUserSettingDbObjects(_viewModel);
                DataGrid.ItemsSource = oo;
                DataGrid.SelectedItem = DataGrid.Items.OfType<object>().FirstOrDefault();
            }
        }

        private void cmdSaveNewSetting(object p)
        {
            var settingId = NewSettingName.Text;
            var allowEdit = cbAllowEditToOthers.IsChecked ?? false;
            var allowView = cbAllowViewToOthers.IsChecked ?? false;

            if (string.IsNullOrEmpty(settingId))
            {
                new DialogBox(DialogBox.DialogBoxKind.Error) { Message = "Налаштування не може бути пустим", Buttons = new[] { "OK" } }.ShowDialog();
                return;
            }

            if (UserSettingsUtils.SaveNewSetting(_viewModel, settingId, allowView, allowEdit))
            {
                _viewModel.cmdSetSetting(settingId);
                ApplicationCommands.Close.Execute(null, this);
            }
        }

        private void cmdSetSetting(object p)
        {
            if (DataGrid.SelectedItems.Count == 1)
            {
                var item = (UserSettingsDbObject)DataGrid.SelectedItems[0];
                _viewModel.cmdSetSetting(item.SettingId);
            }
            ApplicationCommands.Close.Execute(null, this);
        }
        #endregion

        private void DataGrid_OnCopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            for (var i = 0; i < e.ClipboardRowContent.Count; i++)
            {
                if (e.ClipboardRowContent[i].Column.SortMemberPath != "SettingId")
                    e.ClipboardRowContent.RemoveAt(i--);
            }
        }

        private void ScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
