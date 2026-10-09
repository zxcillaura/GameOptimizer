using System.Diagnostics;
using System.Linq;

namespace GameOptimizer.Core
{
    public static class AntiCheatMonitor
    {
        public static bool IsVanguardRunning()
        {
            return Process.GetProcessesByName("vgtray").Any();
        }

        public static bool IsFaceitRunning()
        {
            return Process.GetProcessesByName("faceit").Any();
        }
    }
}