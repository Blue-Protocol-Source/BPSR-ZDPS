using System.Runtime.InteropServices;

namespace BPSR_ZDPS
{
    public class Gdi32
    {
        public const uint BI_RGB = 0;
        public const uint DIB_RGB_COLORS = 0;
        public const uint SRCCOPY = 0x00CC0020;

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);
    }
}
