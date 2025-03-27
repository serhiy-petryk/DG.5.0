using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public class RegionMenuItem
    {
        public static Dictionary<string, RegionMenuItem> RegionMenuItems =
            new(System.StringComparer.OrdinalIgnoreCase)
            {
                { "EN-US", new RegionMenuItem("en-US") }, { "EN-GB", new RegionMenuItem("en-GB") },
                { "UK-UA", new RegionMenuItem("uk-UA") }
            };

        //========================
        public CultureInfo Culture { get; }
        public string Label { get; }
        public ImageSource Icon { get; }
        public bool IsSelected => string.Equals(LocalizationHelper.CurrentCulture.IetfLanguageTag, Culture.IetfLanguageTag);
        public RelayCommand CmdSetRegion { get; }

        public RegionMenuItem(string id)
        {
            Culture = new CultureInfo(id ?? "");
            Label = Culture.DisplayName + (Culture.DisplayName == Culture.NativeName ? "" : $" ({Culture.NativeName})");
            Icon = LocalizationHelper.GetRegionIcon(Culture.IetfLanguageTag);
            CmdSetRegion = new RelayCommand(o => LocalizationHelper.SetRegion(Culture), o => !IsSelected);
        }
    }
}
