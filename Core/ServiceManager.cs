using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace GameOptimizer.Core
{
    public static class ServiceManager
    {
        public static bool IsServiceRunning(string serviceName)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch { return false; }
        }

        public static bool ServiceExists(string serviceName)
        {
            try
            {
                using var sc = new ServiceController(serviceName);
                _ = sc.Status;
                return true;
            }
            catch { return false; }
        }

        /// <summary>Returns "RUNNING" | "STOPPED" | "NOT_INSTALLED" | "ERROR"</summary>
        public static string GetServiceStatus(string serviceName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"query {serviceName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return "ERROR";
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (output.Contains("RUNNING")) return "RUNNING";
                if (output.Contains("STOPPED")) return "STOPPED";
                if (output.Contains("1060") || output.Contains("does not exist")) return "NOT_INSTALLED";
                return "STOPPED";
            }
            catch { return "ERROR"; }
        }

        public static void ToggleService(string serviceName, bool start, Action<string> log)
        {
            string action = start ? "start" : "stop";
            string disable = start ? "" : " & sc config " + serviceName + " start= disabled";
            log($"-> {(start ? "Starting" : "Stopping")} service: {serviceName}...");
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c sc {action} {serviceName}{disable}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using var p = Process.Start(psi);
                p?.WaitForExit();
                log($"[OK] {action} command sent for {serviceName}.");
            }
            catch (Exception ex)
            {
                log($"[ERROR] {ex.Message}");
            }
        }
    }
}
