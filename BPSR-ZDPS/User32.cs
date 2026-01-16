using System.Runtime.InteropServices;

namespace BPSR_ZDPS;

public class User32
{
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;
    public const int SW_MINIMIZE = 6;
    public const int SW_SHOWMINNOACTIVE = 7;
    public const int SW_SHOWNA = 8;
    public const int SW_RESTORE = 9;
    public const int SW_SHOWDEFAULT = 10;
    public const int SW_FORCEMINIMIZE = 11;
    public const int WH_MSGFILTER = -1;
    public const int WH_JOURNALRECORD = 0;
    public const int WH_JOURNALPLAYBACK = 1;
    public const int WH_KEYBOARD = 2;
    public const int WH_GETMESSAGE = 3;
    public const int WH_CALLWNDPROC = 4;
    public const int WH_CBT = 5;
    public const int WH_SYSMSGFILTER = 6;
    public const int WH_MOUSE = 7;
    public const int WH_DEBUG = 9;
    public const int WH_SHELL = 13;
    public const int WH_FOREGROUNDIDLE = 11;
    public const int WH_CALLWNDPROCRET = 12;
    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL = 14;
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int GWLP_WNDPROC = -4;
    public const long WS_EX_ACCEPTFILES = 0x00000010L;
    public const long WS_EX_APPWINDOW = 0x00040000L;
    public const long WS_EX_CLIENTEDGE = 0x00000200L;
    public const long WS_EX_COMPOSITED = 0x02000000L;
    public const long WS_EX_CONTEXTHELP = 0x00000400L;
    public const long WS_EX_CONTROLPARENT = 0x00010000L;
    public const long WS_EX_DLGMODALFRAME = 0x00000001L;
    public const long WS_EX_LAYERED = 0x00080000L;
    public const long WS_EX_LAYOUTRTL = 0x00400000L;
    public const long WS_EX_LEFT = 0x00000000L;
    public const long WS_EX_LEFTSCROLLBAR = 0x00004000L;
    public const long WS_EX_LTRREADING = 0x00000000L;
    public const long WS_EX_MDICHILD = 0x00000040L;
    public const long WS_EX_NOACTIVATE = 0x08000000L;
    public const long WS_EX_NOINHERITLAYOUT = 0x00100000L;
    public const long WS_EX_NOPARENTNOTIFY = 0x00000004L;
    public const long WS_EX_NOREDIRECTIONBITMAP = 0x00200000L;
    public const long WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
    public const long WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
    public const long WS_EX_RIGHT = 0x00001000L;
    public const long WS_EX_RIGHTSCROLLBAR = 0x00000000L;
    public const long WS_EX_RTLREADING = 0x00002000L;
    public const long WS_EX_STATICEDGE = 0x00020000L;
    public const long WS_EX_TOOLWINDOW = 0x00000080L;
    public const long WS_EX_TOPMOST = 0x00000008L;
    public const long WS_EX_TRANSPARENT = 0x00000020L;
    public const long WS_EX_WINDOWEDGE = 0x00000100L;
    public const int LWA_COLORKEY = 0x1;
    public const int LWA_ALPHA = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 4)
        {
            return GetWindowLong32(hWnd, nIndex);
        }
        return GetWindowLongPtr64(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 4)
        {
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }
        return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
}