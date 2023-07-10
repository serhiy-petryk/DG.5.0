using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfSpLib.Helpers
{
    public static class ItemsControlHelper
    {
        public static async Task AddOrReorderItems(this ItemsControl control, object[] sourceData, int insertIndex)
        {
            var items = GetItemsHost(control).Children.OfType<FrameworkElement>().ToArray();
            var itemsSource = (IList)(control.ItemsSource ?? control.Items);

            // To prevent flicker during drop item
            control.ItemContainerGenerator.StatusChanged += OnItemContainerGeneratorStatusChanged;

            var animations = new List<Task>();
            var elements = new List<FrameworkElement>();
            var currentIndex = insertIndex;
            foreach (var item in sourceData)
            {
                var oldIndex = itemsSource.IndexOf(item);
                if (oldIndex >= 0)
                {
                    if (oldIndex < currentIndex) currentIndex--;
                    if (currentIndex != oldIndex)
                    {
                        var removingElement = items.FirstOrDefault(o => o.DataContext == item);
                        if (removingElement != null)
                        {
                            elements.Add(removingElement);
                            animations.AddRange(AnimationHelper.GetHeightContentAnimations(removingElement, false));
                        }
                    }
                }

                currentIndex++;
            }

            if (animations.Count > 0)
            {
                await Task.WhenAll(animations);
                foreach (var element in elements)
                    element.SetCurrentValueSmart(FrameworkElement.HeightProperty, double.NaN);
            }

            currentIndex = insertIndex;
            var insertingItems = new List<object>();
            foreach (var item in sourceData)
            {
                var oldIndex = itemsSource.IndexOf(item);
                if (oldIndex >= 0)
                {
                    if (oldIndex < currentIndex) currentIndex--;
                    if (oldIndex != currentIndex)
                        itemsSource.RemoveAt(oldIndex);
                }

                if (oldIndex != currentIndex)
                {
                    if (item is TabItem tabItem)
                        ((TabControl) tabItem.Parent)?.Items.Remove(tabItem);
                    itemsSource.Insert(currentIndex, item);
                    insertingItems.Add(item);
                }

                currentIndex++;
            }

            VisualHelper.DoEvents(DispatcherPriority.Render);

            animations.Clear();
            elements.Clear();
            foreach (var item in insertingItems)
            {
                if (control.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement itemVisual)
                {
                    animations.AddRange(AnimationHelper.GetHeightContentAnimations(itemVisual, true));
                    elements.Add(itemVisual);
                }
            }

            if (animations.Count > 0)
            {
                await Task.WhenAll(animations);
                foreach (var element in elements)
                    element.SetCurrentValueSmart(FrameworkElement.HeightProperty, double.NaN);
            }

            control.ItemContainerGenerator.StatusChanged -= OnItemContainerGeneratorStatusChanged;
        }

        public static void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            // Hide newly created items of ItemsControl
            // Usage: <ItemsControl>.ItemContainerGenerator.StatusChanged +=/-= OnItemContainerGeneratorStatusChanged;
            var generator = (ItemContainerGenerator)sender;
            if (generator.Status != GeneratorStatus.ContainersGenerated) return;
            foreach (var item2 in generator.Items)
            {
                if (generator.ContainerFromItem(item2) is FrameworkElement fe && fe.LayoutTransform == Transform.Identity && fe.ActualHeight < double.Epsilon && fe.ActualWidth < double.Epsilon)
                {
                    if (fe.LayoutTransform is ScaleTransform transform)
                        transform.ScaleY = 0.0;
                    else
                        fe.LayoutTransform = new ScaleTransform(1.0, 0.0);

                    fe.Dispatcher.BeginInvoke(new Action(() => fe.LayoutTransform = Transform.Identity), DispatcherPriority.Render);
                }
            }
        }

        private static PropertyInfo _piItemsHost;
        public static Panel GetItemsHost(this ItemsControl itemsControl)
        {
            if (_piItemsHost == null)
                _piItemsHost = typeof(ItemsControl).GetProperty("ItemsHost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (Panel)_piItemsHost.GetValue(itemsControl);
        }

        private static PropertyInfo _piDataGridHeaderHost;
        public static FrameworkElement GetHeaderHost(this ItemsControl itemsControl)
        {
            if (itemsControl is DataGrid)
            {
                if (_piDataGridHeaderHost == null)
                    _piDataGridHeaderHost = typeof(DataGrid).GetProperty("ColumnHeadersPresenter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return (DataGridColumnHeadersPresenter)_piDataGridHeaderHost.GetValue(itemsControl);
            }
            return null;
        }

        public static IEnumerable<ElementOfItemsControl> GetAllVisualItems(this ItemsControl control)
        {
            var panel = GetItemsHost(control);
            for (var i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is FrameworkElement element)
                {
                    yield return new ElementOfItemsControl(element, control);
                    if (element is ItemsControl itemsControl) // TreeViewItem
                    {
                        foreach (var childItem in GetAllVisualItems(itemsControl))
                            yield return childItem;
                    }
                }
            }
        }


        #region =============  Helper classes  ===============
        public class ElementOfItemsControl
        {
            public ItemsControl ItemsControl { get; }
            public FrameworkElement VisualElement { get; }

            public ElementOfItemsControl(FrameworkElement element, ItemsControl itemsControl)
            {
                ItemsControl = itemsControl;
                VisualElement = element;
                if (element is HeaderedItemsControl control) // TreeViewItem
                {
                    var aa1 = control.GetVisualChildren().OfType<ContentPresenter>().Where(o => o.ContentSource == "Header");
                    var aa2 = aa1.Where(o => o.GetVisualParents().OfType<ItemsControl>().FirstOrDefault() == element).ToArray();
                    if (aa2.Length == 1)
                        VisualElement = aa2[0];
                }
            }
        }
        #endregion
    }
}
