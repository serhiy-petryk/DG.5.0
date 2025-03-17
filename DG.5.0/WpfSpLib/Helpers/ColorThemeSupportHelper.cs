using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Themes;

namespace WpfSpLib.Helpers
{
    public interface IColorThemeSupport
    {
        MwiThemeInfo Theme { get; set; }
        Color? ThemeColor { get; set; }
        MwiThemeInfo ActualTheme { get; }
        Color ActualThemeColor { get; }
        IColorThemeSupport ColorThemeParent { get; }
    }

    public static class ColorThemeSupportHelper
    {
        public static MwiThemeInfo GetActualTheme(this IColorThemeSupport obj)
        {
            if (obj.Theme != null) return obj.Theme;
            return obj.ColorThemeParent?.ActualTheme ?? MwiThemeInfo.DefaultTheme;
        }

        public static Color GetActualThemeColor(this IColorThemeSupport obj)
        {
            if (obj.ActualTheme.FixedColor != null) return obj.ActualTheme.FixedColor.Value;
            if (obj.ThemeColor.HasValue) return obj.ThemeColor.Value;
            return obj.ColorThemeParent?.ActualThemeColor ?? MwiThemeInfo.DefaultThemeColor;
        }

        public static void SelectTheme(this IColorThemeSupport obj, FrameworkElement host = null)
        {
            var themeSelector = new ThemeSelector {Target = obj};
            var mwiChild = new MwiChild
            {
                Content = themeSelector,
                Width = 784,
                Height = 600,
                MinWidth = 700,
                MinHeight = 500,
                LimitPositionToPanelBounds = false,
                Title = (string)Application.Current.Resources["$ThemeSelector"],
                VisibleButtons = MwiChild.Buttons.Close | MwiChild.Buttons.Maximize
            };

            var adorner = new DialogAdorner(host) { CloseOnClickBackground = true };
            if (adorner.Host.ActualWidth < mwiChild.Width || adorner.Host.ActualHeight < mwiChild.Height)
                mwiChild.WindowState = WindowState.Maximized;

            mwiChild.SetBinding(MwiChild.ThemeProperty, new Binding("ActualTheme") { Source = themeSelector });
            mwiChild.SetBinding(MwiChild.ThemeColorProperty, new Binding("ActualThemeColor") { Source = themeSelector, Converter = ColorHslBrush.Instance });

            adorner.ShowContentDialog(mwiChild);
        }
    }
}
