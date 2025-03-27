using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public class LanguageMenuItem
    {
        public static Dictionary<string, LanguageMenuItem> LanguageMenuItems =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                { "EN-US", new LanguageMenuItem("en-US") }, { "EN-GB", new LanguageMenuItem("en-GB") },
                { "UK-UA", new LanguageMenuItem("uk-UA") }
            };

        //========================
        public CultureInfo Culture { get; }
        public string Label { get; }
        public ImageSource Icon { get; }
        public bool IsSelected => string.Equals(LocalizationHelper.CurrentCulture.IetfLanguageTag, Culture.IetfLanguageTag);
        public RelayCommand CmdSetLanguage { get; }

        public LanguageMenuItem(string id)
        {
            Culture = new CultureInfo(id ?? "");
            Label = Culture.DisplayName + (Culture.DisplayName == Culture.NativeName ? "" : $" ({Culture.NativeName})");
            Icon = LocalizationHelper.GetLanguageIcon(Culture.IetfLanguageTag);
            CmdSetLanguage = new RelayCommand(o => LocalizationHelper.SetLanguage(Culture), o => !IsSelected);
        }
    }
}
