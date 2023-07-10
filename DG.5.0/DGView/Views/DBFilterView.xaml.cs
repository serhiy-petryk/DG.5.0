using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DGCore.UserSettings;
using WpfSpLib.Common;
using WpfSpLib.Controls;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for DbFilterView.xaml
    /// </summary>
    public partial class DBFilterView : UserControl, INotifyPropertyChanged, IUserSettingSupport<List<Filter>>
    {
        public bool IsOpenSettingsButtonEnabled => UserSettingsUtils.GetKeysFromDb(this).Count > 0;
        public RelayCommand CmdLoadData { get; }
        public RelayCommand CmdSaveFilter { get; }
        public RelayCommand CmdClearFilter { get; }
        public Action ApplyAction { get; private set; }
        private string _lastAppliedLayoutName;

        public DBFilterView()
        {
            InitializeComponent();
            DataContext = this;

            CmdLoadData = new RelayCommand(p => ApplyAction?.Invoke());

            CmdSaveFilter = new RelayCommand(p =>
            {
                new DialogBox(DialogBox.DialogBoxKind.Warning) {Message = "Not ready!", Buttons = new[] {"OK"}};
            });

            CmdClearFilter = new RelayCommand(p =>
            {
                FilterGrid.FilterList.ClearFilter();
                RefreshUI();
            });
        }

        public void Bind(DGCore.Filters.FilterList newFilterList, string settingKey, Action applyAction, ICollection dataSource)
        {
            SettingKey = settingKey;
            ApplyAction = applyAction;
            FilterGrid.Bind(newFilterList, dataSource);
            UserSettingsUtils.Init(this, null);
            RefreshUI();
        }

        #region ==========  Event handlers  ==========
        private void OpenSettingButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Resources.Values.OfType<ContextMenu>().FirstOrDefault() is ContextMenu cm)
            {
                cm.Items.Clear();
                foreach (var settingName in UserSettingsUtils.GetKeysFromDb(this))
                {
                    cm.Items.Add(new MenuItem
                    {
                        Header = settingName,
                        IsChecked = settingName == _lastAppliedLayoutName,
                        // DataContext = item,
                        Command = new WpfSpLib.Common.RelayCommand((p) =>
                        {
                            UserSettingsUtils.SetSetting(this, settingName);
                            _lastAppliedLayoutName = settingName;
                            RefreshUI();
                        })
                    });
                }
            }
        }
        #endregion

        #region ============  INotifyPropertyChanged  ============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void RefreshUI()
        {
            OnPropertiesChanged(nameof(ApplyAction), nameof(IsOpenSettingsButtonEnabled));
            FilterGrid.RefreshUI();
        }
        #endregion

        #region ===========  IUserSettingSupport<List<Filter>  ==============

        public string SettingKind => "DGV_DBFilter";
        public string SettingKey { get; private set; }

        List<Filter> IUserSettingSupport<List<Filter>>.GetSettings() =>
            ((IUserSettingSupport<List<Filter>>)FilterGrid.FilterList).GetSettings();

        List<Filter> IUserSettingSupport<List<Filter>>.GetBlankSetting() =>
            ((IUserSettingSupport<List<Filter>>)FilterGrid.FilterList).GetBlankSetting();

        void IUserSettingSupport<List<Filter>>.SetSetting(List<Filter> settings) =>
            ((IUserSettingSupport<List<Filter>>)FilterGrid.FilterList).SetSetting(settings);
        #endregion
    }
}
