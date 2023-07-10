using System;
using System.Collections.Generic;

namespace WpfSpLib.Controls
{
    public partial class VirtualKeyboard
    {
        public partial class KeyModel
        {
            public class KeyDefinition
            {
                public enum KeyCode
                {
                    VK_OEM_3, VK_1, VK_2, VK_3, VK_4, VK_5, VK_6, VK_7, VK_8, VK_9, VK_0, VK_OEM_MINUS, VK_OEM_PLUS, VK_BACK, // first row
                    VK_TAB, VK_Q, VK_W, VK_E, VK_R, VK_T, VK_Y, VK_U, VK_I, VK_O, VK_P, VK_OEM_4, VK_OEM_6, VK_OEM_5, // second row
                    VK_CAPITAL, VK_A, VK_S, VK_D, VK_F, VK_G, VK_H, VK_J, VK_K, VK_L, VK_OEM_1, VK_OEM_7, VK_RETURN,// thirs row
                    VK_LSHIFT, VK_Z, VK_X, VK_C, VK_V, VK_B, VK_N, VK_M, VK_OEM_COMMA, VK_OEM_PERIOD, VK_OEM_2, VK_RSHIFT, // forth row
                    VK_SPACE, VK_LEFT, VK_RIGHT, VK_DELETE, VK_EXTRA 
                }

                public static Dictionary<string, Tuple<KeyDefinition[], Dictionary<KeyCode, int>>> LanguageDefinitions;
                static KeyDefinition()
                {
                    LanguageDefinitions = new Dictionary<string, Tuple<KeyDefinition[], Dictionary<KeyCode, int>>>
                    {
                        {"EN", new Tuple<KeyDefinition[], Dictionary<KeyCode, int>>(en, extraEN)},
                        {"DE", new Tuple<KeyDefinition[], Dictionary<KeyCode, int>>(de, null)},
                        {"UK", new Tuple<KeyDefinition[], Dictionary<KeyCode, int>>(uk, null)}
                    };
                }

                // ===============  key set definitions  ====================
                private static Dictionary<KeyCode, int> extraEN = new Dictionary<KeyCode, int>
                {
                    {KeyCode.VK_OEM_3, 0x201C }, {KeyCode.VK_1, 0x201D},{KeyCode.VK_2, 0x2018 }, {KeyCode.VK_3, 0x2019},
                };

                private static KeyDefinition[] en = {
                    new KeyDefinition(KeyCode.VK_OEM_3, '`', '~', false),
                    new KeyDefinition(KeyCode.VK_1, '1', '!', false),
                    new KeyDefinition(KeyCode.VK_2, '2', '@', false),
                    new KeyDefinition(KeyCode.VK_3, '3', '#', false),
                    new KeyDefinition(KeyCode.VK_4, '4', '$', false),
                    new KeyDefinition(KeyCode.VK_5, '5', '%', false),
                    new KeyDefinition(KeyCode.VK_6, '6', '^', false),
                    new KeyDefinition(KeyCode.VK_7, '7', '&', false),
                    new KeyDefinition(KeyCode.VK_8, '8', '*', false),
                    new KeyDefinition(KeyCode.VK_9, '9', '(', false),
                    new KeyDefinition(KeyCode.VK_0, '0', ')', false),
                    new KeyDefinition(KeyCode.VK_OEM_MINUS, '-', '_', false),
                    new KeyDefinition(KeyCode.VK_OEM_PLUS, '=', '+', false),
                    new KeyDefinition(KeyCode.VK_BACK, "Backspace", 0x21E6),
                    //<!-- 2 -->
                    new KeyDefinition(KeyCode.VK_TAB, "Tab", null, 0x21B9),
                    new KeyDefinition(KeyCode.VK_Q, 'q', 'Q'),
                    new KeyDefinition(KeyCode.VK_W, 'w', 'W'),
                    new KeyDefinition(KeyCode.VK_E, 'e', 'E'),
                    new KeyDefinition(KeyCode.VK_R, 'r', 'R'),
                    new KeyDefinition(KeyCode.VK_T, 't', 'T'),
                    new KeyDefinition(KeyCode.VK_Y, 'y', 'Y'),
                    new KeyDefinition(KeyCode.VK_U, 'u', 'U'),
                    new KeyDefinition(KeyCode.VK_I, 'i', 'I'),
                    new KeyDefinition(KeyCode.VK_O, 'o', 'O'),
                    new KeyDefinition(KeyCode.VK_P, 'p', 'P'),
                    new KeyDefinition(KeyCode.VK_OEM_4, '[', '{', false),
                    new KeyDefinition(KeyCode.VK_OEM_6, ']', '}', false),
                    new KeyDefinition(KeyCode.VK_OEM_5, '\\', '|', false),
                    //<!-- 3 -->
                    new KeyDefinition(KeyCode.VK_CAPITAL, "Caps Lock", 0x21EA),
                    new KeyDefinition(KeyCode.VK_A, 'a', 'A'),
                    new KeyDefinition(KeyCode.VK_S, 's', 'S'),
                    new KeyDefinition(KeyCode.VK_D, 'd', 'D'),
                    new KeyDefinition(KeyCode.VK_F, 'f', 'F'),
                    new KeyDefinition(KeyCode.VK_G, 'g', 'G'),
                    new KeyDefinition(KeyCode.VK_H, 'h', 'H'),
                    new KeyDefinition(KeyCode.VK_J, 'j', 'J'),
                    new KeyDefinition(KeyCode.VK_K, 'k', 'K'),
                    new KeyDefinition(KeyCode.VK_L, 'l', 'L'),
                    new KeyDefinition(KeyCode.VK_OEM_1, ';', ':', false),
                    new KeyDefinition(KeyCode.VK_OEM_7, '\'', '"', false),
                    new KeyDefinition(KeyCode.VK_RETURN, "Enter", null, 0x23CE),
                    //<!-- 4 -->
                    new KeyDefinition(KeyCode.VK_LSHIFT, "Shift", 0x21E7),
                    new KeyDefinition(KeyCode.VK_Z, 'z', 'Z'),
                    new KeyDefinition(KeyCode.VK_X, 'x', 'X'),
                    new KeyDefinition(KeyCode.VK_C, 'c', 'C'),
                    new KeyDefinition(KeyCode.VK_V, 'v', 'V'),
                    new KeyDefinition(KeyCode.VK_B, 'b', 'B'),
                    new KeyDefinition(KeyCode.VK_N, 'n', 'N'),
                    new KeyDefinition(KeyCode.VK_M, 'm', 'M'),
                    new KeyDefinition(KeyCode.VK_OEM_COMMA, ',', '<', false),
                    new KeyDefinition(KeyCode.VK_OEM_PERIOD, '.', '>', false),
                    new KeyDefinition(KeyCode.VK_OEM_2, '/', '?', false),
                    new KeyDefinition(KeyCode.VK_RSHIFT, "Shift", 0x21E7),
                    new KeyDefinition(KeyCode.VK_DELETE, "Del", null),
                    // <!-- -->
                    new KeyDefinition(KeyCode.VK_EXTRA, "Extra", null ), //0x2A01),
                    new KeyDefinition(KeyCode.VK_SPACE, ' ', ' ', false),
                    new KeyDefinition(KeyCode.VK_LEFT, "←", null),
                    new KeyDefinition(KeyCode.VK_RIGHT, "→", null)
                };

