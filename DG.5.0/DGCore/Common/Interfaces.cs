using System.ComponentModel;

namespace DGCore.Common
{
    public interface IIsEmptySupport
    {
        // if IsEmpty will be property than it will be show in datagrid as column
        // Attribute [Browsable(false)] doesn't work (see https://stackoverflow.com/questions/31430659/why-browsablefalse-does-not-hide-columns-in-datagrid)
        bool IsEmpty();
    }

    public interface ILookupTableTypeConverter
    {
        string SqlKey { get; }
        object GetItemByKeyValue(object keyValue);
        object GetKeyByItemValue(object item);
        void LoadData(IComponent consumer);
    }

    public interface IGetValue // For DGVCube(DGVList_GroupItem).IDGVList_GroupItem
    {
        object GetValue(string propertyName);
    }

    public interface ITotalLine
    {
        string Id { get; }
        Enums.TotalFunction TotalFunction { get; set; }
    }

    public interface IMessageBox
    {
        Enums.MessageBoxResult Show(string message, string caption, Enums.MessageBoxButtons buttons, Enums.MessageBoxIcon icon);
    }

}
