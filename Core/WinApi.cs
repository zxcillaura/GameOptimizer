using System;
using System.Runtime.InteropServices;

namespace GameOptimizer.Core
{
    public static class WinApi
    {
        // Timer Resolution
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSetTimerResolution(int DesiredResolution, bool SetResolution, out int CurrentResolution);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryTimerResolution(out int MinimumResolution, out int MaximumResolution, out int CurrentResolution);

        // Memory Management
        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        // Get current timer resolution in ms
        public static double GetCurrentTimerResolutionMs()
        {
            try
            {
                if (NtQueryTimerResolution(out int min, out int max, out int cur) != 0)
                    return -1;
                return cur / 10000.0;
            }
            catch { return -1; }
        }

        // DWM rounded corners (Windows 11+; safely ignored on older builds)
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        public static void ApplyRoundedCorners(IntPtr hwnd)
        {
            try
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { }
        }
    }
}
