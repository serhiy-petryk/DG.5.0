using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Common.ColorSpaces;
using WpfSpLib.Helpers;

namespace WpfSpLib.Themes
{
    public class MwiThemeInfo: NotifyPropertyChangedAbstract
    {
        private static string _currentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        public static Dictionary<string, MwiThemeInfo> Themes = new Dictionary<string, MwiThemeInfo>
        {
            {"Windows7", new MwiThemeInfo(ColorUtils.StringToColor("#FFBBD2EB"),null, new[] { $"pack://application:,,,/{_currentAssemblyName};component/Themes/Mwi.Wnd7.xaml" })},
            {"Windows10", new MwiThemeInfo(null, null, new[] { $"pack://application:,,,/{_currentAssemblyName};component/Themes/Mwi.Wnd10.xaml"})},
            {"Windows10-2", new MwiThemeInfo(null,null, new[] { $"pack://application:,,,/{_currentAssemblyName};component/Themes/Mwi.Wnd10.WithBorders.xaml"})}
        };
        public static MwiThemeInfo DefaultTheme => Themes.First().Value;
        public static Color DefaultThemeColor => (Color) Application.Current.Resources["PrimaryColor"];
        // public static MwiThemeInfo DefaultTheme => Themes["Windows10-2"];

        public static MwiThemeInfo GetNexThemeInfo(MwiThemeInfo current)
        {
            var themes = Themes.Values.ToList();
            var k = themes.IndexOf(current);
            if (k > themes.Count - 2)
                k = -1;

            var newTheme = themes[k + 1];
            return newTheme;
        }

        // ==============   Static section  ======================
        private static Dictionary<string, Assembly> _assemblyCache = new Dictionary<string, Assembly>();

        private static Assembly GetAssembly(string assemblyName)
        {
            var curAsm = Assembly.GetExecutingAssembly();
            if (!_assemblyCache.ContainsKey(assemblyName))
            {
                var resource = assemblyName;
                using (var stm = curAsm.GetManifestResourceStream(resource))
                {
                    var ba = new byte[(int)stm.Length];
                    stm.Read(ba, 0, (int)stm.Length);
                    _assemblyCache.Add(assemblyName, Assembly.Load(ba));
                }
            }
            return _assemblyCache[assemblyName];
        }

        // ==============   Instance section  ======================
        public string Id => Themes.Where(kvp => kvp.Value == this).Select(kvp => kvp.Key).FirstOrDefault();
        public string Name => Application.Current.Resources[$"$ThemeInfo.Name.{Id}"].ToString();
        public Color? FixedColor { get; }
        private string _assemblyName;
        private string[] _uris;
        public MwiThemeInfo(Color? fixedColor, string assemblyName, string[] uris)
        {
            FixedColor = fixedColor;
            _assemblyName = assemblyName;
            _uris = uris;
            LocalizationHelper.RegionChanged += (sender, args) => UpdateUI();
        }

        public ResourceDictionary[] GetResources()
        {
            if (!string.IsNullOrEmpty(_assemblyName))
            {
                var assembly = GetAssembly(_assemblyName);
                var uris = _uris.Select(uri => new Uri("/" + assembly.GetName().Name + ";component/" + uri, UriKind.Relative));
                return uris.Select(uri => Application.LoadComponent(uri) as ResourceDictionary).ToArray();
            }

            if (_uris != null)
                return _uris.Select(uri => new ResourceDictionary { Source = new Uri(uri, UriKind.RelativeOrAbsolute) }).ToArray();
            return new ResourceDictionary[0];
        }

        public override void UpdateUI()
        {
            OnPropertiesChanged(nameof(Name));
        }

        public override string ToString() => Name;

    }
}
