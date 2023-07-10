using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public static class Misc
    {
        // Filter helper for datagrid
        public static bool SetFilter(string text, string filterText)
        {
            if ((filterText ?? "") == "") return true;
            var words = filterText.ToLowerInvariant().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length; i++)
                words[i] = words[i].Trim();

            text = (text ?? "").ToLowerInvariant();
            return words.All(word => text.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public static ImageSource GetImageSourceFromGeometry(Geometry geometry, Brush brush, Pen pen)
        {
            var geometryDrawing = new GeometryDrawing(brush, pen, geometry);
            return new DrawingImage { Drawing = geometryDrawing };
        }

        public static void OpenDGDialog(DataGrid dataGrid, FrameworkElement dialogView, string title, Geometry icon)
        {
            var owner = dataGrid.GetVisualParents().OfType<MwiChild>().FirstOrDefault();
            var host = owner.GetDialogHost();
            var transforms = host.GetActualLayoutTransforms();
            var width = Math.Max(200, Window.GetWindow(host).ActualWidth * 2 / 3 / transforms.Value.M11);
            var height = Math.Max(200, Window.GetWindow(host).ActualHeight * 2 / 3 / transforms.Value.M22);
            OpenMwiDialog(host, dialogView, title, icon, (child, adorner) =>
            {
                child.Height = height;
                child.Width = width;
                child.Theme = owner.ActualTheme;
                child.ThemeColor = owner.ActualThemeColor;
            });
        }

        public static void OpenMwiDialog(FrameworkElement host, FrameworkElement dialogContent, string title, Geometry icon, Action<MwiChild, DialogAdorner> beforeShowDialogAction)
        {
            var width = dialogContent.Width;
            var height = dialogContent.Height;
            if (!double.IsNaN(dialogContent.Width)) dialogContent.Width = double.NaN;
            if (!double.IsNaN(dialogContent.Height)) dialogContent.Height = double.NaN;

            var content = new MwiChild
            {
                Content = dialogContent,
                LimitPositionToPanelBounds = true,
                Title = title,
                VisibleButtons = MwiChild.Buttons.Close | MwiChild.Buttons.Maximize,
                IsActive = true
            };

            // Migrate valid width/height values from dialogContent to host
           if (!double.IsNaN(width)) content.Width = width;
           if (!double.IsNaN(height)) content.Height = height;

            // var adorner = new DialogAdorner(_owner.DialogHost) { CloseOnClickBackground = true };
            var adorner = new DialogAdorner(host) { CloseOnClickBackground = true };
            beforeShowDialogAction?.Invoke(content, adorner);
            if (icon != null)
            {
                var brush = (Brush)ColorHslBrush.Instance.Convert(content.ActualThemeColor, typeof(Brush), "+50%", null);
                content.Icon = GetImageSourceFromGeometry(icon, brush, null);
            }
            adorner.ShowContentDialog(content);
        }
    }
}
