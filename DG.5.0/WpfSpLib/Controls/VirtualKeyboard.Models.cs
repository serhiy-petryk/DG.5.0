using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class VirtualKeyboard
    {
        //======================================
        public class LanguageModel
        {
            public string Id { get; }
            public string Label { get; }
            public bool IsSelected { get; set; }
            public ImageSource Icon { get; }
            public RelayCommand LanguageSelectCommand { get; }

            public event EventHandler OnSelect;

            public LanguageModel(string id)
            {
                Id = id.ToUpper();
                var ci = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures).FirstOrDefault(a => a.IetfLanguageTag.ToUpper() == Id);
                if (ci != null)
                {
                    Label = ci.DisplayName == ci.NativeName ? ci.DisplayName : $"{ci.DisplayName} ({ci.NativeName})";
                    Icon = LocalizationHelper.GetRegionIcon(Id);
                }
                LanguageSelectCommand = new RelayCommand(LanguageSelectHandler);
            }
            private void LanguageSelectHandler(object p) => OnSelect?.Invoke( this, EventArgs.Empty);
        }

        //======================================
        public partial class KeyModel
        {
            private static readonly Dictionary<string, Dictionary<string, KeyModel>> _keyboardSetCache = new Dictionary<string, Dictionary<string, KeyModel>>();
            public static Dictionary<string, KeyModel> GetKeyboardSet(string language)
            {
                if (!_keyboardSetCache.ContainsKey(language))
                {
                    // Prepare base keyboard set (from english)
                    var keyboardSet = KeyDefinition.LanguageDefinitions[DefaultLanguage].Item1.ToDictionary(m => m.Id.ToString(), m => new KeyModel(m));
                    // Apply national key definitions
                    if (language != DefaultLanguage && KeyDefinition.LanguageDefinitions[language].Item1 != null)
                        foreach (var a in KeyDefinition.LanguageDefinitions[language].Item1)
                            keyboardSet[a.Id.ToString()] = new KeyModel(a);

                    // Apply key definitions for extra keys
                    foreach (var a in KeyDefinition.LanguageDefinitions[DefaultLanguage].Item2)
                        keyboardSet[a.Key.ToString()].ExtraText = ((char)a.Value).ToString();
                    // Apply national key definitions for extra keys
                    if (language != DefaultLanguage && KeyDefinition.LanguageDefinitions[language].Item2 != null)
                        foreach (var a in KeyDefinition.LanguageDefinitions[language].Item2)
                            keyboardSet[a.Key.ToString()].ExtraText = ((char)a.Value).ToString();

                    _keyboardSetCache[language] = keyboardSet;
                }
                return _keyboardSetCache[language];
            }
            //===================
            public KeyDefinition.KeyCode Id { get; }
            public string Text { get; }
            public string CapsText { get; }
            public string ShiftedText { get; }
            public string ShiftedCapsText { get; }
            public string ExtraText { get; private set; }
            public string Label { get; }
            public string BeforeText { get; }
            public string AfterText { get; }
            public bool IsCommand => Label != null;

            public KeyModel(KeyDefinition keyDefinition)
            {
                Id = keyDefinition.Id;
                Text = keyDefinition.Text;
                ShiftedText = keyDefinition.ShiftedText;
                CapsText = keyDefinition.CapsText;
                ShiftedCapsText = keyDefinition.ShiftedCapsText;
                Label = keyDefinition.Label;
                BeforeText = keyDefinition.BeforeText;
                AfterText = keyDefinition.AfterText;
            }
            public string GetKeyText(bool isCapsLock, bool isShifted, bool isExtra)
            {
                if (IsCommand) return null;
                if (isExtra) return ExtraText;
                if (!isCapsLock)
                    return isShifted ? ShiftedText : Text;
                return isShifted ? ShiftedCapsText : CapsText;
            }
        }
    }
}
