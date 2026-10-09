using System;
using System.Diagnostics;
using System.Text;

namespace GameOptimizer.Utils
{
    /// <summary>Thin, always-quoted-safe process helper used by every v6 module.</summary>
    public static class ProcessRunner
    {
        public static (int code, string output) Run(string file, string args, int timeoutMs = 60000)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8
                };

                using var p = Process.Start(psi);
                if (p == null) return (-1, string.Empty);

                var sb = new StringBuilder();
                var outTask = p.StandardOutput.ReadToEndAsync();
                var errTask = p.StandardError.ReadToEndAsync();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(true); } catch { }
                    return (-2, sb.ToString() + "\n[timeout]");
                }

                sb.Append(outTask.Result);
                string err = errTask.Result;
                if (!string.IsNullOrWhiteSpace(err)) sb.Append('\n').Append(err);
                return (p.ExitCode, sb.ToString());
            }
            catch (Exception ex)
            {
                return (-1, ex.Message);
            }
        }

        /// <summary>Runs a PowerShell snippet. Returns combined output.</summary>
        public static (int code, string output) PowerShell(string script, int timeoutMs = 120000)
        {
            // The preamble removes CLIXML progress noise and forces UTF-8 output
            // so Cyrillic from PowerShell survives the pipe intact.
            const string preamble =
                "$ProgressPreference='SilentlyContinue';" +
                "[Console]::OutputEncoding=[Text.Encoding]::UTF8;" +
                "$ErrorActionPreference='Continue';";

            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(preamble + script));
            return Run("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encoded}",
                timeoutMs);
        }

        public static bool RunElevated(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args)
                {
                    UseShellExecute = true,
                    Verb            = "runas",
                    CreateNoWindow  = true,
                    WindowStyle     = ProcessWindowStyle.Hidden
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(60000);
                return true;
            }
            catch { return false; }
        }
    }
}
