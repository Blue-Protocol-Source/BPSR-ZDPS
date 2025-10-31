using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class HotKeyManager
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int GWLP_WNDPROC = -4;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProc")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndProc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public long x;
            public long y;
        }

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint WM_HOTKEY = 0x0312; // Hotkey message
        public const uint PM_REMOVE = 0x0001; // Remove message from queue

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static List<HotKey> RegisteredKeys = new(); // <KeyName, VirtualKeyCode>
        private static IntPtr OriginalWndProc;
        private static WndProc HotKeysWndProc;

        struct HotKey
        {
            public string Name;
            public uint VK;
            public int Id;
            public Action action;
        }

        public unsafe static void RegisterKey(string keyName, Action action, uint vk, uint modififers = 0)
        {
            HotKey? registeredKey = RegisteredKeys.Where(x => x.Name == keyName).FirstOrDefault();
            if (registeredKey != null && registeredKey.Value.Name == keyName)
            {
                UnregisterHotKey(HelperMethods.MainWindowPlatformHandleRaw, registeredKey.Value.Id);
                RegisteredKeys.Remove(registeredKey.Value);
            }

            HotKey? last = RegisteredKeys.LastOrDefault();
            int nextId = last != null ? last.Value.Id + 1 : 1;

            HotKey newKey = new HotKey()
            {
                Name = keyName,
                VK = vk,
                Id = nextId,
                action = action
            };
            if (RegisterHotKey(HelperMethods.MainWindowPlatformHandleRaw, newKey.Id, HotKeyManager.MOD_NOREPEAT | modififers, vk))
            {
                RegisteredKeys.Add(newKey);
            }
        }

        public static void UnregisterAllHotKeys()
        {
            foreach (var key in RegisteredKeys)
            {
                UnregisterHotKey(HelperMethods.MainWindowPlatformHandleRaw, key.Id);
            }
        }

        public unsafe static void SetWndProc()
        {
            HotKeysWndProc = new WndProc(HotKeyWndProc);

            IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(HotKeysWndProc);

            OriginalWndProc = SetWindowLongPtr(HelperMethods.MainWindowPlatformHandleRaw, GWLP_WNDPROC, newProcPtr);
        }

        public static IntPtr HotKeyWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                var hotKeyId = wParam.ToInt32();

                HotKey? boundKeys = RegisteredKeys.Where(x => x.Id == hotKeyId).FirstOrDefault();
                if (boundKeys != null)
                {
                    boundKeys?.action();
                }
            }

            return CallWindowProc(OriginalWndProc, hWnd, msg, wParam, lParam);
        }

        public static int ImGuiKeyToVirtualKey(Hexa.NET.ImGui.ImGuiKey key)
        {
            switch (key)
            {
                // Letters A–Z
                case Hexa.NET.ImGui.ImGuiKey.A: return 65;
                case Hexa.NET.ImGui.ImGuiKey.B: return 66;
                case Hexa.NET.ImGui.ImGuiKey.C: return 67;
                case Hexa.NET.ImGui.ImGuiKey.D: return 68;
                case Hexa.NET.ImGui.ImGuiKey.E: return 69;
                case Hexa.NET.ImGui.ImGuiKey.F: return 70;
                case Hexa.NET.ImGui.ImGuiKey.G: return 71;
                case Hexa.NET.ImGui.ImGuiKey.H: return 72;
                case Hexa.NET.ImGui.ImGuiKey.I: return 73;
                case Hexa.NET.ImGui.ImGuiKey.J: return 74;
                case Hexa.NET.ImGui.ImGuiKey.K: return 75;
                case Hexa.NET.ImGui.ImGuiKey.L: return 76;
                case Hexa.NET.ImGui.ImGuiKey.M: return 77;
                case Hexa.NET.ImGui.ImGuiKey.N: return 78;
                case Hexa.NET.ImGui.ImGuiKey.O: return 79;
                case Hexa.NET.ImGui.ImGuiKey.P: return 80;
                case Hexa.NET.ImGui.ImGuiKey.Q: return 81;
                case Hexa.NET.ImGui.ImGuiKey.R: return 82;
                case Hexa.NET.ImGui.ImGuiKey.S: return 83;
                case Hexa.NET.ImGui.ImGuiKey.T: return 84;
                case Hexa.NET.ImGui.ImGuiKey.U: return 85;
                case Hexa.NET.ImGui.ImGuiKey.V: return 86;
                case Hexa.NET.ImGui.ImGuiKey.W: return 87;
                case Hexa.NET.ImGui.ImGuiKey.X: return 88;
                case Hexa.NET.ImGui.ImGuiKey.Y: return 89;
                case Hexa.NET.ImGui.ImGuiKey.Z: return 90;

                // Numbers 0–9
                case Hexa.NET.ImGui.ImGuiKey.Key0: return 48;
                case Hexa.NET.ImGui.ImGuiKey.Key1: return 49;
                case Hexa.NET.ImGui.ImGuiKey.Key2: return 50;
                case Hexa.NET.ImGui.ImGuiKey.Key3: return 51;
                case Hexa.NET.ImGui.ImGuiKey.Key4: return 52;
                case Hexa.NET.ImGui.ImGuiKey.Key5: return 53;
                case Hexa.NET.ImGui.ImGuiKey.Key6: return 54;
                case Hexa.NET.ImGui.ImGuiKey.Key7: return 55;
                case Hexa.NET.ImGui.ImGuiKey.Key8: return 56;
                case Hexa.NET.ImGui.ImGuiKey.Key9: return 57;

                // Function keys F1–F12
                case Hexa.NET.ImGui.ImGuiKey.F1: return 112;
                case Hexa.NET.ImGui.ImGuiKey.F2: return 113;
                case Hexa.NET.ImGui.ImGuiKey.F3: return 114;
                case Hexa.NET.ImGui.ImGuiKey.F4: return 115;
                case Hexa.NET.ImGui.ImGuiKey.F5: return 116;
                case Hexa.NET.ImGui.ImGuiKey.F6: return 117;
                case Hexa.NET.ImGui.ImGuiKey.F7: return 118;
                case Hexa.NET.ImGui.ImGuiKey.F8: return 119;
                case Hexa.NET.ImGui.ImGuiKey.F9: return 120;
                case Hexa.NET.ImGui.ImGuiKey.F10: return 121;
                case Hexa.NET.ImGui.ImGuiKey.F11: return 122;
                case Hexa.NET.ImGui.ImGuiKey.F12: return 123;

                // Arrow keys
                case Hexa.NET.ImGui.ImGuiKey.LeftArrow: return 37;
                case Hexa.NET.ImGui.ImGuiKey.UpArrow: return 38;
                case Hexa.NET.ImGui.ImGuiKey.RightArrow: return 39;
                case Hexa.NET.ImGui.ImGuiKey.DownArrow: return 40;

                // Modifiers
                case Hexa.NET.ImGui.ImGuiKey.LeftShift: return 160;
                case Hexa.NET.ImGui.ImGuiKey.RightShift: return 161;
                case Hexa.NET.ImGui.ImGuiKey.LeftCtrl: return 162;
                case Hexa.NET.ImGui.ImGuiKey.RightCtrl: return 163;
                case Hexa.NET.ImGui.ImGuiKey.LeftAlt: return 164;
                case Hexa.NET.ImGui.ImGuiKey.RightAlt: return 165;

                // Common keys
                case Hexa.NET.ImGui.ImGuiKey.Enter: return 13;
                case Hexa.NET.ImGui.ImGuiKey.Escape: return 27;
                case Hexa.NET.ImGui.ImGuiKey.Space: return 32;
                case Hexa.NET.ImGui.ImGuiKey.Tab: return 9;
                case Hexa.NET.ImGui.ImGuiKey.Backspace: return 8;
                case Hexa.NET.ImGui.ImGuiKey.Delete: return 46;
                case Hexa.NET.ImGui.ImGuiKey.Insert: return 45;
                case Hexa.NET.ImGui.ImGuiKey.Home: return 36;
                case Hexa.NET.ImGui.ImGuiKey.End: return 35;
                case Hexa.NET.ImGui.ImGuiKey.PageUp: return 33;
                case Hexa.NET.ImGui.ImGuiKey.PageDown: return 34;
                case Hexa.NET.ImGui.ImGuiKey.Pause: return 19;
                case Hexa.NET.ImGui.ImGuiKey.PrintScreen: return 44;
                case Hexa.NET.ImGui.ImGuiKey.ScrollLock: return 145;

                // Numpad keys
                case Hexa.NET.ImGui.ImGuiKey.Keypad0: return 96;
                case Hexa.NET.ImGui.ImGuiKey.Keypad1: return 97;
                case Hexa.NET.ImGui.ImGuiKey.Keypad2: return 98;
                case Hexa.NET.ImGui.ImGuiKey.Keypad3: return 99;
                case Hexa.NET.ImGui.ImGuiKey.Keypad4: return 100;
                case Hexa.NET.ImGui.ImGuiKey.Keypad5: return 101;
                case Hexa.NET.ImGui.ImGuiKey.Keypad6: return 102;
                case Hexa.NET.ImGui.ImGuiKey.Keypad7: return 103;
                case Hexa.NET.ImGui.ImGuiKey.Keypad8: return 104;
                case Hexa.NET.ImGui.ImGuiKey.Keypad9: return 105;
                case Hexa.NET.ImGui.ImGuiKey.KeypadMultiply: return 106;
                case Hexa.NET.ImGui.ImGuiKey.KeypadAdd: return 107;
                case Hexa.NET.ImGui.ImGuiKey.KeypadEnter: return 108; // Numpad Enter
                case Hexa.NET.ImGui.ImGuiKey.KeypadSubtract: return 109;
                case Hexa.NET.ImGui.ImGuiKey.KeypadDecimal: return 110;
                case Hexa.NET.ImGui.ImGuiKey.KeypadDivide: return 111;

                // Punctuation and symbols
                case Hexa.NET.ImGui.ImGuiKey.Semicolon: return 186;
                case Hexa.NET.ImGui.ImGuiKey.Equal: return 187;
                case Hexa.NET.ImGui.ImGuiKey.Comma: return 188;
                case Hexa.NET.ImGui.ImGuiKey.Minus: return 189;
                case Hexa.NET.ImGui.ImGuiKey.Period: return 190;
                case Hexa.NET.ImGui.ImGuiKey.Slash: return 191;
                case Hexa.NET.ImGui.ImGuiKey.GraveAccent: return 192;
                case Hexa.NET.ImGui.ImGuiKey.LeftBracket: return 219;
                case Hexa.NET.ImGui.ImGuiKey.Backslash: return 220;
                case Hexa.NET.ImGui.ImGuiKey.RightBracket: return 221;
                case Hexa.NET.ImGui.ImGuiKey.Apostrophe: return 222;

                // Mouse buttons
                case Hexa.NET.ImGui.ImGuiKey.MouseLeft: return 1;   // VK_LBUTTON
                case Hexa.NET.ImGui.ImGuiKey.MouseRight: return 2;  // VK_RBUTTON
                case Hexa.NET.ImGui.ImGuiKey.MouseMiddle: return 4; // VK_MBUTTON
                case Hexa.NET.ImGui.ImGuiKey.MouseX1: return 5;     // VK_XBUTTON1
                case Hexa.NET.ImGui.ImGuiKey.MouseX2: return 6;     // VK_XBUTTON2

                default:
                    return 0; // Unmapped or unknown key
            }
        }

        public static ImGuiKey VirtualKeyToImGuiKey(int vk)
        {
            switch (vk)
            {
                // Basic navigation keys
                case 9: return ImGuiKey.Tab;
                case 37: return ImGuiKey.LeftArrow;
                case 39: return ImGuiKey.RightArrow;
                case 38: return ImGuiKey.UpArrow;
                case 40: return ImGuiKey.DownArrow;
                case 33: return ImGuiKey.PageUp;
                case 34: return ImGuiKey.PageDown;
                case 36: return ImGuiKey.Home;
                case 35: return ImGuiKey.End;
                case 45: return ImGuiKey.Insert;
                case 46: return ImGuiKey.Delete;
                case 8: return ImGuiKey.Backspace;
                case 32: return ImGuiKey.Space;
                case 13: return ImGuiKey.Enter;
                case 27: return ImGuiKey.Escape;

                // Punctuation and OEM
                case 188: return ImGuiKey.Comma;
                case 190: return ImGuiKey.Period;
                case 20: return ImGuiKey.CapsLock;
                case 145: return ImGuiKey.ScrollLock;
                case 144: return ImGuiKey.NumLock;
                case 44: return ImGuiKey.PrintScreen;
                case 19: return ImGuiKey.Pause;
                case 186: return ImGuiKey.Semicolon;
                case 187: return ImGuiKey.Equal;
                case 189: return ImGuiKey.Minus;
                case 191: return ImGuiKey.Slash;
                case 192: return ImGuiKey.GraveAccent;
                case 219: return ImGuiKey.LeftBracket;
                case 220: return ImGuiKey.Backslash;
                case 221: return ImGuiKey.RightBracket;
                case 222: return ImGuiKey.Apostrophe;

                // Numpad
                case 96: return ImGuiKey.Keypad0;
                case 97: return ImGuiKey.Keypad1;
                case 98: return ImGuiKey.Keypad2;
                case 99: return ImGuiKey.Keypad3;
                case 100: return ImGuiKey.Keypad4;
                case 101: return ImGuiKey.Keypad5;
                case 102: return ImGuiKey.Keypad6;
                case 103: return ImGuiKey.Keypad7;
                case 104: return ImGuiKey.Keypad8;
                case 105: return ImGuiKey.Keypad9;
                case 110: return ImGuiKey.KeypadDecimal;
                case 111: return ImGuiKey.KeypadDivide;
                case 106: return ImGuiKey.KeypadMultiply;
                case 109: return ImGuiKey.KeypadSubtract;
                case 107: return ImGuiKey.KeypadAdd;

                // Modifiers
                case 160: return ImGuiKey.LeftShift;
                case 162: return ImGuiKey.LeftCtrl;
                case 164: return ImGuiKey.LeftAlt;
                case 91: return ImGuiKey.LeftSuper;
                case 161: return ImGuiKey.RightShift;
                case 163: return ImGuiKey.RightCtrl;
                case 165: return ImGuiKey.RightAlt;
                case 92: return ImGuiKey.RightSuper;
                case 93: return ImGuiKey.Menu;

                // Number keys (top row)
                case 48: return ImGuiKey.Key0;
                case 49: return ImGuiKey.Key1;
                case 50: return ImGuiKey.Key2;
                case 51: return ImGuiKey.Key3;
                case 52: return ImGuiKey.Key4;
                case 53: return ImGuiKey.Key5;
                case 54: return ImGuiKey.Key6;
                case 55: return ImGuiKey.Key7;
                case 56: return ImGuiKey.Key8;
                case 57: return ImGuiKey.Key9;

                // Letters A–Z
                case 65: return ImGuiKey.A;
                case 66: return ImGuiKey.B;
                case 67: return ImGuiKey.C;
                case 68: return ImGuiKey.D;
                case 69: return ImGuiKey.E;
                case 70: return ImGuiKey.F;
                case 71: return ImGuiKey.G;
                case 72: return ImGuiKey.H;
                case 73: return ImGuiKey.I;
                case 74: return ImGuiKey.J;
                case 75: return ImGuiKey.K;
                case 76: return ImGuiKey.L;
                case 77: return ImGuiKey.M;
                case 78: return ImGuiKey.N;
                case 79: return ImGuiKey.O;
                case 80: return ImGuiKey.P;
                case 81: return ImGuiKey.Q;
                case 82: return ImGuiKey.R;
                case 83: return ImGuiKey.S;
                case 84: return ImGuiKey.T;
                case 85: return ImGuiKey.U;
                case 86: return ImGuiKey.V;
                case 87: return ImGuiKey.W;
                case 88: return ImGuiKey.X;
                case 89: return ImGuiKey.Y;
                case 90: return ImGuiKey.Z;

                // Function keys F1–F24
                case 112: return ImGuiKey.F1;
                case 113: return ImGuiKey.F2;
                case 114: return ImGuiKey.F3;
                case 115: return ImGuiKey.F4;
                case 116: return ImGuiKey.F5;
                case 117: return ImGuiKey.F6;
                case 118: return ImGuiKey.F7;
                case 119: return ImGuiKey.F8;
                case 120: return ImGuiKey.F9;
                case 121: return ImGuiKey.F10;
                case 122: return ImGuiKey.F11;
                case 123: return ImGuiKey.F12;
                case 124: return ImGuiKey.F13;
                case 125: return ImGuiKey.F14;
                case 126: return ImGuiKey.F15;
                case 127: return ImGuiKey.F16;
                case 128: return ImGuiKey.F17;
                case 129: return ImGuiKey.F18;
                case 130: return ImGuiKey.F19;
                case 131: return ImGuiKey.F20;
                case 132: return ImGuiKey.F21;
                case 133: return ImGuiKey.F22;
                case 134: return ImGuiKey.F23;
                case 135: return ImGuiKey.F24;

                // Browser / App navigation keys
                case 166: return ImGuiKey.AppBack;
                case 167: return ImGuiKey.AppForward;

                default:
                    break;
            }

            return ImGuiKey.None;
        }

    }
}
