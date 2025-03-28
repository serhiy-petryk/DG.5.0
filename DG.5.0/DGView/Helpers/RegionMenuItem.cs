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
        private static readonly Dictionary<string, RegionMenuItem> _regionMenuItems = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Where(a => !a.IsNeutralCulture && a.IetfLanguageTag.StartsWith("en-") || a.IetfLanguageTag.StartsWith("ca-") || a.IetfLanguageTag.StartsWith("uk-"))
            .OrderBy(a => a.DisplayName).ToDictionary(a => a.IetfLanguageTag,
                a => new RegionMenuItem(a.IetfLanguageTag), System.StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, RegionMenuItem> RegionMenuItems = _regionMenuItems;

        //========================
        public CultureInfo Culture { get; }
        public string Label { get; }
        public ImageSource Icon { get; }
        public bool IsSelected => string.Equals(LocalizationHelper.CurrentCulture.IetfLanguageTag, Culture.IetfLanguageTag);
        public RelayCommand CmdSetRegion { get; }

        public RegionMenuItem(string id)
        {
            Culture = new CultureInfo(id ?? "");
            Label = Culture.DisplayName +
                    (Misc.MyCalculateSimilarity(Culture.DisplayName, Culture.NativeName) > 0.5
                        ? ""
                        : $" ({Culture.NativeName})");
            Icon = LocalizationHelper.GetRegionIcon(Culture.IetfLanguageTag);
            CmdSetRegion = new RelayCommand(o => LocalizationHelper.SetRegion(Culture), o => !IsSelected);
        }
    }
}
