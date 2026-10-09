using System;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using GameOptimizer.Core.Models;

namespace GameOptimizer.Core.Diagnostics
{
    public class ACDiagnostic
    {
        public CheckResult CheckTPM()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\TPM");
                if (key != null)
                {
                    var val = key.GetValue("Enabled");
                    if (val != null && Convert.ToInt32(val) == 1)
                        return OK("TPM 2.0 enabled", "TPM is working correctly");
                }
                return Error("TPM 2.0 disabled",
                    "To enable TPM:\n1. Reboot -> BIOS/UEFI\n2. Security -> Intel PTT (Intel) or AMD fTPM (AMD) -> Enabled\n3. Save (F10)");
            }
            catch (Exception ex)
            {
                return Error($"TPM error: {ex.Message}", "TPM module may be absent.");
            }
        }

        public CheckResult CheckSecureBoot()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                if (key != null)
                {
                    var val = key.GetValue("UEFISecureBootEnabled");
                    if (val != null && Convert.ToInt32(val) == 1)
                        return OK("Secure Boot enabled", "Secure Boot is working correctly");
                }
                return Error("Secure Boot disabled",
                    "To enable Secure Boot:\n1. BIOS/UEFI -> Boot or Security\n2. Secure Boot -> Enabled\n3. Save (F10)");
            }
            catch (Exception ex)
            {
                return Error($"Secure Boot error: {ex.Message}", "System may not support Secure Boot.");
            }
        }

        public CheckResult CheckVBS()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
                if (key != null)
                {
                    var val = key.GetValue("EnableVirtualizationBasedSecurity");
                    if (val != null && Convert.ToInt32(val) == 1)
                        return OK("VBS enabled", "Virtualization Based Security is active");
                }
                return Warn("VBS disabled (required by some anti-cheats)",
                    "To enable VBS:\n1. Windows Security -> Device Security\n2. Core isolation -> Memory integrity -> On\n3. Reboot");
            }
            catch (Exception ex)
            {
                return Warn($"VBS error: {ex.Message}", "Could not read VBS settings.");
            }
        }

        public CheckResult CheckHyperV()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = "/enum",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return Warn("Hyper-V: query error", "");
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                bool on = output.Contains("hypervisorlaunchtype    Auto", StringComparison.OrdinalIgnoreCase)
                       || output.Contains("hypervisorlaunchtype    On", StringComparison.OrdinalIgnoreCase);

                return on
                    ? OK("Hyper-V enabled", "Hyper-V is active")
                    : Warn("Hyper-V disabled", "Run: bcdedit /set hypervisorlaunchtype auto — then reboot");
            }
            catch (Exception ex)
            {
                return Warn($"Hyper-V error: {ex.Message}", "");
            }
        }

        public CheckResult CheckHVCI()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
                if (key != null)
                {
                    var val = key.GetValue("Enabled");
                    if (val != null && Convert.ToInt32(val) == 1)
                        return Warn("HVCI enabled (may block FACEIT)",
                            "Disable Memory Integrity: Windows Security -> Device Security -> Core isolation -> Memory integrity -> Off -> Reboot");
                }
                return OK("HVCI disabled", "Memory Integrity is OFF — FACEIT can run");
            }
            catch
            {
                return OK("HVCI: status unknown", "Could not read HVCI value.");
            }
        }

        public (CheckResult tpm, CheckResult secureBoot, CheckResult vbs, CheckResult hyperV, CheckResult hvci) CheckAll()
            => (CheckTPM(), CheckSecureBoot(), CheckVBS(), CheckHyperV(), CheckHVCI());

        /// <summary>FACEIT-specific diagnostics: HVCI, Hyper-V, driver integrity, VBS.</summary>
        public CheckResult[] RunFaceitDiagnostics()
        {
            return new[]
            {
                WrapCheck("HVCI (Memory Integrity)", CheckHVCI),
                WrapCheck("Hyper-V",                 CheckHyperV),
                WrapCheck("VBS",                     CheckVBS),
                WrapCheck("Secure Boot",             CheckSecureBoot),
                WrapCheck("TPM 2.0",                 CheckTPM),
                CheckDriverSignatures(),
                CheckFaceitInstalled(),
            };
        }

        /// <summary>Vanguard-specific diagnostics: TPM, SecureBoot, VBS, Hyper-V, HVCI.</summary>
        public CheckResult[] RunVanguardDiagnostics()
        {
            return new[]
            {
                WrapCheck("TPM 2.0",       CheckTPM),
                WrapCheck("Secure Boot",   CheckSecureBoot),
                WrapCheck("VBS",           CheckVBS),
                WrapCheck("Hyper-V",       CheckHyperV),
                WrapCheck("HVCI",          CheckHVCI),
                CheckVanguardService(),
            };
        }

        private CheckResult CheckDriverSignatures()
        {
            try
            {
                var psi = new ProcessStartInfo("bcdedit", "/enum")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                if (p == null) return Warn("Driver Signature: query failed", "");
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                bool testSigning = output.Contains("testsigning           Yes", StringComparison.OrdinalIgnoreCase);
                return testSigning
                    ? Error("Test signing ON — FACEIT may block this",
                            "Run: bcdedit /set testsigning off — then reboot")
                    : OK("Driver signatures enforced", "No unsigned drivers detected");
            }
            catch (Exception ex) { return Warn($"Driver signature check: {ex.Message}", ""); }
        }

        private CheckResult CheckFaceitInstalled()
        {
            bool running = Process.GetProcessesByName("faceit").Length > 0
                        || Process.GetProcessesByName("FACEITClient").Length > 0;
            return running
                ? OK("FACEIT Client: running", "Anti-cheat is active")
                : Warn("FACEIT Client: not running",
                       "Start the FACEIT client before launching the game");
        }

        private CheckResult CheckVanguardService()
        {
            try
            {
                using var sc = new ServiceController("vgc");
                _ = sc.Status;
                bool running = sc.Status == ServiceControllerStatus.Running;
                return running
                    ? OK("Vanguard service (vgc): running", "")
                    : Warn("Vanguard service (vgc): not running",
                           "Open Valorant or start vgc service manually");
            }
            catch
            {
                return Error("Vanguard service (vgc): not installed",
                             "Install Riot Vanguard by launching Valorant");
            }
        }

        private static CheckResult WrapCheck(string label, Func<CheckResult> fn)
        {
            try
            {
                var r = fn();
                r.Message = $"{label}: {r.Message}";
                return r;
            }
            catch (Exception ex) { return Warn($"{label}: {ex.Message}", ""); }
        }

        private static CheckResult OK(string msg, string instr)    => new() { Status = CheckStatus.Ok,      Message = msg, Instruction = instr };
        private static CheckResult Warn(string msg, string instr)  => new() { Status = CheckStatus.Warning, Message = msg, Instruction = instr };
        private static CheckResult Error(string msg, string instr) => new() { Status = CheckStatus.Error,   Message = msg, Instruction = instr };
    }
}
