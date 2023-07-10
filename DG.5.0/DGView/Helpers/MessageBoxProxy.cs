using System;
using DGCore.Common;
using WpfSpLib.Controls;

namespace DGView.Helpers
{
    public class MessageBoxProxy : IMessageBox
    {
        public Enums.MessageBoxResult Show(string message, string caption, Enums.MessageBoxButtons buttons, Enums.MessageBoxIcon icon)
        {
            string[] buttons2 = null;
            switch (buttons)
            {
                case Enums.MessageBoxButtons.OK:
                    buttons2 = new[] { "OK" };
                    break;
                case Enums.MessageBoxButtons.OKCancel:
                    buttons2 = new[] { "OK", "Cancel" };
                    break;
                case Enums.MessageBoxButtons.YesNoCancel:
                    buttons2 = new[] { "Yes", "No", "Cancel" };
                    break;
                case Enums.MessageBoxButtons.YesNo:
                    buttons2 = new[] { "Yes", "No" };
                    break;
            }

            DialogBox.DialogBoxKind? dialogKind = null;
            if (Enum.TryParse<DialogBox.DialogBoxKind>(icon.ToString(), out var icon21))
                dialogKind = icon21;

            var result2 = new DialogBox(dialogKind)
            {
                Caption = caption,
                Message = message,
                Buttons = buttons2
            }.ShowDialog();

            if (Enum.TryParse<Enums.MessageBoxResult>(result2, out var result))
                return result;
            return Enums.MessageBoxResult.None;
        }
    }
}
