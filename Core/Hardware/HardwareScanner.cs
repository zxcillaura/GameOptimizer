using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Hardware
{
    public sealed class RigReport
    {
        public string Cpu        { get; set; } = "не определён";
        public string Gpu        { get; set; } = "не определена";
        public string GpuDriver  { get; set; } = "?";
        public string GpuDriverDate { get; set; } = "?";
        public string Os         { get; set; } = "не определена";
        public string Display    { get; set; } = "?";
        public string RamSummary { get; set; } = "?";
        public string NicSummary { get; set; } = "?";
        public string DiskSummary { get; set; } = "?";

        public int  RamSticks      { get; set; }
        public bool SingleChannel  { get; set; }
        public int  RamSpeedMhz    { get; set; }
        public long RamTotalGb     { get; set; }
        public bool VbsRunning     { get; set; }
        public bool HvciRunning    { get; set; }
        public bool HyperVEnabled  { get; set; }
        public bool HagsEnabled    { get; set; }
        public bool ReBarEnabled   { get; set; }
        public string ActivePowerPlan { get; set; } = "?";
        public bool NicLinkedAt100Mbit { get; set; }

        // ── 1.0: кросс-железный профиль ──────────────────────────────
        public string CpuVendor  { get; set; } = "Unknown";   // Intel / AMD
        public string GpuVendor  { get; set; } = "Unknown";   // NVIDIA / AMD / Intel
        public int    CpuCores   { get; set; }
        public bool   IsLaptop   { get; set; }
        public bool   HasSsd     { get; set; }
        public bool   HasNvme    { get; set; }
        public int    WinMajor   { get; set; }                // 10 / 11
        public bool   IsLtsc     { get; set; }
        public int    HealthScore { get; set; } = -1;         // 0-100

        public string PowerPlanGuid { get; set; } = string.Empty;

        public List<string> Findings { get; set; } = new();
        public List<string> Actions  { get; set; } = new();
        public List<string> Errors   { get; set; } = new();

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("──────────────────────────────────────────────────────────");
            sb.AppendLine("  ДИАГНОСТИКА СИСТЕМЫ — Optimization by zxcillaura");
            sb.AppendLine("──────────────────────────────────────────────────────────");
            if (HealthScore >= 0) sb.AppendLine($"  Оценка системы : {HealthScore}/100");
            sb.AppendLine($"  Процессор      : {Cpu} [{CpuVendor}]");
            sb.AppendLine($"  Видеокарта     : {Gpu} [{GpuVendor}]");
            sb.AppendLine($"  Драйвер GPU    : {GpuDriver}  ({GpuDriverDate})");
            sb.AppendLine($"  Память         : {RamSummary}");
            sb.AppendLine($"  Монитор        : {Display}");
            sb.AppendLine($"  Система        : {Os}");
            sb.AppendLine($"  Накопители     : {DiskSummary}");
            sb.AppendLine($"  Сеть           : {NicSummary}");
            sb.AppendLine("──────────────────────────────────────────────────────────");
            sb.AppendLine($"  VBS            : {(VbsRunning ? "включён (защита Windows; влияние на игры зависит от системы)" : "выключен")}");
            sb.AppendLine($"  Memory Integrity: {(HvciRunning ? "ВКЛЮЧЁН" : "выключен")}");
            sb.AppendLine($"  Hyper-V        : {(HyperVEnabled ? "включён" : "выключен")}");
            sb.AppendLine($"  HAGS           : {(HagsEnabled ? "включён" : "выключен")}");
            sb.AppendLine($"  Resizable BAR  : {(ReBarEnabled ? "включён" : "не подтверждён")}");
            sb.AppendLine($"  Схема питания  : {ActivePowerPlan}");
            sb.AppendLine("──────────────────────────────────────────────────────────");

            if (Findings.Count > 0)
            {
                sb.AppendLine("  ЧТО ОГРАНИЧИВАЕТ ПРОИЗВОДИТЕЛЬНОСТЬ:");
                foreach (var f in Findings) sb.AppendLine($"   ! {f}");
                sb.AppendLine();
            }
            if (Actions.Count > 0)
            {
                sb.AppendLine("  ЧТО ДЕЛАТЬ:");
                foreach (var a in Actions) sb.AppendLine($"   > {a}");
            }
            if (Errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  НЕ УДАЛОСЬ ПРОЧИТАТЬ (не влияет на работу твиков):");
                foreach (var e in Errors) sb.AppendLine($"   - {e}");
            }
            return sb.ToString();
        }
    }

    public static class HardwareScanner
    {
        public static RigReport Scan(Action<string> log = null)
        {
            var r = new RigReport();

            // ── CPU ──────────────────────────────────────────────────────
            try
            {
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var o in s.Get())
                {
                    string nm = o["Name"]?.ToString() ?? "";
                    r.Cpu = $"{nm} | {o["NumberOfCores"]} ядер / {o["NumberOfLogicalProcessors"]} потоков";
                    int.TryParse(o["NumberOfCores"]?.ToString() ?? "0", out int cores);
                    r.CpuCores = cores;
                    string mf = (o["Manufacturer"]?.ToString() ?? "") + " " + nm;
                    if (mf.Contains("Intel", StringComparison.OrdinalIgnoreCase)) r.CpuVendor = "Intel";
                    else if (mf.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                             mf.Contains("Ryzen", StringComparison.OrdinalIgnoreCase) ||
                             mf.Contains("Authentic", StringComparison.OrdinalIgnoreCase)) r.CpuVendor = "AMD";
                    break;
                }
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── GPU + display ────────────────────────────────────────────
            try
            {
                bool gpuSet = false;
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (var o in s.Get())
                {
                    string name = o["Name"]?.ToString() ?? "";
                    if (name.Length == 0) continue;
                    if (name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.Contains("Remote Display", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!gpuSet)
                    {
                        r.Gpu = name;
                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GTX", StringComparison.OrdinalIgnoreCase)) r.GpuVendor = "NVIDIA";
                        else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) r.GpuVendor = "AMD";
                        else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                                 name.Contains("Arc", StringComparison.OrdinalIgnoreCase)) r.GpuVendor = "Intel";
                        r.GpuDriver = o["DriverVersion"]?.ToString() ?? "?";
                        string raw = o["DriverDate"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(raw) && raw.Length >= 8)
                            r.GpuDriverDate = $"{raw.Substring(6, 2)}.{raw.Substring(4, 2)}.{raw.Substring(0, 4)}";
                        else r.GpuDriverDate = "?";
                        gpuSet = true;
                    }

                    // display mode lives on the adapter that actually drives the panel
                    var h = o["CurrentHorizontalResolution"];
                    var v = o["CurrentVerticalResolution"];
                    var hz = o["CurrentRefreshRate"];
                    if (h != null && v != null && Convert.ToInt64(h, CultureInfo.InvariantCulture) > 0)
                        r.Display = $"{h}x{v} @ {hz} Гц";
                }
            }
            catch (Exception ex) { r.Errors.Add($"видеокарта: {ex.Message}"); }

            // ── RAM (channel detection) ──────────────────────────────────
            try
            {
                var sticks = new List<string>();
                long total = 0; int speed = 0;
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                foreach (var o in s.Get())
                {
                    long cap = Convert.ToInt64(o["Capacity"] ?? 0L, CultureInfo.InvariantCulture);
                    total += cap;
                    int sp = 0;
                    int.TryParse(o["ConfiguredClockSpeed"]?.ToString() ?? "0", out sp);
                    if (sp == 0) int.TryParse(o["Speed"]?.ToString() ?? "0", out sp);
                    if (sp > speed) speed = sp;
                    sticks.Add($"{o["DeviceLocator"]}: {cap / 1024 / 1024 / 1024} ГБ @ {sp} МГц " +
                               $"{o["Manufacturer"]} {o["PartNumber"]}".Trim());
                }
                r.RamSticks  = sticks.Count;
                r.RamSpeedMhz = speed;
                r.RamTotalGb = total / 1024 / 1024 / 1024;
                r.SingleChannel = sticks.Count < 2;
                r.RamSummary = $"{total / 1024 / 1024 / 1024} ГБ, планок: {sticks.Count}, {speed} МГц " +
                               (r.SingleChannel ? "— ОДНОКАНАЛЬНЫЙ РЕЖИМ" : "— двухканальный режим");
                foreach (var st in sticks) r.RamSummary += $"\n                   {st}";
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── OS ───────────────────────────────────────────────────────
            try
            {
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (var o in s.Get())
                {
                    string dv = Reg.Read(RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion")?.ToString();
                    string caption = o["Caption"]?.ToString() ?? "";
                    r.Os = $"{caption} | build {o["BuildNumber"]} {(string.IsNullOrWhiteSpace(dv) ? "" : dv)}".Trim();
                    int.TryParse(o["BuildNumber"]?.ToString() ?? "0", out int build);
                    r.WinMajor = build >= 22000 ? 11 : 10;
                    r.IsLtsc = caption.Contains("LTSC", StringComparison.OrdinalIgnoreCase) ||
                               caption.Contains("LTSB", StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── disks ────────────────────────────────────────────────────
            try
            {
                var parts = new List<string>();
                using var s = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3");
                foreach (var o in s.Get())
                {
                    long size = Convert.ToInt64(o["Size"] ?? 0L, CultureInfo.InvariantCulture);
                    long free = Convert.ToInt64(o["FreeSpace"] ?? 0L, CultureInfo.InvariantCulture);
                    if (size == 0) continue;
                    parts.Add($"{o["DeviceID"]} {size / 1024 / 1024 / 1024}ГБ (свободно {free / 1024 / 1024 / 1024}ГБ)");
                }
                r.DiskSummary = string.Join("  ", parts);
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── network ──────────────────────────────────────────────────
            try
            {
                var (_, nic) = ProcessRunner.PowerShell(
                    "Get-NetAdapter | Where-Object Status -eq 'Up' | " +
                    "ForEach-Object { \"$($_.Name) [$($_.InterfaceDescription)] $($_.LinkSpeed)\" }");
                r.NicSummary = nic.Replace("\r", "").Trim();
                r.NicLinkedAt100Mbit = nic.Contains("100 Mbps", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── VBS / HVCI ───────────────────────────────────────────────
            try
            {
                using var s = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\DeviceGuard", "SELECT * FROM Win32_DeviceGuard");
                foreach (var o in s.Get())
                {
                    int status = Convert.ToInt32(o["VirtualizationBasedSecurityStatus"] ?? 0, CultureInfo.InvariantCulture);
                    r.VbsRunning = status == 2;
                    var running = o["SecurityServicesRunning"] as int[];
                    r.HvciRunning = running != null && running.Contains(2);
                    break;
                }
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            try
            {
                var hv = Reg.Read(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard",
                    "EnableVirtualizationBasedSecurity");
                if (hv != null && Convert.ToInt32(hv, CultureInfo.InvariantCulture) == 1) r.VbsRunning = true;

                var (_, opt) = ProcessRunner.PowerShell(
                    "(Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All).State");
                r.HyperVEnabled = opt.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── HAGS ─────────────────────────────────────────────────────
            try
            {
                var v = Reg.Read(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
                r.HagsEnabled = v != null && Convert.ToInt32(v, CultureInfo.InvariantCulture) == 2;
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── Resizable BAR via BAR1 size ──────────────────────────────
            try
            {
                var (_, smi) = ProcessRunner.Run("nvidia-smi.exe", "-q -d MEMORY", 15000);
                // BAR1 == total VRAM means the large BAR window is active.
                var lines = smi.Split('\n').Select(l => l.Trim()).ToArray();
                int bar1 = -1, vram = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("BAR1 Memory Usage", StringComparison.OrdinalIgnoreCase))
                        for (int j = i; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(lines[j], @"^Total\s*:\s*(\d+)\s*MiB");
                            if (m.Success) { bar1 = int.Parse(m.Groups[1].Value); break; }
                        }
                    if (lines[i].StartsWith("FB Memory Usage", StringComparison.OrdinalIgnoreCase) ||
                        lines[i].StartsWith("Memory Usage", StringComparison.OrdinalIgnoreCase))
                        for (int j = i; j < Math.Min(i + 4, lines.Length); j++)
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(lines[j], @"^Total\s*:\s*(\d+)\s*MiB");
                            if (m.Success) { vram = int.Parse(m.Groups[1].Value); break; }
                        }
                }
                if (bar1 > 0 && vram > 0) r.ReBarEnabled = bar1 >= vram;
            }
            catch (Exception ex) { r.Errors.Add(ex.Message); }

            // ── power plan ───────────────────────────────────────────────
            try
            {
                var (_, pl) = ProcessRunner.Run("powercfg.exe", "/getactivescheme");
                var gm = System.Text.RegularExpressions.Regex.Match(pl,
                    @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
                if (gm.Success)
                {
                    // The GUID is ASCII and always readable — the localised plan name is
                    // not (powercfg writes in the OEM codepage), so map known GUIDs.
                    r.PowerPlanGuid = gm.Groups[1].Value.ToLowerInvariant();
                    r.ActivePowerPlan = r.PowerPlanGuid switch
                    {
                        "e9a42b02-d5df-448d-aa00-03f14749eb61" => "Максимальная производительность (Ultimate)",
                        "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" => "Высокая производительность",
                        "381b4222-f694-41f0-9685-ff5bb260df2e" => "Сбалансированная",
                        "a1841308-3541-4fab-bc81-f71556f20b4a" => "Экономия энергии",
                        _ => $"пользовательская ({r.PowerPlanGuid})"
                    };
                }
                else
                {
                    r.ActivePowerPlan = "не удалось прочитать";
                    r.Errors.Add($"powercfg: пустой ответ ({(pl ?? "").Trim().Length} симв.)");
                }
            }
            catch (Exception ex) { r.Errors.Add($"схема питания: {ex.Message}"); }

            // ── 1.0: SSD/NVMe + ноут/десктоп ─────────────────────────────
            try
            {
                var (_, media) = ProcessRunner.PowerShell(
                    "Get-PhysicalDisk | ForEach-Object { \"$($_.MediaType)|$($_.BusType)\" }", 30000);
                string m = (media ?? "");
                r.HasSsd  = m.Contains("SSD", StringComparison.OrdinalIgnoreCase);
                r.HasNvme = m.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) { r.Errors.Add($"диски(тип): {ex.Message}"); }

            try
            {
                using var s = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (var _ in s.Get()) { r.IsLaptop = true; break; }
                if (!r.IsLaptop)
                {
                    using var s2 = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
                    foreach (var o in s2.Get())
                        if (o["ChassisTypes"] is ushort[] ct)
                            foreach (var c in ct) if (c == 8 || c == 9 || c == 10 || c == 14) r.IsLaptop = true;
                }
            }
            catch (Exception ex) { r.Errors.Add($"тип ПК: {ex.Message}"); }

            BuildFindings(r);
            log?.Invoke(r.ToText());
            return r;
        }

        private static void BuildFindings(RigReport r)
        {
            int score = 100;

            if (r.SingleChannel && r.RamSticks > 0)
            {
                score -= 20;
                r.Findings.Add($"Память работает в ОДНОМ канале ({r.RamSticks} планка). Это один из главных " +
                               "ограничителей в CPU-зависимых играх: пропускная способность памяти вдвое ниже нормы.");
                r.Actions.Add("Добавь вторую такую же планку в свободный слот — получится двухканальный режим. " +
                              "Это часто даёт больше, чем все программные твики вместе взятые.");
            }

            if (r.VbsRunning)
            {
                score -= 12;
                r.Findings.Add("VBS (виртуализация безопасности) включён. На отдельных системах он влияет на CPU-зависимые игры, но отключение снижает защиту Windows — нужен собственный замер.");
            }
            if (r.HvciRunning)
            {
                score -= 5;
                r.Findings.Add("Memory Integrity (HVCI) включён — это важная защита ядра. Не отключай её без понятной причины и сравнения результатов.");
            }
            if (r.HyperVEnabled)
                r.Findings.Add("Hyper-V включён (работает гипервизор). Отключение затронет WSL2 / виртуальные машины / эмуляторы.");

            // HAGS полезен на дискретных NVIDIA/AMD; на старых картах и iGPU — не всегда.
            if (!r.HagsEnabled && (r.GpuVendor == "NVIDIA" || r.GpuVendor == "AMD"))
            {
                score -= 4;
                r.Findings.Add("Аппаратное планирование GPU (HAGS) выключено. На современных видеокартах его стоит протестировать, но результат зависит от игры и драйвера.");
            }

            if (r.NicLinkedAt100Mbit)
            {
                score -= 6;
                r.Findings.Add("Сетевой адаптер залинкован на 100 Мбит/с вместо 1 Гбит/с — чаще всего это энергосбережение " +
                               "сетевой карты (Green Ethernet / EEE) или кабель/порт, а не провайдер.");
            }

            const string ultimatePlan = "e9a42b02-d5df-448d-aa00-03f14749eb61";
            const string highPlan     = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            bool goodPlan = string.Equals(r.PowerPlanGuid, ultimatePlan, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.PowerPlanGuid, highPlan, StringComparison.OrdinalIgnoreCase);
            if (!goodPlan)
            {
                score -= 6;
                if (r.IsLaptop)
                    r.Findings.Add($"Активная схема питания: «{r.ActivePowerPlan}». На ноутбуке «Высокая производительность» " +
                                   "даёт ровнее frametime (но сильнее греется и ест батарею — в играх от сети это ок).");
                else
                    r.Findings.Add($"Активная схема питания: «{r.ActivePowerPlan}» — схема «Максимальная производительность» " +
                                   "(Ultimate) даёт более ровный frametime.");
            }

            if (!r.HasSsd && !r.HasNvme && r.DiskSummary.Length > 0)
                r.Findings.Add("Система, похоже, на HDD. Перенос Windows и игр на SSD/NVMe — самый заметный апгрейд скорости.");

            if (r.RamTotalGb > 0 && r.RamTotalGb < 16)
            {
                score -= 8;
                r.Findings.Add($"ОЗУ {r.RamTotalGb} ГБ — для современных игр в самый раз 16 ГБ и больше.");
            }

            if (score < 0) score = 0;
            if (score > 100) score = 100;
            r.HealthScore = score;

            if (r.Findings.Count == 0) r.Findings.Add("Критичных ограничителей не найдено — система в хорошей форме.");
        }
    }
}
