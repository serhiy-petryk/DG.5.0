using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DGCore.Common;
using DGCore.UserSettings;
using DGView.Controls;

namespace DGView.ViewModels
{
    public partial class DGViewModel
    {
        internal const string UserSettingsKind = "DGV_Setting";
        string IUserSettingProperties.SettingKind => UserSettingsKind;
        private string LayoutId { get; set; }
        string IUserSettingProperties.SettingKey => LayoutId;

        public DGV GetSettings()
        {
            var o = new DGV
            {
                WhereFilter = ((IUserSettingSupport<List<Filter>>)Data.WhereFilter)?.GetSettings(),
                FilterByValue = ((IUserSettingSupport<List<Filter>>)Data.FilterByValue)?.GetSettings(),
                ShowTotalRow = Data.ShowTotalRow,
                ExpandedGroupLevel = Data.ExpandedGroupLevel,
                ShowGroupsOfUpperLevels = Data.ShowGroupsOfUpperLevels,
                BaseFont = $"{DGControl.FontFamily}, {DGControl.FontSize * 72.0 / 96.0}pt",
                IsGridVisible = IsGridLinesVisible,
                RowViewMode = RowViewMode,
                TextFastFilter = QuickFilterText
            };
            SaveColumnLayout(o);
            return o;
        }

        public DGV GetBlankSetting()
        {
            // Utils.Dgv.EndEdit(this);
            Data.ResetSettings();

            var cnt = 0;
            foreach (var column in DGControl.Columns)
            {
                column.Visibility = column.SortMemberPath.Contains(DGCore.Common.Constants.MDelimiter) ? Visibility.Collapsed : Visibility.Visible;
                column.DisplayIndex = cnt++;
            }

            DGControl.FrozenColumnCount = 0;
            //Font = _startupFont;
            // CellBorderStyle = DataGridViewCellBorderStyle.Single; // For _IsGridVisible
            // RowViewMode = Enums.DGRowViewMode.OneRow;

            // For AllColumns
            //_allValidColumnNames = DGControl.Columns.Where(col => !string.IsNullOrEmpty(col.SortMemberPath) && !col.SortMemberPath.Contains('.'))
            //  .Select(col => col.SortMemberPath).ToList();

            ResizeColumnWidth(); // !!! Before SaveColumnInfo*/
            return ((IUserSettingSupport<DGV>)this).GetSettings();
        }

        public int _layoutCount = 0;
        public void ResizeColumnWidth()
        {
            this._layoutCount++;
            // this.AutoResizeColumns(this._RowViewMode == DGCore.Common.Enums.DGRowViewMode.NotSet ? DataGridViewAutoSizeColumnsMode.DisplayedCells : DataGridViewAutoSizeColumnsMode.ColumnHeader, false);
            // old this.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader, true);
        }

