using System.Runtime.InteropServices;

namespace BPSR_ZDPS;

public class User32
{
    public static readonly IntPtr HWND_TOPMOST   = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    public const           uint   SWP_NOMOVE     = 0x0002;
    public const           uint   SWP_NOSIZE     = 0x0001;
    public const           uint   SWP_SHOWWINDOW = 0x0040;
    public const           int    SW_MINIMIZE    = 6;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}