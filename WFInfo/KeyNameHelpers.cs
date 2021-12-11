using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace WFInfo
{
    public class KeyNameHelpers
    {
        // Black magic below - blame: https://stackoverflow.com/a/5826175

        //[DllImport("user32.dll")]
        //public static extern bool GetKeyboardState(byte[] lpKeyState);
        public static string GetKeyName(Key key)
        {
            char temp = GetCharFromKey(key);
            switch (key)
            {
                case Key.OemTilde:
                    return "Tilde";
                case Key.Return:
                    return "Enter";
                case Key.Next:
                    return "PageDown";
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                    return key.ToString();
                case Key.Decimal:
                    return "NumpadDot";
                case Key.Add:
                case Key.Subtract:
                case Key.Multiply:
                case Key.Divide:
                    return "NumPad" + key.ToString().Substring(0, 3);
            }
            if (temp > ' ')
                return temp.ToString().ToUpper();
            return key.ToString();
        }

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            //  Disabled to avoid Shifted variants   EX: Shift + \ => |
            //  But we don't care about the character, we just want the key
            //  So ignore they current keyboard state
            //GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                default:
                {
                    ch = stringBuilder[0];
                    break;
                }
            }
            return ch;
        }
    }
}