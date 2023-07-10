using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace WpfSpLib.Helpers
{
    public class LocalizationHelper
    {
        public static ImageSource GetLanguageIcon(string IetfLanguageTag)
        {
            var key = $"LanguageIcon_{IetfLanguageTag.ToUpper()}";
            return Application.Current.Resources.Contains(key) ? Application.Current.Resources[key] as ImageSource : null;
        }

        public static event EventHandler LanguageChanged;

        public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        public static CultureInfo CurrentCulture = Thread.CurrentThread.CurrentUICulture;

        private static readonly MethodInfo _miDatePickerRefresh = typeof(DatePicker).GetMethod("SetSelectedDate", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void SetLanguage(CultureInfo newCulture)
        {
            // if (Equals(newLanguage, Thread.CurrentThread.CurrentUICulture)) return;

            Thread.CurrentThread.CurrentUICulture = newCulture;
            Thread.CurrentThread.CurrentCulture = newCulture;
            // CultureInfo.DefaultThreadCurrentCulture = newCulture;
            // CultureInfo.DefaultThreadCurrentUICulture = newCulture;

            foreach (var rd in GetLocalizedResourceDictionaries(newCulture))
                FillResources(rd);

            CurrentCulture = newCulture;
            Application.Current.Resources["CurrentLanguage"] = XmlLanguage.GetLanguage(newCulture.IetfLanguageTag);

            // Update current date pickers, numericboxes, ..
            foreach (var wnd in Application.Current.Windows.OfType<Window>())
            {
                wnd.Language = XmlLanguage.GetLanguage(newCulture.IetfLanguageTag);

                foreach (var dp in wnd.GetVisualChildren().OfType<DatePicker>())
                    _miDatePickerRefresh.Invoke(dp, null);

                // var focusedControl = FocusManager.GetFocusedElement(wnd);
                // foreach (var dp in wnd.GetVisualChildren().OfType<DatePicker>())
                //    dp.Focus();
                // FocusManager.SetFocusedElement(wnd, focusedControl);
            }

            LanguageChanged?.Invoke(Application.Current, new EventArgs());
        }

        #region ===========  Private methods  =============
        private static ResourceDictionary[] GetLocalizedResourceDictionaries(CultureInfo culture)
        {
            var dictionaries = new List<ResourceDictionary>();
            var cultureName = culture.Name.ToLower();
            var resourceName = String.IsNullOrEmpty(cultureName) || cultureName.StartsWith("en")
                ? "resources/lang.xaml"
                : $"resources/lang.{culture.Name.ToLower()}.xaml";

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies.Where(a => !a.IsDynamic))
            {
                var resName = asm.GetName().Name + ".g.resources";
                using (var stream = asm.GetManifestResourceStream(resName))
                    if (stream != null)
                        using (var reader = new ResourceReader(stream))
                            dictionaries.AddRange(reader.OfType<DictionaryEntry>()
                                .Where(kvp => ((string) kvp.Key).ToLower() == resourceName)
                                .Select(kvp => new ResourceDictionary
                                {
                                    Source = new Uri("/" + asm.GetName().Name + ";component/" + (string)kvp.Key, UriKind.Relative)
                                }).ToArray());
            }
            return dictionaries.ToArray();
        }

        private static void FillResources(ResourceDictionary resources)
        {
            foreach (var rd in resources.MergedDictionaries)
                FillResources(rd);
            foreach (var key in resources.Keys.OfType<string>())
                Application.Current.Resources[key] = resources[key];
        }
        #endregion
    }
}
