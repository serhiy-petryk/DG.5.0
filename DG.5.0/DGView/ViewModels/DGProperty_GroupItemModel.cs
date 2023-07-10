using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace DGView.ViewModels
{
    public class PropertyGroupItem : INotifyPropertyChanged, IEquatable<PropertyGroupItem>
    {
        public enum ItemType { Root, Details, Group, Label, Sorting}
        public static IEnumerable<PropertyGroupItem> GetAllChildren(PropertyGroupItem current)
        {
            yield return current;
            foreach (var item in current.Children)
            foreach (var child in GetAllChildren(item))
                yield return child;
        }

        public PropertyGroupItem Parent { get; }
        public DGProperty_ItemModel Item { get; }

        private ListSortDirection _sortDirection;
        public ListSortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                _sortDirection = value;
                if (Type == ItemType.Group && !Equals(Item.GroupDirection, value))
                    Item.GroupDirection = value;
                OnPropertiesChanged(nameof(SortDirection));
            }
        }

        public bool CanSort => Type == ItemType.Group  || Type == ItemType.Sorting;
        public string Name => Type == ItemType.Details  ? "Sortings of detail items:" : (Type == ItemType.Root ? "Root" : (Item == null ? "Sortings:" : Item.Name));
        public ItemType Type => Parent == null ? ItemType.Root : (Item == null ? (Parent.Type == ItemType.Root  ? ItemType.Details : ItemType.Label) : (Parent.Type == ItemType.Root ? ItemType.Group : ItemType.Sorting));
        public ObservableCollection<PropertyGroupItem> Children { get; } = new ObservableCollection<PropertyGroupItem>();
        public PropertyGroupItem Root => Parent == null ? this : Parent.Root;
        public Color BaseColor
        {
            get
            {
//                if (Type == "Details") return Color.FromArgb(0xFF, 0xF5, 0xFA, 0xFF);
                if (Type == ItemType.Details) return Colors.White;
                if (Type == ItemType.Group)
                {
                    var groupIndex = Root.Children.IndexOf(this) % (DGCore.Helpers.ColorInfo.GroupColors.Length - 1) + 1;
                    var groupColor = DGCore.Helpers.ColorInfo.GroupColors[groupIndex];
                    return Color.FromArgb(255, groupColor.R, groupColor.G, groupColor.B);
                }
                return Parent.BaseColor;
            }
        }

        public RelayCommand CmdRemove { get; }

        public PropertyGroupItem(PropertyGroupItem parent, DGProperty_ItemModel item = null)
        {
            Parent = parent;
            Item = item;
            if (Type == ItemType.Group || Type == ItemType.Details) // Add 'Sortings:' label
                Children.Add(new PropertyGroupItem(this));

            CmdRemove = new RelayCommand(cmdRemove);
        }

        private async void cmdRemove(object p)
        {
            if (Type == ItemType.Group)
                Item.GroupDirection = null;
            else if (Type == ItemType.Sorting)
            {
                if (((FrameworkElement)p).GetVisualParents().OfType<TreeViewItem>().FirstOrDefault(o => o.DataContext == this) is TreeViewItem tvi)
                    await Task.WhenAll(AnimationHelper.GetHeightContentAnimations(tvi, false));
                Parent.Children.Remove(this);
            }
        }

        public PropertyGroupItem AddNewItem(DGProperty_ItemModel item, ListSortDirection sortDirection)
        {
            var oldItem = Children.FirstOrDefault(o => o.Item == item);
            if (oldItem != null)
                Children.Remove(oldItem);
            var newItem = new PropertyGroupItem(this, item) { SortDirection = sortDirection };
            if (Children.Count > 0 && Children[Children.Count - 1].Type == ItemType.Details)
                Children.Insert(Children.Count - 1, newItem);
            else
                Children.Add(newItem);
            return newItem;
        }

        public void UpdateUI()
        {
            OnPropertiesChanged(nameof(BaseColor));
        }

        public bool Equals(PropertyGroupItem other) => other != null && Equals(Item, other.Item) && Equals(Parent, other.Parent);

        public override string ToString() => Name;

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;
        internal void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