                private static KeyDefinition[] de = {
                    new KeyDefinition(KeyCode.VK_OEM_3, '^', '°', false),
                    new KeyDefinition(KeyCode.VK_1, '1', '!', '!','1'),
                    new KeyDefinition(KeyCode.VK_2, '2', '"','"','2'),
                    new KeyDefinition(KeyCode.VK_3, '3', '§', '§', '3'),
                    new KeyDefinition(KeyCode.VK_4, '4', '$', '$', '4'),
                    new KeyDefinition(KeyCode.VK_5, '5', '%', '%', '5' ),
                    new KeyDefinition(KeyCode.VK_6, '6', '&', '&', '6'),
                    new KeyDefinition(KeyCode.VK_7, '7', '/', '/', '7'),
                    new KeyDefinition(KeyCode.VK_8, '8', '(', '(', '8'),
                    new KeyDefinition(KeyCode.VK_9, '9', ')', ')', '9'),
                    new KeyDefinition(KeyCode.VK_0, '0', '=', '=', '0'),
                    new KeyDefinition(KeyCode.VK_OEM_MINUS, 'ß', '?', '?', 'ß'),
                    new KeyDefinition(KeyCode.VK_OEM_PLUS, '´', '`', false),
                    //<!-- 2 -->
                    new KeyDefinition(KeyCode.VK_Y, 'z', 'Z'),
                    new KeyDefinition(KeyCode.VK_OEM_4,  'ü', 'Ü'),
                    new KeyDefinition(KeyCode.VK_OEM_6, '+', '*', '*', '+'),
                    new KeyDefinition(KeyCode.VK_OEM_5, '#', '\'', false),
                    //<!-- 3 -->
                    new KeyDefinition(KeyCode.VK_OEM_1,  'ö', 'Ö'),
                    new KeyDefinition(KeyCode.VK_OEM_7,  'ä', 'Ä'),
                    //<!-- 4 -->
                    new KeyDefinition(KeyCode.VK_LSHIFT, "Shift", 0x21E7),
                    new KeyDefinition(KeyCode.VK_Z, 'y', 'Y'),
                    new KeyDefinition(KeyCode.VK_OEM_COMMA, ',', ';', ';', ','),
                    new KeyDefinition(KeyCode.VK_OEM_PERIOD, '.', ':', ':', '.'),
                    new KeyDefinition(KeyCode.VK_OEM_2, '-', '_', false),
                };

