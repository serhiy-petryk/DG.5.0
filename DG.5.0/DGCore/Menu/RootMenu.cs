using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DGCore.Common;

namespace DGCore.Menu
{
    public enum DbProvider
    {
        SqlClient,
        MySqlClient,
        OleDb,
        File
    }

    public partial class RootMenu : SubMenu
    {
        private MainObject _mainObject;

        public string ApplicationTitle => _mainObject.Title;

        public RootMenu() : base("Root")
        {
            var jsonFileName = Misc.AppSettings.CONFIG_FILE_NAME;
            Init(jsonFileName);
        }

        private void Init(string jsonFileName)
        {
            try
            {
                _mainObject = JsonSerializer.Deserialize<MainObject>(File.ReadAllText(jsonFileName), Utils.Json.DefaultJsonOptions);
                Utils.Json.ConvertJsonElements(_mainObject);
            }
            catch (Exception ex)
            {
                throw new LoadJsonConfigException(Path.GetFullPath(jsonFileName), ex);
            }

            _mainObject.Normalize();

            var isFirstConnectionString = true;
            foreach (var kvp in _mainObject.DbConnections)
            {
                var cs = kvp.Value.Provider + ";" + kvp.Value.CS.Trim();
                DB.DbCmd._standardConnections.Add(kvp.Key, cs);
                if (isFirstConnectionString)
                {
                    Misc.AppSettings.settingsStorage = cs;
                    isFirstConnectionString = false;
                }
            }

            var menuItems = new Dictionary<int, SubMenu>();
            foreach (var mo in _mainObject.FlatMenu)
            {
                if (mo.IsSubmenu)
                {
                    var item = new SubMenu(mo.Label);
                    menuItems.Add(mo.Id, item);
                    if (mo.ParentId.HasValue) // parent is root
                        menuItems[mo.ParentId.Value].Items.Add(item);
                    else
                        Items.Add(item);
                }
                else
                {
                    if (mo.ParentId.HasValue)
                        menuItems[mo.ParentId.Value].Items.Add(mo);
                    else
                        Items.Add(mo);
                }
            }
        }

    }
}
