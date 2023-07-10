using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DGCore.Misc
{

    public class TotalLine : Common.ITotalLine
    {
        #region ==========  Static section  ==============
        public static bool IsTypeSupport(Type t) => Utils.Types.IsNumericType(t);

        public static void ApplySettings(IEnumerable<TotalLine> target, IEnumerable<Common.ITotalLine> source)
        {
            foreach (TotalLine tl1 in target)
            {
                if (source.FirstOrDefault(o => o.Id == tl1.Id) is Common.ITotalLine tl2)
                    tl1.TotalFunction = tl2.TotalFunction;
                else
                    tl1.TotalFunction = Common.Enums.TotalFunction.None;
            }
        }
        #endregion

        // PropertyDescriptor _pd;// can be null; before work with pd you need to activate it (use PropertyDescriptor property set)
        // Common.Enums.TotalFunction _totalFunction = Common.Enums.TotalFunction.None;

        // Valid only for AverageFunction
        private int? _dpTotals = null;

        public TotalLine() { }

        public TotalLine(PropertyDescriptor pd)
        {
            Id = pd.Name;
            PropertyDescriptor = pd;
        }

        [Browsable(false)]
        public string Id { get; }
        public string DisplayName => PropertyDescriptor.DisplayName; // For label in totals of datagridview (DGWnd project)
        public Common.Enums.TotalFunction TotalFunction { get; set; }
        [Browsable(false)]
        public PropertyDescriptor PropertyDescriptor { get; set; }
        public UserSettings.TotalLine ToSettingsTotalLine() => new UserSettings.TotalLine { Id = Id, TotalFunction = TotalFunction };
        public override string ToString() => Id;
    }

}