        public void SetSetting(DGV settings)
        {
            if (settings.Groups.Count != _groupColumns.Count)
                ManageGroupColumns(settings.Groups.Count);

            // Prepare columns list
            _frozenColumns.Clear();
            _frozenColumns.AddRange(settings.FrozenColumns);

            _columns.Clear();
            _columns.AddRange(settings.AllColumns);
            for (var k = 0; k < _columns.Count; k++)
            {
                var dgCol = DGControl.Columns.FirstOrDefault(c => c.SortMemberPath == _columns[k].Id);
                if (dgCol == null)
                    _columns.RemoveAt(k--);
                else if (dgCol is DataGridBoundColumn boundColumn && !Equals(boundColumn.Binding.StringFormat, _columns[k].Format))
                {
                    var b = WpfSpLib.Helpers.BindingHelper.CloneBinding((Binding)boundColumn.Binding);
                    b.StringFormat = _columns[k].Format;
                    boundColumn.Binding = b;
                }
            }
            foreach (var col in DGControl.Columns.Where(c => !string.IsNullOrEmpty(c.SortMemberPath)))
            {
                var c1 = _columns.FirstOrDefault(c => c.Id == col.SortMemberPath);
                if (c1 == null)
                    _columns.Add(new Column() { Id = col.SortMemberPath, IsHidden = true });
                // _columns.Add(new Column() { Id = col.SortMemberPath, DisplayName = col.SortMemberPath.Replace(".", "^"), IsHidden = true });
                col.CanUserReorder = !_frozenColumns.Contains(col.SortMemberPath);
            }

            // ToDo: reorder columns according frozen columns

            // ====================
            // Prepare start column layout : restore columns order & width
            // ====================
            DGControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                var colCnt = 0;
                for (var k = 0; k < _groupColumns.Count; k++)
                {
                    if (_groupColumns[k].DisplayIndex != colCnt++)
                        _groupColumns[k].DisplayIndex = colCnt - 1;
                }

                if (GroupItemCountColumn != null && GroupItemCountColumn.DisplayIndex != colCnt++)
                    GroupItemCountColumn.DisplayIndex = colCnt - 1;

                for (var k = 0; k < _columns.Count; k++)
                {
                    var col = _columns[k];
                    var dgCol = DGControl.Columns.First(c => c.SortMemberPath == col.Id);
                    if (dgCol.DisplayIndex != colCnt++)
                        dgCol.DisplayIndex = colCnt - 1;
                }

                DGControl.FrozenColumnCount = _groupColumns.Count + _frozenColumns.Count + (GroupItemCountColumn == null ? 0 : 1);
            }));

            SetColumnVisibility();

            Data.SetSettings(settings);
            RowViewMode = settings.RowViewMode;
            if (!string.IsNullOrEmpty(settings.BaseFont))
            {
                var ss = settings.BaseFont.Trim().Split(',');
                DGControl.FontFamily = new FontFamily(ss[0]);
                if (ss[1].EndsWith("pt"))
                    DGControl.FontSize = double.Parse(ss[1].Substring(0, ss[1].Length - 2).Trim(), CultureInfo.InvariantCulture) * 96.0 / 72.0;
                else
                {
                    double d;
                    if (double.TryParse(ss[1].Trim(), out d))
                        DGControl.FontSize = d;
                }
            }

            OnPropertiesChanged(nameof(IsGroupLevelButtonEnabled));

            // Fixed bug (RefreshData()):
            // 2. Зміна записаних налаштувань для MastCoA.
            //  - default налаштування з групами
            //  - якщо міняємо налаштування, то збиваються колонки
            if (Data.UnderlyingData.IsDataReady)
                Data.RefreshData();
            // else
            // await Task.Factory.StartNew(() => Data.UnderlyingData.GetData(false));

            // Invalidate(); // corrected bug - column header is blank after apply setting with new column
        }

        internal void SetColumnVisibility()
        {
            DGControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Set group columns visibility
                for (var k = 0; k < _groupColumns.Count; k++)
                    Helpers.DGHelper.SetColumnVisibility(_groupColumns[k], Data.IsGroupColumnVisible(k));

                // Set GroupItemCount column visibility
                if (GroupItemCountColumn != null)
                    Helpers.DGHelper.SetColumnVisibility(GroupItemCountColumn, Data.Groups.Count > 0);

                foreach (var dgCol in DGControl.Columns.OfType<DataGridBoundColumn>().Where(c => !string.IsNullOrEmpty(c.SortMemberPath)))
                {
                    var col = _columns.FirstOrDefault(c => c.Id == dgCol.SortMemberPath);
                    if (col == null)
                        Helpers.DGHelper.SetColumnVisibility(dgCol, false);
                    else
                        Helpers.DGHelper.SetColumnVisibility(dgCol, !col.IsHidden && Data.IsPropertyVisible(dgCol.SortMemberPath));
                }

                _fontFactors = new List<double> { 1.0 };
                var factor = 0.0;
                for (var k = _groupColumns.Count - 1; k >= 0; k--)
                {
                    if (_groupColumns[k].Visibility == Visibility.Visible)
                    {
                        _fontFactors.Insert(1, factor);
                        factor = factor < 0.5 ? 1.0 : factor * 1.05;
                    }
                    else
                        _fontFactors.Insert(1, factor);
                }
            }));
        }

        private void ManageGroupColumns(int groupCount)
        {
            if (GroupItemCountColumn == null)
            {
                GroupItemCountColumn = DGControl.Resources["GroupItemCountColumn"] as DataGridColumn;
                DGControl.Columns.Insert(0, GroupItemCountColumn);
            }

            // Remove all group columns
            foreach (var groupColumn in _groupColumns)
                DGControl.Columns.Remove(groupColumn);
            _groupColumns.Clear();

            // Create group columns
            while (groupCount > _groupColumns.Count)
            {
                var groupColumn = DGControl.Resources["GroupColumn"] as DataGridColumn;
                groupColumn.Width = _groupColumns.Count == 0 ? 21 : 20;
                groupColumn.HeaderStringFormat = $"{Constants.GroupColumnNamePrefix}{_groupColumns.Count}";
                DGControl.Columns.Insert(_groupColumns.Count, groupColumn);
                _groupColumns.Add(groupColumn);
            }

            CustomDataGrid.CheckGroupBrushes(_groupColumns.Count);
        }

        private void SaveColumnLayout(DGV settings)
        {
            // Columns in display order
            var cols = DGControl.Columns.OrderBy(c => c.DisplayIndex).ToArray();

            // Set columns for default settings
            foreach (var c in cols)
            {
                if (!string.IsNullOrEmpty(c.SortMemberPath))
                {
                    var col = _columns.FirstOrDefault(c1 => c1.Id == c.SortMemberPath);
                    settings.AllColumns.Add(new Column
                    {
                        Id = c.SortMemberPath,
                        Format = (c as DataGridBoundColumn)?.Binding?.StringFormat,
                        // DisplayName = Properties[c.SortMemberPath].DisplayName,
                        // IsHidden = c.Visibility != Visibility.Visible,
                        IsHidden = col?.IsHidden ?? c.Visibility != Visibility.Visible,
                        Width = c.Width.IsAuto ? (int?)null : Convert.ToInt32(c.ActualWidth)
                    });

                    if (c.IsFrozen)
                        settings.FrozenColumns.Add(c.SortMemberPath);
                }
            }

            settings.Groups.AddRange(Data.Groups.Select(e => new Sorting { Id = e.PropertyDescriptor.Name, SortDirection = e.SortDirection }));

            settings.Sorts.AddRange(Data.Sorts.Select(e => new Sorting { Id = e.PropertyDescriptor.Name, SortDirection = e.SortDirection }));

            Data.SortsOfGroups.ForEach(e1 =>
            {
                var list = e1.Select(e2 => new Sorting { Id = e2.PropertyDescriptor.Name, SortDirection = e2.SortDirection }).ToList();
                settings.SortsOfGroup.Add(list);
            });

            foreach (var totalLine in Data.TotalLines.Where(tl => tl.TotalFunction != DGCore.Common.Enums.TotalFunction.None))
                settings.TotalLines.Add(new TotalLine
                {
                    Id = totalLine.Id,
                    TotalFunction = totalLine.TotalFunction
                });
        }

        private void SetCellElementStyleAndWidth()
        {
            foreach (var col in DGControl.Columns.OfType<DataGridTextColumn>())
            {
                var p = Properties.OfType<PropertyDescriptor>().FirstOrDefault(p1 => p1.Name == col.SortMemberPath);
                if (p != null)
                {
                    // ?? need new class :Generic alignment; var alignment1 = ((IMemberDescriptor)p).Alignment;
                    var alignment = Helpers.DGHelper.GetDefaultColumnAlignment(p.PropertyType);

                    if (alignment.HasValue)
                    {
                        DGControl.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var style = new Style(typeof(TextBlock));
                            style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
                            style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(2)));
                            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment.Value));
                            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, RowViewMode == Enums.DGRowViewMode.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap));
                            if (RowViewMode != Enums.DGRowViewMode.WordWrap)
                                style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
                            col.ElementStyle = style;
                        }), DispatcherPriority.ContextIdle);
                    }
                }
            }
        }
    }
}
