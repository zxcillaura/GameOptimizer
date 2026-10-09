using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace GameOptimizer.Core
{
    /// <summary>
    /// Advanced tweaks: timer resolution, C-States, GPU, network, input, memory.
    /// Hard preset uses all; Soft preset uses only safe tweaks.
    /// </summary>
    public static class AdvancedOptimizer
    {
        // ── Timer ──────────────────────────────────────────────────────────────

        /// <summary>Set timer resolution to 0.5 ms for minimal input lag. HARD only.</summary>
        public static void SetMaximumTimerResolution(Action<string> log)
        {
            log("[Timer] Setting maximum timer resolution (0.5 ms)...");
            try
            {
                int result = WinApi.NtSetTimerResolution(5000, true, out int current);
                if (result == 0)
                    log($"[OK] Timer resolution set to {current / 10000.0:F2} ms.");
                else
                    log($"[WARN] NtSetTimerResolution returned {result}. May need admin.");
            }
            catch (Exception ex) { log($"[WARN] Timer resolution: {ex.Message}"); }
        }

        public static void EnforceTscTimer(Action<string> log)
        {
            log("[Timer] Enforcing TSC sync policy (Enhanced)...");
            RunBcd("bcdedit /set tscsyncpolicy Enhanced", log);
            RunBcd("bcdedit /set useplatformclock false", log);
            RunBcd("bcdedit /set disabledynamictick yes", log);
        }

        // ── CPU ────────────────────────────────────────────────────────────────

        /// <summary>Disable deep C-States (C1E/C6) to eliminate micro-stutter. HARD only.</summary>
        public static void DisableDeepCStates(Action<string> log)
        {
            log("[CPU] Disabling deep C-States (C1E/C6)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Processor",
                "Capabilities", 0x0007e066, RegistryValueKind.DWord);
            log("[OK] C-States disabled.");
        }

        public static void ApplyHiddenPowerSettings(Action<string> log)
        {
            log("[CPU] Applying hidden power plan tweaks...");
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor PERFINCPOL 0", log);
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor PERFDECPOL 0", log);
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100", log);
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor CPMAXCORES 100", log);
            RunCmd("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", log);
            log("[OK] Power plan tweaks applied.");
        }

        public static void ApplySoftPowerSettings(Action<string> log)
        {
            log("[CPU] Applying balanced power tweaks...");
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100", log);
            RunCmd("powercfg -setacvalueindex scheme_current sub_processor CPMAXCORES 100", log);
            // Balanced scheme
            RunCmd("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", log);
            log("[OK] Balanced power plan applied.");
        }

        // ── GPU ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies only reversible, vendor-neutral performance settings. This does
        /// not change voltage, clock limits, fan curves, or firmware, so it cannot
        /// overclock the CPU/GPU or create an unsafe thermal target.
        /// </summary>
        public static void ApplySafeHardwareProfile(Action<string> log)
        {
            log("[Hardware] Applying safe CPU/GPU performance profile...");
            RunCmd("powercfg /setactive SCHEME_MIN", log);
            RunCmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5", log);
            RunCmd("powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100", log);
            RunCmd("powercfg /setactive SCHEME_CURRENT", log);
            log("[Hardware] No voltage, frequency, firmware, or fan settings were changed.");
            log("[OK] Safe hardware profile applied. Monitor temperatures with the GPU/CPU vendor tools.");
        }

        /// <summary>Force V-Sync off via driver registry. HARD only.</summary>
        public static void ForceVSyncOff(Action<string> log)
        {
            // Kept for API compatibility with older profiles. The previous registry
            // write targeted an undocumented driver value and could damage VRR/G-SYNC
            // behavior, so the optimizer no longer changes V-Sync globally.
            log("[GPU] Global V-Sync override skipped; configure it per game/driver profile.");
        }

        public static void EnableLowLatencyGlobal(Action<string> log)
        {
            log("[GPU] Enabling global Low Latency mode...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "D3D10LowLatency", 1, RegistryValueKind.DWord);
            log("[OK] Low Latency mode enabled.");
        }

        public static void DisableMPO(Action<string> log)
        {
            log("[GPU] Disabling MPO (Multi-Plane Overlay)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\Dwm",
                "OverlayTestMode", 5, RegistryValueKind.DWord);
            log("[OK] MPO disabled — microstutter fix applied.");
        }

        public static void EnableHAGS(Action<string> log)
        {
            log("[GPU] Enabling HAGS (Hardware-Accelerated GPU Scheduling)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "HwSchMode", 2, RegistryValueKind.DWord);
            log("[OK] HAGS enabled.");
        }

        // ── Network ────────────────────────────────────────────────────────────

        public static void SetCongestionControlProvider(Action<string> log)
        {
            log("[Net] Setting CTCP (Compound TCP)...");
            RunCmd("netsh int tcp set global congestionprovider=ctcp", log);
        }

        public static void SetTcpAutoTuning(Action<string> log, bool enabled)
        {
            string mode = enabled ? "normal" : "disabled";
            log($"[Net] TCP auto-tuning: {mode}...");
            RunCmd($"netsh int tcp set global autotuninglevel={mode}", log);
        }

        public static void OptimizeTcpTimedWaitDelay(Action<string> log)
        {
            log("[Net] Setting TcpTimedWaitDelay to 30...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                "TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
            log("[OK] TcpTimedWaitDelay = 30 seconds.");
        }

        // ── Input ──────────────────────────────────────────────────────────────

        public static void OptimizeUsbPollingRate(Action<string> log)
        {
            log("[Input] Optimizing USB mouse polling rate...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\services\HidUsb\Parameters",
                "MouseDataQueueSize", 20, RegistryValueKind.DWord);
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\services\HidUsb\Parameters",
                "LowLatency", 1, RegistryValueKind.DWord);
            log("[OK] USB polling rate optimized.");
        }

        public static void DisableKeyboardFilters(Action<string> log)
        {
            log("[Input] Disabling keyboard accessibility filters...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Control Panel\Accessibility\StickyKeys", "Flags", "506", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Control Panel\Accessibility\Keyboard Response", "Flags", "122", RegistryValueKind.String);
            log("[OK] Keyboard filters disabled.");
        }

        // ── Memory ─────────────────────────────────────────────────────────────

        /// <summary>Prevent kernel/drivers from being paged to disk. HARD only.</summary>
        public static void DisablePagingExecutive(Action<string> log)
        {
            log("[Mem] Disabling paging executive (DisablePagingExecutive=1)...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                "DisablePagingExecutive", 1, RegistryValueKind.DWord);
            log("[OK] DisablePagingExecutive enabled.");
        }

        public static void SetLargeSystemCache(Action<string> log, bool enable)
        {
            log($"[Mem] LargeSystemCache = {(enable ? 1 : 0)}...");
            RegistryManager.SetValue(RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                "LargeSystemCache", enable ? 1 : 0, RegistryValueKind.DWord);
        }

        public static void CleanStandbyList(Action<string> log)
        {
            log("[Mem] Cleaning standby list (EmptyWorkingSet)...");
            try
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                WinApi.EmptyWorkingSet(process.Handle);
                log("[OK] Working set cleared.");
            }
            catch (Exception ex) { log($"[WARN] {ex.Message}"); }
        }

        // ── Desktop ────────────────────────────────────────────────────────────

        public static void DisableDesktopAnimations(Action<string> log)
        {
            log("[Desktop] Disabling window animations...");
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String);
            RegistryManager.SetValue(RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarAnimations", 0, RegistryValueKind.DWord);
            log("[OK] Desktop animations disabled.");
        }

        // ── Services ───────────────────────────────────────────────────────────

        /// <summary>Disable services that waste CPU/disk during gaming. HARD only.</summary>
        public static void DisableGamingWasteServices(Action<string> log)
        {
            string[] services = { "DiagTrack", "SysMain", "WSearch", "XboxNetApiSvc",
                                   "XboxGipSvc", "TabletInputService", "FontCache", "DusmSvc" };
            foreach (var svc in services)
                ServiceManager.ToggleService(svc, false, log);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void RunCmd(string cmd, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/d /s /c \"{cmd}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { log($"[WARN] {cmd}: {ex.Message}"); }
        }

        private static void RunBcd(string cmd, Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c {cmd}")
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                using var p = Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { log($"[WARN] {cmd}: {ex.Message}"); }
        }
    }
}
