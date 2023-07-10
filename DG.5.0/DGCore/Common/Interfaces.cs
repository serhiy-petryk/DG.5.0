using System.ComponentModel;

namespace DGCore.Common
{
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