                private static KeyDefinition[] uk = {
                    new KeyDefinition(KeyCode.VK_OEM_3, '\'', '₴', false),
                    new KeyDefinition(KeyCode.VK_2, '2', '"', false),
                    new KeyDefinition(KeyCode.VK_3, '3', '№', false),
                    new KeyDefinition(KeyCode.VK_4, '4', ';', false),
                    new KeyDefinition(KeyCode.VK_6, '6', ':', false),
                    new KeyDefinition(KeyCode.VK_7, '7', '?', false),
                    //<!-- 2 -->
                    new KeyDefinition(KeyCode.VK_Q, 'й', 'Й'),
                    new KeyDefinition(KeyCode.VK_W, 'ц', 'Ц'),
                    new KeyDefinition(KeyCode.VK_E, 'у', 'У'),
                    new KeyDefinition(KeyCode.VK_R, 'к', 'К'),
                    new KeyDefinition(KeyCode.VK_T, 'е', 'Е'),
                    new KeyDefinition(KeyCode.VK_Y, 'н', 'Н'),
                    new KeyDefinition(KeyCode.VK_U, 'г', 'Г'),
                    new KeyDefinition(KeyCode.VK_I, 'ш', 'Ш'),
                    new KeyDefinition(KeyCode.VK_O, 'щ', 'Щ'),
                    new KeyDefinition(KeyCode.VK_P, 'з', 'З'),
                    new KeyDefinition(KeyCode.VK_OEM_4, 'х', 'Х'),
                    new KeyDefinition(KeyCode.VK_OEM_6, 'ї', 'Ї'),
                    new KeyDefinition(KeyCode.VK_OEM_5, '\\', '/', false),
                    //<!-- 3 -->
                    new KeyDefinition(KeyCode.VK_A, 'ф', 'Ф'),
                    new KeyDefinition(KeyCode.VK_S, 'і', 'І'),
                    new KeyDefinition(KeyCode.VK_D, 'в', 'В'),
                    new KeyDefinition(KeyCode.VK_F, 'а', 'А'),
                    new KeyDefinition(KeyCode.VK_G, 'п', 'П'),
                    new KeyDefinition(KeyCode.VK_H, 'р', 'Р'),
                    new KeyDefinition(KeyCode.VK_J, 'о', 'О'),
                    new KeyDefinition(KeyCode.VK_K, 'л', 'Л'),
                    new KeyDefinition(KeyCode.VK_L, 'д', 'Д'),
                    new KeyDefinition(KeyCode.VK_OEM_1, 'ж', 'Ж'),
                    new KeyDefinition(KeyCode.VK_OEM_7, 'є', 'Є'),
                    //<!-- 4 -->
                    new KeyDefinition(KeyCode.VK_Z, 'я', 'Я'),
                    new KeyDefinition(KeyCode.VK_X, 'ч', 'Ч'),
                    new KeyDefinition(KeyCode.VK_C, 'с', 'С'),
                    new KeyDefinition(KeyCode.VK_V, 'м', 'М'),
                    new KeyDefinition(KeyCode.VK_B, 'и', 'И'),
                    new KeyDefinition(KeyCode.VK_N, 'т', 'Т'),
                    new KeyDefinition(KeyCode.VK_M, 'ь', 'Ь'),
                    new KeyDefinition(KeyCode.VK_OEM_COMMA, 'б', 'Б'),
                    new KeyDefinition(KeyCode.VK_OEM_PERIOD, 'ю', 'Ю'),
                    new KeyDefinition(KeyCode.VK_OEM_2, '.', ',', false),
                };

                //===============================================
                #region ============   Instance   ===========

                public readonly KeyCode Id;
                public string Text;
                public string CapsText;
                public string ShiftedText;
                public string ShiftedCapsText;
                public readonly string Label; // For command key
                public readonly string BeforeText; // For command key
                public readonly string AfterText; // For command key

                public KeyDefinition(KeyCode id, char mainChar, char shiftedOrCapsChar, bool isLetter = true)
                {
                    Id = id;
                    if (isLetter)
                    {
                        Text = mainChar.ToString();
                        ShiftedText = shiftedOrCapsChar.ToString(); ;
                        CapsText = ShiftedText;
                        ShiftedCapsText = Text;
                    }
                    else
                    {
                        Text = mainChar.ToString();
                        CapsText = Text;
                        ShiftedText = shiftedOrCapsChar.ToString();
                        ShiftedCapsText = ShiftedText;
                    }
                }
                public KeyDefinition(KeyCode id, char mainChar, char shiftedChar, char capsChar, char shiftedCapsChar)
                {
                    Id = id;
                    Text = mainChar.ToString();
                    ShiftedText = shiftedChar.ToString();
                    CapsText = capsChar.ToString();
                    ShiftedCapsText = shiftedCapsChar.ToString();
                }
                public KeyDefinition(KeyCode id, string label, int? beforeChar, int? afterChar = null)
                {
                    Id = id;
                    Label = label ?? "";
                    BeforeText = beforeChar.HasValue ? ((char)beforeChar.Value).ToString() : null;
                    AfterText = afterChar.HasValue ? ((char)afterChar.Value).ToString() : null;
                }
                #endregion
            }
        }
    }
}
