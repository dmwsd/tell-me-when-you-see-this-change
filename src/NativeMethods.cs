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

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        public const uint FLASHW_STOP = 0;
        public const uint FLASHW_ALL = 3;
        public const uint FLASHW_TIMERNOFG = 12;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

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

        public static void Flash(IntPtr hwnd, bool on)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO));
            fi.hwnd = hwnd;
            fi.dwFlags = on ? (FLASHW_ALL | FLASHW_TIMERNOFG) : FLASHW_STOP;
            fi.uCount = uint.MaxValue;
            fi.dwTimeout = 0;
            FlashWindowEx(ref fi);
        }
    }
}
