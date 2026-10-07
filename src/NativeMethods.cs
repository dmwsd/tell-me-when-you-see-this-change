using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TellMeWhenYouSeeThisChange
{
    internal static class NativeMethods
    {
        public const uint CLR_INVALID = 0xFFFFFFFF;

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int x, int y);

        /// <summary>Reads a live screen pixel. Returns false when the screen can't be read (locked, secure desktop, etc.).</summary>
        public static bool TryReadScreenPixel(int x, int y, out Color color)
        {
            color = Color.Empty;
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return false;
            try
            {
                uint c = GetPixel(hdc, x, y);
                if (c == CLR_INVALID) return false;
                // COLORREF is 0x00BBGGRR
                color = Color.FromArgb((int)(c & 0xFF), (int)((c >> 8) & 0xFF), (int)((c >> 16) & 0xFF));
                return true;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }
    }
}
