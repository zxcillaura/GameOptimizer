using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Monitor
{
    public sealed class Sample
    {
        public DateTime At      { get; set; } = DateTime.Now;
        public double CpuLoad   { get; set; }
        public double RamPercent { get; set; }
        public double RamUsedGb { get; set; }
        public double RamTotalGb { get; set; }
        public double CpuClockMhz { get; set; }
        public double GpuLoad   { get; set; }
        public double GpuMemPct { get; set; }
        public double GpuMemUsedMb { get; set; }
        public double GpuTempC  { get; set; }
        public double GpuClockMhz { get; set; }
        public double GpuPowerW { get; set; }
        public bool   GpuValid  { get; set; }
    }

    public sealed class ProcInfo
    {
        public string Name   { get; set; } = "";
        public int    Pid    { get; set; }
        public double CpuPct { get; set; }
        public double MemMb  { get; set; }
    }

    /// <summary>
    /// Live system telemetry. CPU and RAM come from native counters (cheap),
    /// GPU numbers from nvidia-smi, sampled on a background thread.
    /// </summary>
    public static class SystemMonitor
    {
        private static Thread _thread;
        private static volatile bool _stop = true;
        private static int _intervalMs = 1000;

        public static Sample Latest { get; private set; } = new();
        public static bool IsRunning => !_stop;
        public static event Action<Sample> Sampled;

        // ── native counters ──────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime { public uint Low; public uint High; }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

        [StructLayout(LayoutKind.Sequential)]
        private class MemoryStatusEx
        {
            public uint  Length;
            public uint  MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

        private static ulong ToUlong(FileTime ft) => ((ulong)ft.High << 32) | ft.Low;

        private static double _lastIdle, _lastTotal;
        private static double _cpuClock = 0;
        private static DateTime _clockRefreshed = DateTime.MinValue;
        private static readonly object _procsLock = new();
        private static Dictionary<int, (TimeSpan cpu, DateTime at)> _procSnapshot = new();

        public static void Start(int intervalMs = 1000)
        {
            if (!_stop) return;
            _intervalMs = Math.Max(300, intervalMs);
            _stop = false;
            _thread = new Thread(Loop) { IsBackground = true, Name = "GAMEOPTIMIZv7-Monitor" };
            _thread.Start();
        }

        public static void Stop()
        {
            // NOTE: never null the Sampled event here — that would silently detach
            // every subscriber (the monitor page) and the UI would never update again.
            _stop = true;
        }

        private static void Loop()
        {
            int tick = 0;
            while (!_stop)
            {
                try
                {
                    var s = new Sample { At = DateTime.Now, CpuLoad = ReadCpuLoad() };
                    ReadMemory(s);

                    if (tick % 2 == 0)   // GPU every other tick — nvidia-smi spawns a process
                    {
                        ReadGpu(s);
                        RefreshCpuClock();
                    }
                    else
                    {
                        var prev = Latest;
                        s.GpuLoad = prev.GpuLoad; s.GpuMemPct = prev.GpuMemPct;
                        s.GpuMemUsedMb = prev.GpuMemUsedMb; s.GpuTempC = prev.GpuTempC;
                        s.GpuClockMhz = prev.GpuClockMhz; s.GpuPowerW = prev.GpuPowerW;
                        s.GpuValid = prev.GpuValid;
                    }
                    s.CpuClockMhz = _cpuClock;

                    Latest = s;
                    Sampled?.Invoke(s);
                }
                catch { }
                tick++;
                Thread.Sleep(_intervalMs);
            }
        }

        private static double ReadCpuLoad()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;
            double i = ToUlong(idle), k = ToUlong(kernel), u = ToUlong(user);
            double total = k + u;                       // kernel already contains idle
            double dIdle = i - _lastIdle, dTotal = total - _lastTotal;
            _lastIdle = i; _lastTotal = total;
            if (dTotal <= 0 || _lastTotal <= 0) return 0;
            double load = (1.0 - dIdle / dTotal) * 100.0;
            return Math.Max(0, Math.Min(100, load));
        }

        private static void ReadMemory(Sample s)
        {
            try
            {
                var m = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                if (!GlobalMemoryStatusEx(m)) return;
                s.RamTotalGb = m.TotalPhys / 1024.0 / 1024 / 1024;
                s.RamUsedGb  = (m.TotalPhys - m.AvailPhys) / 1024.0 / 1024 / 1024;
                s.RamPercent = m.MemoryLoad;
            }
            catch { }
        }

        private static void RefreshCpuClock()
        {
            if ((DateTime.Now - _clockRefreshed).TotalSeconds < 8) return;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor");
                foreach (var o in searcher.Get())
                {
                    if (double.TryParse(o["CurrentClockSpeed"]?.ToString(), out double c) && c > 0)
                        _cpuClock = c;
                    break;
                }
                _clockRefreshed = DateTime.Now;
            }
            catch { }
        }

        private static void ReadGpu(Sample s)
        {
            try
            {
                var (code, output) = ProcessRunner.Run("nvidia-smi.exe",
                    "--query-gpu=utilization.gpu,memory.used,memory.total,temperature.gpu,clocks.sm,power.draw " +
                    "--format=csv,noheader,nounits", 6000);
                if (code != 0 || string.IsNullOrWhiteSpace(output)) return;

                var parts = output.Split('\n')[0].Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 5) return;

                s.GpuLoad = ParseD(parts[0]);
                s.GpuMemUsedMb = ParseD(parts[1]);
                double totalMb = ParseD(parts[2]);
                s.GpuMemPct = totalMb > 0 ? s.GpuMemUsedMb / totalMb * 100.0 : 0;
                s.GpuTempC = ParseD(parts[3]);
                s.GpuClockMhz = ParseD(parts[4]);
                s.GpuPowerW = parts.Length > 5 ? ParseD(parts[5]) : 0;
                s.GpuValid = true;
            }
            catch { }
        }

        private static double ParseD(string s)
        {
            s = s.Replace("[N/A]", "").Replace("N/A", "").Trim();
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0;
        }

        /// <summary>Top processes by CPU share since the previous call.</summary>
        public static List<ProcInfo> TopProcesses(int count = 8)
        {
            var result = new List<ProcInfo>();
            try
            {
                int cores = Math.Max(1, Environment.ProcessorCount);
                var now = DateTime.Now;
                var fresh = new Dictionary<int, (TimeSpan cpu, DateTime at)>();
                var list = new List<ProcInfo>();

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        var cpu = p.TotalProcessorTime;
                        fresh[p.Id] = (cpu, now);

                        double pct = 0;
                        if (_procSnapshot.TryGetValue(p.Id, out var prev))
                        {
                            double ms = (cpu - prev.cpu).TotalMilliseconds;
                            double wall = (now - prev.at).TotalMilliseconds;
                            if (wall > 0) pct = Math.Max(0, Math.Min(100, ms / (wall * cores) * 100.0));
                        }

                        list.Add(new ProcInfo
                        {
                            Name = p.ProcessName,
                            Pid = p.Id,
                            CpuPct = pct,
                            MemMb = p.WorkingSet64 / 1024.0 / 1024.0
                        });
                    }
                    catch { }
                }

                lock (_procsLock) _procSnapshot = fresh;
                result = list.OrderByDescending(x => x.CpuPct).ThenByDescending(x => x.MemMb)
                             .Take(count).ToList();
            }
            catch { }
            return result;
        }

        public static int ProcessCount
        {
            get { try { return Process.GetProcesses().Length; } catch { return 0; } }
        }
    }
}
