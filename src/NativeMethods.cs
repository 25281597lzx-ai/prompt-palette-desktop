using System;
using System.Runtime.InteropServices;

namespace PromptPalette
{
    internal static class NativeMethods
    {
        public const int WM_NCHITTEST = 0x0084;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int bar, bool show);

        public static void UseDarkScrollbar(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;
            try { SetWindowTheme(handle, "DarkMode_Explorer", null); }
            catch { }
        }

        public static void HideVerticalScrollBar(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;
            try { ShowScrollBar(handle, 1, false); }
            catch { }
        }

        public static void ApplyWindowChrome(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;
            try
            {
                int rounded = 2;
                DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
                int dark = 0;
                DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
            }
            catch
            {
                // Older Windows versions simply keep square corners.
            }
        }
    }
}
