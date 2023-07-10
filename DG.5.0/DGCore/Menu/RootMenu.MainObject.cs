using System.Collections.Generic;
using System.Text.Json;

namespace DGCore.Menu
{
    public partial class RootMenu
    {
        public class MainObject
        {
            public string Title { get; set; }
            public Dictionary<string, DbConnection> DbConnections { get; set; }
            public Dictionary<string, Lookup> Lookups { get; set; }
            public Dictionary<string, object> Menu { get; set; }
            internal List<MenuOption> FlatMenu { get; } = new List<MenuOption>();

            public void Normalize()
            {
                SetFlatMenu();
                Title = Title?.Trim();

                foreach (var o in Lookups.Values)
                    o.Normalize(this);

                foreach (var mo in FlatMenu)
                    mo.Normalize(this);
            }

            //=======================================
            private void SetFlatMenu()
            {
                FlatMenu.Clear();
                foreach (var kvp in Menu)
                    SetFlatMenuRecursive(null, kvp);
            }

            private void SetFlatMenuRecursive(MenuOption parent, KeyValuePair<string, object> kvp)
            {
                // var mo = ((Newtonsoft.Json.Linq.JObject) kvp.Value).ToObject<MenuOption>();
                var mo = JsonSerializer.Deserialize<MenuOption>(((JsonElement)kvp.Value).GetRawText(), Utils.Json.DefaultJsonOptions);
                Utils.Json.ConvertJsonElements(mo);
                mo.Label = kvp.Key;
                mo.ParentId = parent?.Id;
                FlatMenu.Add(mo);
                if (!mo.IsSubmenu) return;
                // Submenu
                // var o2 = ((Newtonsoft.Json.Linq.JObject) kvp.Value).ToObject<Dictionary<string, object>>();
                var o2 = JsonSerializer.Deserialize<Dictionary<string, object>>(((JsonElement)kvp.Value).GetRawText(), Utils.Json.DefaultJsonOptions);
                Utils.Json.ConvertJsonElements(o2);
                foreach (var kvp1 in o2)
                    SetFlatMenuRecursive(mo, kvp1);
            }
        }
    }
}
