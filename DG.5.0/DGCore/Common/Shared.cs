using System.Diagnostics;

namespace DGCore.Common
{
    public static class Shared
    {
        public static IMessageBox MessageBoxProxy;

        public static Enums.MessageBoxResult ShowMessage(string message, string caption, Enums.MessageBoxButtons buttons, Enums.MessageBoxIcon icon)
        {
            if (MessageBoxProxy == null)
            {
                Debug.Print($"MESSAGEBOX: {message}");
                switch (buttons)
                {
                    case Enums.MessageBoxButtons.OK: return Enums.MessageBoxResult.OK;
                    case Enums.MessageBoxButtons.OKCancel: return Enums.MessageBoxResult.Cancel;
                }
                return Enums.MessageBoxResult.No;
            }
            return MessageBoxProxy.Show(message, caption, buttons, icon);
        }
    }
}
