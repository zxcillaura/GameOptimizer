using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameOptimizer.Core.Latency
{
    /// <summary>
    /// Resident latency helper:
    ///   • holds a 0.5 ms system timer resolution while a game is running
    ///   • purges the standby memory list periodically (16 GB machines stutter on it)
    ///   • raises the game process priority and keeps it on all cores
    /// Everything is released the moment the game closes.
    /// </summary>
    public static class LatencyKeeper
    {
        private static readonly string[] GameProcesses = { "cs2", "dota2" };

        private static Thread _thread;
        private static volatile bool _stop = true;
        private static volatile bool _timerHeld;
        private static uint _originalResolution;

        public static bool IsRunning => !_stop;
        public static string Status { get; private set; } = "выключен";

        [DllImport("ntdll.dll")] private static extern int NtSetTimerResolution(uint desired, bool set, out uint current);
        [DllImport("ntdll.dll")] private static extern int NtQueryTimerResolution(out uint min, out uint max, out uint current);
        [DllImport("ntdll.dll")] private static extern int NtSetSystemInformation(int cls, IntPtr info, int size);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr proc, uint access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string system, string name, out long luid);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivileges newState,
            int bufferLength, IntPtr previous, IntPtr returnLength);
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();

        [StructLayout(LayoutKind.Sequential)]
        private struct Luid { public uint Low; public int High; }
        [StructLayout(LayoutKind.Sequential)]
        private struct TokenPrivileges
        {
            public int Count;
            public Luid Luid;
            public int Attributes;
        }

        private const int SystemMemoryListInformation = 0x50;   // 80
        private const int MemoryPurgeStandbyList      = 4;

        public static string Start(Action<string> log = null)
        {
            if (!_stop) return "уже запущен";
            _stop = false;
            _thread = new Thread(Loop) { IsBackground = true, Name = "GAMEOPTIMIZv6-Latency" };
            _thread.Start();
            Status = "ожидание игры";
            log?.Invoke("[LATENCY] режим удержания латентности включён (таймер 0.5 мс + очистка standby при запущенной игре)");
            return "запущен";
        }

        public static string Stop(Action<string> log = null)
        {
            _stop = true;
            Release();
            Status = "выключен";
            log?.Invoke("[LATENCY] режим удержания латентности выключен");
            return "остановлен";
        }

        private static void Loop()
        {
            int tick = 0;
            while (!_stop)
            {
                try
                {
                    bool playing = false;
                    foreach (var name in GameProcesses)
                    {
                        foreach (var p in Process.GetProcessesByName(name))
                        {
                            playing = true;
                            try
                            {
                                if (p.PriorityClass != ProcessPriorityClass.High &&
                                    p.PriorityClass != ProcessPriorityClass.RealTime)
                                    p.PriorityClass = ProcessPriorityClass.High;
                                var all = new IntPtr(unchecked((long)((1UL << Environment.ProcessorCount) - 1)));
                                p.ProcessorAffinity = all;
                            }
                            catch { }
                        }
                    }

                    if (playing)
                    {
                        if (!_timerHeld) HoldTimer();
                        Status = "активен: игра запущена";
                        if (tick % 10 == 0) PurgeStandby();
                    }
                    else if (_timerHeld)
                    {
                        Release();
                        Status = "ожидание игры";
                    }
                }
                catch { }
                tick++;
                Thread.Sleep(2000);
            }
        }

        private static void HoldTimer()
        {
            try
            {
                NtQueryTimerResolution(out _, out _, out _originalResolution);
                if (NtSetTimerResolution(5000, true, out _) == 0)   // 5000 × 100 ns = 0.5 ms
                    _timerHeld = true;
            }
            catch { }
        }

        private static void Release()
        {
            if (!_timerHeld) return;
            try
            {
                uint res = _originalResolution == 0 ? 10000u : _originalResolution;
                for (int i = 0; i < 3 && NtSetTimerResolution(res, true, out _) != 0; i++)
                    Thread.Sleep(50);
            }
            catch { }
            _timerHeld = false;
        }

        public static bool PurgeStandby()
        {
            try
            {
                EnablePrivilege("SeProfileSingleProcessPrivilege");
                IntPtr buf = Marshal.AllocHGlobal(4);
                try
                {
                    Marshal.WriteInt32(buf, MemoryPurgeStandbyList);
                    return NtSetSystemInformation(SystemMemoryListInformation, buf, 4) == 0;
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
            catch { return false; }
        }

        private static void EnablePrivilege(string name)
        {
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), 0x0020 | 0x0008, out IntPtr token)) return;
                if (!LookupPrivilegeValue(null, name, out long luid)) return;
                var tp = new TokenPrivileges
                {
                    Count = 1,
                    Luid = new Luid { Low = (uint)luid, High = (int)(luid >> 32) },
                    Attributes = 0x00000002   // SE_PRIVILEGE_ENABLED
                };
                AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }
    }
}
