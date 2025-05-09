using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class ResizableControl
    {
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AddLoadedEvents();
            AddContentChangedEvents(Content);
        }

        private static Dictionary<Type, List<FieldInfo>> _fiCache = new Dictionary<Type, List<FieldInfo>>();
        private static Dictionary<Type, List<PropertyInfo>> _piCache = new Dictionary<Type, List<PropertyInfo>>();

        private void AddLoadedEvents()
        {
            foreach (var thumb in this.GetVisualChildren<Thumb>().Where(t => t.Name.StartsWith("Resize")))
            {
                thumb.DragDelta -= ResizeThumb_OnDragDelta;
                thumb.DragDelta += ResizeThumb_OnDragDelta;
            }

            if (HostPanel.GetVisualParents().OfType<ScrollViewer>().FirstOrDefault() is ScrollViewer sv && !Equals(sv.Resources["State"], "Activated"))
            {
                sv.Resources.Add("State", "Activated");
                sv.ScrollChanged += ScrollViewer_OnScrollChanged;
            }
        }

        private void AddVisualParentChangedEvents(bool onlyRemove = false)
        {
            if (!(Parent is Window wnd)) return;

            if (!Position.HasValue && !wnd.IsLoaded)
                wnd.LocationChanged -= OnWindowLocationChanged;

            if (onlyRemove) return;

            if (!Position.HasValue && !wnd.IsLoaded)
                wnd.LocationChanged += OnWindowLocationChanged;

            void OnWindowLocationChanged(object sender, EventArgs e)
            {
                var wnd1 = (Window)sender;
                wnd1.LocationChanged -= OnWindowLocationChanged;
                Position = new Point(wnd1.Left, wnd1.Top);
            }
        }

        private void AddContentChangedEvents(object content, bool onlyRemove = false)
        {
            var dpdWidth = DependencyPropertyDescriptor.FromProperty(WidthProperty, typeof(FrameworkElement));
            var dpdHeight = DependencyPropertyDescriptor.FromProperty(HeightProperty, typeof(FrameworkElement));

            if (!(content is FrameworkElement m_Content)) return;

            // Remove events
            MovingThumb = null;

            BindingOperations.ClearBinding(this, MinWidthProperty);
            BindingOperations.ClearBinding(this, MaxWidthProperty);
            BindingOperations.ClearBinding(this, MinHeightProperty);
            BindingOperations.ClearBinding(this, MaxHeightProperty);

            dpdWidth.RemoveValueChanged(m_Content, OnWidthChanged);
            dpdHeight.RemoveValueChanged(m_Content, OnHeightChanged);

            if (onlyRemove) return;

            // Add events
            if (!Tips.AreEqual(0, m_Content.MinWidth))
                SetBinding(MinWidthProperty, new Binding("MinWidth") { Source = m_Content });
            if (!double.IsInfinity(m_Content.MaxWidth))
                SetBinding(MaxWidthProperty, new Binding("MaxWidth") { Source = m_Content });
            if (!Tips.AreEqual(0, m_Content.MinHeight))
                SetBinding(MinHeightProperty, new Binding("MinHeight") { Source = m_Content });
            if (!double.IsInfinity(m_Content.MaxHeight))
                SetBinding(MaxHeightProperty, new Binding("MaxHeight") { Source = m_Content });

            if (!double.IsNaN(m_Content.Width))
            {
                Width = m_Content.Width;
                m_Content.Width = double.NaN;
            }
            if (!double.IsNaN(m_Content.Height))
            {
                Height = m_Content.Height;
                m_Content.Height = double.NaN;
            }

            dpdWidth.AddValueChanged(m_Content, OnWidthChanged);
            dpdHeight.AddValueChanged(m_Content, OnHeightChanged);

            if (m_Content.IsLoaded)
                OnContentLoaded(m_Content, null);
            else
                m_Content.Loaded += OnContentLoaded;

            Activate();

            void OnWidthChanged(object sender, EventArgs e)
            {
                var content1 = (FrameworkElement)sender;
                if (!double.IsNaN(content1.Width))
                {
                    var resizableControl = content1.GetVisualParents().OfType<ResizableControl>().FirstOrDefault();
                    resizableControl.Width = content1.Width;
                    content1.Dispatcher.InvokeAsync(() => content1.Width = double.NaN, DispatcherPriority.Render);
                }
            }
            void OnHeightChanged(object sender, EventArgs e)
            {
                var content1 = (FrameworkElement)sender;
                if (!double.IsNaN(content1.Height))
                {
                    var resizableControl = content1.GetVisualParents().OfType<ResizableControl>().FirstOrDefault();
                    resizableControl.Height = content1.Height;
                    content1.Dispatcher.InvokeAsync(() => content1.Height = double.NaN, DispatcherPriority.Render);
                }
            }
            void OnContentLoaded(object sender, RoutedEventArgs args)
            {
                var content1 = (FrameworkElement)sender;
                content1.Loaded -= OnContentLoaded;
                MovingThumb = MovingThumb ?? content1.GetVisualChildren<Thumb>().FirstOrDefault(e => e.Name == MovingThumbName);
            }
        }

        public virtual void OnUnloaded(object sender, RoutedEventArgs e) => AddContentChangedEvents(Content, true);
    }
}
