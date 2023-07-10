using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    /// <summary>
    /// Interaction logic for PopupResizeShell.xaml
    /// </summary>
    public class PopupResizeControl : ContentControl
    {
        private static Dictionary<string, Size> _settings = new Dictionary<string, Size>();

        private const int MaxSize = 1200;
        private const int MinSize = 50;

        public string SettingId { get; set; }

        public PopupResizeControl()
        {
            Loaded += (sender, args) => AttachEvents();
            Unloaded += (sender, args) => AttachEvents(true);
        }

        private void AttachEvents(bool onlyRemove = false)
        {
            var resizeThumb = GetTemplateChild("PART_ResizeGrip") as Thumb;
            if (resizeThumb == null) return;

            resizeThumb.DragDelta -= ThumbDragDelta;
            var root = GetTemplateChild("PART_Root") as Grid;
            foreach (var thumb in root.Children.OfType<Thumb>())
                thumb.DragDelta -= ThumbDragDelta;
            if (onlyRemove) return;


            resizeThumb.DragDelta += ThumbDragDelta;
            foreach (var thumb in root.Children.OfType<Thumb>())
                thumb.DragDelta += ThumbDragDelta;

            if (!string.IsNullOrEmpty(SettingId) && _settings.ContainsKey(SettingId) && IsVisible)
            {
                if (this.GetVisualParents().OfType<Popup>().FirstOrDefault() is Popup popup)
                {
                    popup.Width = _settings[SettingId].Width;
                    popup.Height = _settings[SettingId].Height;
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (this.GetVisualParents().OfType<Popup>().FirstOrDefault() is Popup popup)
            {
                if (double.IsNaN(popup.Width)) popup.Width = ActualWidth;
                if (double.IsNaN(popup.Height)) popup.Height = ActualHeight;
                if (!double.IsNaN(Width)) Width = double.NaN;
                if (!double.IsNaN(Height)) Height = double.NaN;
                if (Content is FrameworkElement content)
                {
                    if (!double.IsNaN(content.Width)) content.Width = double.NaN;
                    if (!double.IsNaN(content.Height)) content.Height = double.NaN;
                }
            }
        }

        private void ThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb) sender;
            var popup = this.GetVisualParents().OfType<Popup>().FirstOrDefault();
            if (popup == null) return;

            if (thumb.Cursor == Cursors.SizeWE || thumb.Cursor == Cursors.SizeNWSE)
                popup.Width = Math.Min(MaxSize, Math.Max(popup.Width + e.HorizontalChange, MinSize));
            if (thumb.Cursor == Cursors.SizeNS || thumb.Cursor == Cursors.SizeNWSE)
                popup.Height = Math.Min(MaxSize, Math.Max(popup.Height + e.VerticalChange, MinSize));

            if (!string.IsNullOrEmpty(SettingId) && IsVisible)
            {
                if (!_settings.ContainsKey(SettingId))
                    _settings[SettingId] = new Size();
                _settings[SettingId] = new Size(popup.Width, popup.Height);
            }
        }

        #region ===========  Properties  ==============
        public static readonly DependencyProperty DoesContentSupportElasticLayoutProperty = DependencyProperty.Register("DoesContentSupportElasticLayout", typeof(bool), typeof(PopupResizeControl), new FrameworkPropertyMetadata(false));
        public bool DoesContentSupportElasticLayout
        {
            get => (bool)GetValue(DoesContentSupportElasticLayoutProperty);
            set => SetValue(DoesContentSupportElasticLayoutProperty, value);
        }
        #endregion
    }
}
