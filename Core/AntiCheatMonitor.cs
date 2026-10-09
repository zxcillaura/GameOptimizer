using System.Diagnostics;

namespace GameOptimizer.Core
{
    public static class AntiCheatMonitor
    {
        public static bool IsVanguardRunning()
            => Process.GetProcessesByName("vgtray").Length > 0
            || Process.GetProcessesByName("vgc").Length > 0;

        public static bool IsFaceitRunning()
            => Process.GetProcessesByName("faceit").Length > 0
            || Process.GetProcessesByName("FACEITClient").Length > 0;
    }
}
