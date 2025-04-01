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
        private static readonly string[] CultureList =
            { "en-AU", "en-CA", "en-IN", "en-IE", "en-NZ", "en-SG", "en-GB", "en-US", "uk-UA" };

        private static readonly Dictionary<string, RegionMenuItem> _regionMenuItems = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures).Where(a => CultureList.Contains(a.IetfLanguageTag))
            .OrderBy(a => a.DisplayName).ToDictionary(a => a.IetfLanguageTag,
                a => new RegionMenuItem(a.IetfLanguageTag), System.StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<string, RegionMenuItem> RegionMenuItems = _regionMenuItems;

        //========================
        public CultureInfo Culture { get; }
        public string Label { get; }
        public ImageSource Icon { get; }
        public bool IsSelected => string.Equals(LocalizationHelper.CurrentCulture.IetfLanguageTag, Culture.IetfLanguageTag);
        public RelayCommand CmdSetRegion { get; }
        public RegionInfo Region { get; }

        public RegionMenuItem(string id)
        {
            Culture = new CultureInfo(id ?? "");
            Label = Culture.EnglishName +
                    (Misc.MyCalculateSimilarity(Culture.EnglishName, Culture.NativeName) > 0.5
                        ? ""
                        : $" ({Culture.NativeName})")+ " " + id;
            Icon = LocalizationHelper.GetRegionIcon(Culture.IetfLanguageTag);
            Region = new RegionInfo(id);
            CmdSetRegion = new RelayCommand(o => LocalizationHelper.SetRegion(Culture), o => !IsSelected);
        }
    }
}
