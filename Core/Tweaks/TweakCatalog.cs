using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Games;
using GameOptimizer.Core.Latency;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Tweaks
{
    /// <summary>
    /// Every change the program knows how to make, as an individually
    /// controllable item with live state, its own apply and its own undo.
    /// </summary>
    public static class TweakCatalog
    {
        private const string MM      = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string GAMES   = MM + @"\Tasks\Games";
        private const string MSTCPIP = @"SOFTWARE\Microsoft\MSTCPIP\Parameters";
        private const string GD      = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        private const string GCS     = @"System\GameConfigStore";
        private const string GBAR    = @"Software\Microsoft\GameBar";
        private const string GDVR    = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
        private const string MOUSE   = @"Control Panel\Mouse";
        private const string POWER   = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
        private const string DEVGUARD = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
        private const string HVCI     = DEVGUARD + @"\Scenarios\HypervisorEnforcedCodeIntegrity";
        private const string TCPIP6   = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        private const string PSCHED   = @"SOFTWARE\Policies\Microsoft\Windows\Psched";

        // powercfg subgroup/setting GUIDs (identical on every Windows install)
        private const string P_PROC = "54533251-82be-4824-96c1-47b60b740d00";
        private const string P_USB  = "2a737441-1930-4402-8d77-b2bebba308a3";
        private const string P_DISK = "0012ee47-9041-4b5d-9b77-535fba8b1442";
        private const string P_PCIE = "501a4d13-42af-4429-9fd1-a8218c268e20";

        public static List<TweakItem> Build() => new()
        {
            // ═══════════════ ПИТАНИЕ ═══════════════
            new ActionTweak
            {
                Id = "power-ultimate", Group = "Питание", Risk = TweakRisk.Safe,
                Title = "Схема питания Ultimate Performance",
                Desc = "Держит частоты процессора высоко и убирает скачки — ровнее frametime.",
                Detector = () =>
                {
                    var (_, text) = ProcessRunner.Run("powercfg.exe", "/getactivescheme");
                    var m = Regex.Match(text ?? "", @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
                    bool on = m.Success && m.Groups[1].Value.Equals(SystemTweaks.PlanUltimate, StringComparison.OrdinalIgnoreCase);
                    return new TweakState
                    {
                        Status = on ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = m.Success ? $"активная схема: {m.Groups[1].Value}" : "схема не прочитана"
                    };
                },
                Action = log =>
                {
                    var (_, text) = ProcessRunner.Run("powercfg.exe", "/getactivescheme");
                    var m = Regex.Match(text ?? "", @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
                    if (m.Success) BackupManager.Current.Meta["plan"] = m.Groups[1].Value;
                    SystemTweaks.SetPowerPlan(SystemTweaks.PlanUltimate, "Максимальная производительность", log);
                    return true;
                },
                Undo = (snap, log) =>
                {
                    if (snap?.Meta != null && snap.Meta.TryGetValue("plan", out string g) && !string.IsNullOrWhiteSpace(g))
                    { SystemTweaks.SetPowerPlan(g, "исходная схема", log); return true; }
                    SystemTweaks.SetPowerPlan(SystemTweaks.PlanHighPerf, "Высокая производительность", log);
                    return true;
                }
            },
            PowerTweak("power-usb-suspend", "Отключить USB selective suspend",
                "Windows перестаёт усыплять USB-устройства — меньше микро-задержек у мыши и клавиатуры.",
                P_USB, "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", 0, 1, TweakRisk.Safe),
            PowerTweak("power-pcie-aspm", "Отключить PCIe ASPM",
                "Запрещает энергосбережение шины PCIe — отклик видеокарты стабильнее.",
                P_PCIE, "ee12f906-d277-404b-b6da-e5fa1a576df5", 0, 1, TweakRisk.Safe),
            PowerTweak("power-disk-never-off", "Не отключать жёсткие диски",
                "Убирает спин-даун диска: нет пауз при подгрузке уровня или текстур.",
                P_DISK, "6738e2c4-e8a5-4a42-b16a-e040e769756e", 0, 20, TweakRisk.Safe),
            PowerTweak("power-core-parking", "Снять парковку ядер",
                "Все ядра остаются активными — меньше рывков при резкой смене нагрузки.",
                P_PROC, "0cc5b647-c1df-4637-891a-dec35c318583", 100, 100, TweakRisk.Safe),
            PowerTweak("power-cpu-min-100", "Минимальное состояние CPU 100%",
                "Процессор держит частоту постоянно. Простой станет горячее.",
                P_PROC, "893dee8e-2bef-41e0-89c6-b55d0929964c", 100, 5, TweakRisk.Caution),
            // 0 = disabled, 1 = enabled, 2 = aggressive  ->  "off" must be 1, not 2
            PowerTweak("power-boost-aggressive", "Агрессивный режим буста CPU",
                "Процессор охотнее уходит на максимальную частоту при нагрузке.",
                P_PROC, "be337238-0d82-4146-a960-4f3749d470c7", 2, 1, TweakRisk.Safe),
            RegTweak("power-fast-startup-off", "Питание", "Отключить быстрый запуск Windows",
                "Ядро грузится заново при каждом включении — чище состояние драйверов.",
                TweakRisk.Caution, RegistryHive.LocalMachine, POWER, "HiberbootEnabled", 0, reboot: true),
            new ActionTweak
            {
                Id = "power-hibernate-off", Group = "Питание", Risk = TweakRisk.Safe,
                Title = "Отключить гибернацию",
                Desc = "Освобождает hiberfil.sys (до 12 ГБ) и убирает лишние операции с диском.",
                Detector = () => new TweakState
                {
                    Status = File.Exists(@"C:\hiberfil.sys") ? TweakStatus.NotApplied : TweakStatus.Applied,
                    Detail = File.Exists(@"C:\hiberfil.sys") ? "hiberfil.sys существует" : "hiberfil.sys удалён"
                },
                Action = log => { ProcessRunner.Run("powercfg.exe", "/h off"); log("      powercfg /h off"); return true; },
                Undo = (snap, log) => { ProcessRunner.Run("powercfg.exe", "/h on"); log("      powercfg /h on"); return true; }
            },

            // ═══════════════ ЛАТЕНТНОСТЬ ═══════════════
            RegTweak("lat-system-responsiveness", "Латентность", "SystemResponsiveness = 0",
                "Windows отдаёт приоритет играм, а не фоновым задачам планировщика.",
                TweakRisk.Safe, RegistryHive.LocalMachine, MM, "SystemResponsiveness", 0),
            RegTweak("lat-network-throttling", "Латентность", "Отключить сетевой троттлинг",
                "Снимает искусственное ограничение сетевого стека в 10 пакетов на мс.",
                TweakRisk.Safe, RegistryHive.LocalMachine, MM, "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF)),
            new RegistryTweak
            {
                Id = "lat-mmcss-games", Group = "Латентность", Risk = TweakRisk.Safe,
                Title = "Профиль MMCSS «Games» = High",
                Desc = "Повышает приоритет игрового потока: GPU priority 8, планирование High.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.LocalMachine, Sub = GAMES, Name = "GPU Priority", Value = 8, Label = "GPU Priority" },
                    new() { Hive = RegistryHive.LocalMachine, Sub = GAMES, Name = "Priority", Value = 6, Label = "Priority" },
                    new() { Hive = RegistryHive.LocalMachine, Sub = GAMES, Name = "Scheduling Category", Value = "High",
                            Kind = RegistryValueKind.String, Label = "Scheduling" },
                    new() { Hive = RegistryHive.LocalMachine, Sub = GAMES, Name = "SFIO Priority", Value = "High",
                            Kind = RegistryValueKind.String, Label = "SFIO" },
                }
            },
            TcpTweak("lat-tcp-nodelay", "TCP NoDelay",
                "Отправляет пакеты сразу, не дожидаясь накопления буфера. Пишется в активные сетевые интерфейсы, а не в устаревший общий ключ.",
                "TCPNoDelay", 1, TweakRisk.Safe),
            TcpTweak("lat-tcp-ackfreq", "TcpAckFrequency = 1",
                "Подтверждения уходят немедленно — меньше задержка ответа сервера.",
                "TcpAckFrequency", 1, TweakRisk.Caution, "может увеличить трафик"),
            TcpTweak("lat-tcp-delack", "Отключить delayed ACK",
                "Убирает задержку отложенных подтверждений TCP.",
                "TcpDelAckTicks", 0, TweakRisk.Caution),
            BcdTweak("lat-disabledynamictick", "Латентность", "Отключить dynamic tick",
                "Системный таймер перестаёт объединяться — стабильнее отклик ввода.",
                "disabledynamictick", "yes", TweakRisk.Caution, reboot: true),
            BcdTweak("lat-platformclock", "Латентность", "Не использовать platform clock",
                "Заставляет Windows опираться на TSC процессора, а не на чипсет.",
                "useplatformclock", "false", TweakRisk.Caution, reboot: true),
            new ActionTweak
            {
                Id = "lat-timer-05ms", Group = "Латентность", Risk = TweakRisk.Safe,
                Title = "Таймер 0.5 мс",
                Desc = "Держит минимальное разрешение таймера, пока запущена игра.",
                Note = "работает пока открыта программа",
                Detector = () => new TweakState
                {
                    Status = LatencyKeeper.IsRunning ? TweakStatus.Applied : TweakStatus.NotApplied,
                    Detail = LatencyKeeper.IsRunning ? "режим активен" : "режим выключен"
                },
                Action = log => { LatencyKeeper.Start(log); return true; },
                Undo = (snap, log) => { LatencyKeeper.Stop(log); return true; }
            },
            new ActionTweak
            {
                Id = "mem-trim-now", Group = "Память", Risk = TweakRisk.Safe,
                Title = "Очистить память сейчас",
                Desc = "Подрезает рабочие наборы процессов и чистит список ожидания.",
                Detector = () => new TweakState
                {
                    Status = TweakStatus.OneShot,
                    Detail = "разовое действие — состояние не отслеживается"
                },
                Action = log =>
                {
                    bool purged = LatencyKeeper.PurgeStandby();
                    SystemTweaks.TrimWorkingSets(log);
                    log($"      standby очищен: {(purged ? "да" : "нет прав")}");
                    return true;
                }
            },

            // ═══════════════ ГРАФИКА ═══════════════
            RegTweak("gfx-hags", "Графика", "HAGS (аппаратное планирование GPU)",
                "Видеокарта сама управляет очередью кадров. Для RTX 30-й серии выигрышно.",
                TweakRisk.Safe, RegistryHive.LocalMachine, GD, "HwSchMode", 2, reboot: true),
            // value is a size cap in GB; 10 is the widely used sane value.
            // (100 would cap the cache at 100 GB, and 0xFFFFFFFF would be unlimited.)
            // value is a size CAP in GB (not a preallocation). 100 GB matches the
            // recommended NVIDIA App setting; the driver default is far smaller.
            RegTweak("gfx-shader-cache", "Графика", "Кэш шейдеров = 100 ГБ",
                "Шейдеры не пересобираются на ходу — нет фризов при входе в новую зону.",
                TweakRisk.Safe, RegistryHive.LocalMachine, GD, "ShaderCacheSize", 100,
                note: "занимает место на диске"),
            new RegistryTweak
            {
                Id = "gfx-fullscreen-exclusive", Group = "Графика", Risk = TweakRisk.Safe,
                Title = "Истинный exclusive fullscreen",
                Desc = "Отключает полноэкранные оптимизации: нет скачков от композитора Windows.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = GCS, Name = "GameDVR_FSEBehaviorMode", Value = 2 },
                    new() { Hive = RegistryHive.CurrentUser, Sub = GCS, Name = "GameDVR_HonorUserFSEBehaviorMode", Value = 1 },
                    new() { Hive = RegistryHive.CurrentUser, Sub = GCS, Name = "GameDVR_DXGIHonorFSEWindowsCompatible", Value = 1 },
                    new() { Hive = RegistryHive.CurrentUser, Sub = GCS, Name = "GameDVR_EFSEFeatureFlags", Value = 0 },
                }
            },
            new RegistryTweak
            {
                Id = "gfx-game-dvr-off", Group = "Графика", Risk = TweakRisk.Safe,
                Title = "Отключить Game DVR и фоновую запись",
                Desc = "Windows перестаёт писать буфер игры в фоне — минус нагрузка на диск и GPU.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = GDVR, Name = "AppCaptureEnabled", Value = 0 },
                    new() { Hive = RegistryHive.CurrentUser, Sub = GDVR, Name = "HistoricalCaptureEnabled", Value = 0 },
                    new() { Hive = RegistryHive.CurrentUser, Sub = GCS,  Name = "GameDVR_Enabled", Value = 0 },
                }
            },
            RegTweak("gfx-game-mode-on", "Графика", "Игровой режим Windows",
                "Система отдаёт ресурсы активной игре, а не фоновым службам.",
                TweakRisk.Safe, RegistryHive.CurrentUser, GBAR, "AutoGameModeEnabled", 1),
            new RegistryTweak
            {
                Id = "gfx-mouse-accel-off", Group = "Графика", Risk = TweakRisk.Safe,
                Title = "Отключить ускорение мыши",
                Desc = "Один к одному движение руки и курсора — важно для прицеливания.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseSpeed", Value = "0",
                            Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseThreshold1", Value = "0",
                            Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseThreshold2", Value = "0",
                            Kind = RegistryValueKind.String },
                },
                Note = "после перезахода в систему"
            },

            // ═══════════════ СЕТЬ ═══════════════
            new ActionTweak
            {
                Id = "net-nic-powersave", Group = "Сеть", Risk = TweakRisk.Safe,
                Title = "Отключить энергосбережение сетевой карты",
                Desc = "EEE, Green Ethernet, Gigabit Lite, модерация прерываний — всё выключается.",
                Note = "чинит линк 100 Мбит/с",
                Detector = () =>
                {
                    var (_, nic) = ProcessRunner.PowerShell(
                        "Get-NetAdapterAdvancedProperty -RegistryKeyword 'GigaLite' -ErrorAction SilentlyContinue | " +
                        "Select-Object -First 1 -ExpandProperty RegistryValue");
                    var (_, up) = ProcessRunner.PowerShell(
                        "Get-NetAdapter | Where-Object Status -eq 'Up' | " +
                        "ForEach-Object { \"$($_.Name) $($_.LinkSpeed)\" }");
                    string link = (up ?? "").Replace("\r", "").Trim();
                    bool off = nic != null && nic.Trim() == "0";
                    return new TweakState
                    {
                        Status = off ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"линк: {link.Replace("\n", " | ")}"
                    };
                },
                Action = log => { SystemTweaks.TuneNetworkAdapter(log); return true; },
                Undo = (snap, log) => RestoreNic(log)
            },
            new ActionTweak
            {
                Id = "net-dns-fast", Group = "Сеть", Risk = TweakRisk.Caution,
                Title = "DNS Cloudflare 1.1.1.1",
                Desc = "Быстрее находит игровые серверы и матчмейкинг, чем DNS провайдера.",
                Detector = () =>
                {
                    var (_, dns) = ProcessRunner.PowerShell(
                        "(Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object { $_.ServerAddresses.Count -gt 0 } | " +
                        "Select-Object -First 1).ServerAddresses -join ','");
                    string cur = (dns ?? "").Replace("\r", "").Trim();
                    return new TweakState
                    {
                        Status = cur.Contains("1.1.1.1") ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = string.IsNullOrEmpty(cur) ? "DNS от роутера (авто)" : $"сейчас: {cur}"
                    };
                },
                Action = log =>
                {
                    var (_, cur) = ProcessRunner.PowerShell(
                        "(Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object { $_.ServerAddresses.Count -gt 0 } | " +
                        "Select-Object -First 1).ServerAddresses -join ','");
                    BackupManager.Current.Meta["dns"] = (cur ?? "").Replace("\r", "").Trim();

                    ProcessRunner.PowerShell(NicForeach(
                        "Set-DnsClientServerAddress -InterfaceAlias $_.Name -ServerAddresses ('1.1.1.1','1.0.0.1')"), 60000);
                    log("      DNS -> 1.1.1.1 / 1.0.0.1");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    string prev = snap?.Meta != null && snap.Meta.TryGetValue("dns", out string p) ? p : "";
                    string cmd = string.IsNullOrWhiteSpace(prev)
                        ? NicForeach("Set-DnsClientServerAddress -InterfaceAlias $_.Name -ResetServerAddresses")
                        : NicForeach($"Set-DnsClientServerAddress -InterfaceAlias $_.Name -ServerAddresses ('{prev.Replace(",", "','")}')");
                    ProcessRunner.PowerShell(cmd, 60000);
                    log($"      DNS возвращён: {(string.IsNullOrWhiteSpace(prev) ? "авто" : prev)}");
                    return true;
                }
            },
            new ActionTweak
            {
                Id = "net-reset-stack", Group = "Сеть", Risk = TweakRisk.Risky,
                Title = "Сбросить стек TCP/IP и Winsock",
                Desc = "Ремонтное действие: помогает при странных лагах, обрывах и высоком пинге.",
                Note = "нужна перезагрузка, откат невозможен",
                RequiresReboot = true,
                Detector = () => new TweakState
                {
                    Status = TweakStatus.OneShot,
                    Detail = "разовое ремонтное действие — состояние не отслеживается"
                },
                Action = log =>
                {
                    ProcessRunner.Run("netsh.exe", "int ip reset", 120000);
                    ProcessRunner.Run("netsh.exe", "winsock reset", 120000);
                    ProcessRunner.Run("netsh.exe", "int tcp reset", 120000);
                    log("      стек сброшен — перезагрузи ПК");
                    return true;
                }
            },

            // ═══════════════ GPU ═══════════════
            new ActionTweak
            {
                Id = "gpu-persistence", Group = "GPU", Risk = TweakRisk.Caution,
                Title = "Режим сохраняемости драйвера",
                Desc = "Драйвер остаётся инициализированным — меньше задержка при старте игры.",
                Note = "поддержка зависит от драйвера",
                Detector = () =>
                {
                    string v = Nv("--query-gpu=persistence_mode --format=csv,noheader");
                    if (v == null) return new TweakState { Status = TweakStatus.Unsupported, Detail = "nvidia-smi недоступен" };
                    if (v.Contains("[N/A]", StringComparison.OrdinalIgnoreCase))
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = "GPU не поддерживает persistence mode" };
                    return new TweakState
                    {
                        Status = v.StartsWith("Enabled", StringComparison.OrdinalIgnoreCase) ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"persistence_mode = {v}"
                    };
                },
                Action = log => NvRun("-pm 1", log, "persistence mode = enabled"),
                Undo = (snap, log) => { NvRun("-pm 0", log, "persistence mode = disabled"); return true; }
            },
            new ActionTweak
            {
                Id = "gpu-lock-clocks", Group = "GPU", Risk = TweakRisk.Risky,
                Title = "Зафиксировать максимальные частоты ядра",
                Desc = "Частота GPU не скачет вниз — самый ровный frametime из возможного.",
                Note = "выше температура и шум",
                RequiresReboot = false,
                Detector = () => new TweakState
                {
                    Status = TweakStatus.OneShot,
                    Detail = "факт фиксации не читается; отменяется кнопкой отката"
                },
                Action = log =>
                {
                    string max = Nv("--query-gpu=clocks.max.sm --format=csv,noheader,nounits");
                    if (string.IsNullOrWhiteSpace(max)) { log("      не удалось узнать максимальную частоту"); return false; }
                    BackupManager.Current.Meta["gpu-max-sm"] = max.Trim();
                    return NvRun($"-lgc {max.Trim()}", log, $"частоты ядра зафиксированы на {max.Trim()} МГц");
                },
                Undo = (snap, log) => { NvRun("-rgc", log, "частоты ядра возвращены в авторежим"); return true; }
            },
            new ActionTweak
            {
                Id = "gpu-power-limit", Group = "GPU", Risk = TweakRisk.Caution,
                Title = "Поднять лимит мощности GPU",
                Desc = "Разрешает видеокарте потреблять максимум — выше частоты под нагрузкой.",
                Detector = () =>
                {
                    string v = Nv("--query-gpu=power.limit,power.default_limit,power.max_limit --format=csv,noheader,nounits");
                    if (v == null) return new TweakState { Status = TweakStatus.Unsupported, Detail = "nvidia-smi недоступен" };
                    return new TweakState { Status = TweakStatus.OneShot, Detail = $"лимиты (тек/штат/макс): {v}" };
                },
                Action = log =>
                {
                    string v = Nv("--query-gpu=power.limit,power.default_limit,power.max_limit --format=csv,noheader,nounits");
                    if (string.IsNullOrWhiteSpace(v)) return false;
                    var parts = v.Split(',');
                    if (parts.Length < 3) return false;
                    double def = ParseD(parts[1]), max = ParseD(parts[2]);
                    if (max <= def) { log("      поднимать нечего: максимальный лимит равен штатному"); return true; }
                    BackupManager.Current.Meta["gpu-pl"] = def.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return NvRun($"-pl {max.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                                 log, $"лимит мощности -> {max} Вт");
                },
                Undo = (snap, log) =>
                {
                    if (snap?.Meta != null && snap.Meta.TryGetValue("gpu-pl", out string d) && !string.IsNullOrWhiteSpace(d))
                        NvRun($"-pl {d}", log, $"лимит мощности возвращён на {d} Вт");
                    return true;
                }
            },

            // ═══════════════ ИГРЫ ═══════════════
            new ActionTweak
            {
                Id = "game-cs2-config", Group = "Игры", Risk = TweakRisk.Safe,
                Title = "Конфиг CS2 (autoexec)",
                Desc = "Кап кадров под G-Sync, Reflex, сетевые и звуковые параметры. Твои строки сохраняются.",
                Detector = () => FileState(GameConfigWriter.Cs2Autoexec),
                Action = log => GameConfigWriter.WriteAll(log, cs2: true, dota: false),
                Undo = (snap, log) => true
            },
            new ActionTweak
            {
                Id = "game-dota-config", Group = "Игры", Risk = TweakRisk.Safe,
                Title = "Конфиг Dota 2 (autoexec)",
                Desc = "Кап кадров, отключение тяжёлых эффектов, сетевые параметры. Твои строки сохраняются.",
                Detector = () => FileState(GameConfigWriter.DotaAutoexec),
                Action = log => GameConfigWriter.WriteAll(log, cs2: false, dota: true),
                Undo = (snap, log) => true
            },
            new ActionTweak
            {
                Id = "game-close-overlays", Group = "Игры", Risk = TweakRisk.Safe,
                Title = "Закрыть оверлеи сейчас",
                Desc = "Оверлеи NVIDIA и Discord заметно портят 1% low в CS2 и Dota 2.",
                Detector = () =>
                {
                    int n = 0;
                    foreach (var p in new[] { "NVIDIA Overlay", "NVIDIA Share" })
                        n += System.Diagnostics.Process.GetProcessesByName(p).Length;
                    return new TweakState
                    {
                        Status = n == 0 ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = n == 0 ? "оверлеев не найдено" : $"работает процессов оверлея: {n}"
                    };
                },
                Action = log =>
                {
                    SystemTweaks.CloseOverlayProcesses(log);
                    DisableDiscordOverlay(log);
                    return true;
                }
            },
            new ActionTweak
            {
                Id = "game-cs2-launch", Group = "Игры", Risk = TweakRisk.Safe,
                Title = "Параметры запуска CS2",
                Desc = "-novid +exec autoexec.cfg. Steam будет закрыт автоматически, иначе он перезапишет правку.",
                Detector = () =>
                {
                    string cfg = SteamLaunchOptions.ActiveConfig();
                    if (string.IsNullOrEmpty(cfg))
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = "Steam не найден" };
                    string cur = SteamLaunchOptions.Read(cfg, "730");
                    bool ok = !string.IsNullOrEmpty(cur) && cur.Contains("+exec autoexec.cfg");
                    return new TweakState
                    {
                        Status = ok ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"сейчас в Steam: {(string.IsNullOrWhiteSpace(cur) ? "пусто" : cur)}"
                    };
                },
                Action = log => WriteLaunchOptions("730", "-novid +exec autoexec.cfg", log)
            },
            new ActionTweak
            {
                Id = "game-dota-launch", Group = "Игры", Risk = TweakRisk.Safe,
                Title = "Параметры запуска Dota 2",
                Desc = "-novid. Больше ничего не нужно: -threads, -high и -freq либо не работают, либо вредят.",
                Detector = () =>
                {
                    string cfg = SteamLaunchOptions.ActiveConfig();
                    if (string.IsNullOrEmpty(cfg))
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = "Steam не найден" };
                    string cur = SteamLaunchOptions.Read(cfg, "570");
                    bool ok = !string.IsNullOrEmpty(cur) && cur.Contains("-novid");
                    return new TweakState
                    {
                        Status = ok ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"сейчас в Steam: {(string.IsNullOrWhiteSpace(cur) ? "пусто" : cur)}"
                    };
                },
                Action = log => WriteLaunchOptions("570", "-novid", log)
            },

            // ═══════════════ СИСТЕМА ═══════════════
            new ActionTweak
            {
                Id = "sys-vbs-off", Group = "Система", Risk = TweakRisk.Risky,
                Title = "Выключить VBS и Memory Integrity",
                Desc = "Убирает 5–15% потерь там, где игра упирается в процессор — в первую очередь в поздней Dota 2.",
                Note = "ломает BlueStacks и WSL2 · нужна перезагрузка",
                RequiresReboot = true,
                Detector = () =>
                {
                    int vbs = ToInt(Reg.Read(RegistryHive.LocalMachine, DEVGUARD, "EnableVirtualizationBasedSecurity"));
                    int hvci = ToInt(Reg.Read(RegistryHive.LocalMachine, HVCI, "Enabled"));
                    bool on = vbs == 0 && hvci == 0;
                    return new TweakState
                    {
                        Status = on ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"VBS = {(vbs == 1 ? "включён" : "выключен")}, " +
                                 $"Memory Integrity = {(hvci == 1 ? "включён" : "выключен")}"
                    };
                },
                Action = log =>
                {
                    BackupManager.WriteTracked(RegistryHive.LocalMachine, DEVGUARD, "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
                    BackupManager.WriteTracked(RegistryHive.LocalMachine, HVCI, "Enabled", 0, RegistryValueKind.DWord);
                    BackupManager.TrackBcd("hypervisorlaunchtype");
                    ProcessRunner.Run("bcdedit.exe", "/set hypervisorlaunchtype off");
                    log("      VBS и Memory Integrity выключены, гипервизор отключён — нужна перезагрузка");
                    return true;
                }
            },

            // ═══════════════ 0.8: НОВАЯ СЕТЬ ═══════════════
            RegTweak("net-ipv6-off", "Сеть", "Отключить IPv6",
                "Убирает IPv6 со стека: меньше лишних пакетов и конфликтов с некоторыми античитами. Откат вернёт былое состояние.",
                TweakRisk.Caution, RegistryHive.LocalMachine, TCPIP6, "DisabledComponents", 0xFF,
                note: "редкие игры/античиты могут требовать IPv6"),
            RegTweak("net-qos-off", "Сеть", "Снять резерв QoS (20% канала)",
                "Windows держит 20% пропускной способности «про запас» для служебного трафика. Без резерва полоса идёт в игру.",
                TweakRisk.Safe, RegistryHive.LocalMachine, PSCHED, "MinBandwidth", 0,
                note: "влияет на систему в целом"),
            new ActionTweak
            {
                Id = "net-tcp-autotune-normal", Group = "Сеть", Risk = TweakRisk.Safe,
                Title = "TCP autotuning = Normal",
                Desc = "Отключает агрессивное масштабирование буферов: под загрузкой пинг ровнее, без «всплесков».",
                Detector = () =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "(Get-NetTCPSetting | Where-Object { $_.AutoTuningLevelLocal } | Select-Object -First 1).AutoTuningLevelLocal", 30000);
                    string cur = (outp ?? "").Replace("\r", "").Trim();
                    return new TweakState
                    {
                        Status = cur.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                                 ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = cur.Length > 0 ? $"сейчас: {cur}" : "не удалось прочитать"
                    };
                },
                Action = log =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "(Get-NetTCPSetting | Where-Object { $_.AutoTuningLevelLocal } | Select-Object -First 1).AutoTuningLevelLocal", 30000);
                    BackupManager.Current.Meta["tcp-autotune"] = (outp ?? "").Replace("\r", "").Trim();
                    var (code, _) = ProcessRunner.Run("netsh.exe", "int tcp set global autotuninglevel=normal");
                    log("      autotuninglevel -> normal");
                    return code == 0;
                },
                Undo = (snap, log) =>
                {
                    string prev = snap?.Meta != null && snap.Meta.TryGetValue("tcp-autotune", out string p) && p.Length > 0
                        ? p : "normal";
                    ProcessRunner.Run("netsh.exe", $"int tcp set global autotuninglevel={prev}");
                    log($"      autotuninglevel -> {prev} (как было)");
                    return true;
                }
            },

            // ═══════════════ 0.8: НОВАЯ СИСТЕМА ═══════════════
            RegTweak("sys-dwm-transparency-off", "Система", "Отключить прозрачность окон (DWM)",
                "Без полупрозрачности в меню и панели задач: GPU меньше занят, меньше мерцания.",
                TweakRisk.Caution, RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\DWM", "EnableTransparency", 0,
                note: "визуальное изменение, откат вернёт прозрачность"),
            new ActionTweak
            {
                Id = "sys-ssd-trim", Group = "Система", Risk = TweakRisk.Safe,
                Title = "Оптимизировать диски (TRIM)",
                Desc = "Подсказывает SSD, какие блоки можно очистить — диск остаётся быстрым в играх.",
                Detector = () => new TweakState
                {
                    Status = TweakStatus.OneShot,
                    Detail = "разовое действие — состояние не отслеживается"
                },
                Action = log =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "Get-Volume | Where-Object { $_.DriveType -eq 'Fixed' -and $_.DriveLetter } | " +
                        "Optimize-Volume -ReTrim -ErrorAction SilentlyContinue; 'TRIM-DONE'", 300000);
                    bool ok = outp != null && outp.Contains("TRIM-DONE");
                    log(ok ? "      TRIM по жёстким дискам выполнен" : "      TRIM: не удалось — проверь диски вручную");
                    return ok;
                }
            },

            // ═══════════════ 1.0: МЫШЬ И КЛАВА (инпут-лаг) ═══════════════
            new RegistryTweak
            {
                Id = "mk-accel-1to1", Group = "Мышь и клава", Risk = TweakRisk.Safe,
                Title = "Мышь 1:1 (полное снятие акселерации, MarkC-fix)",
                Desc = "Курсор двигается ровно на столько, на сколько двигаешь мышь — без «разгона» винды. "
                     + "Выверенные кривые 6/11 (точный способ, а не просто галочка). Важно для прицела.",
                Note = "применяется после перезахода в систему",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseSensitivity", Value = "10", Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseSpeed",       Value = "0",  Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseThreshold1",  Value = "0",  Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "MouseThreshold2",  Value = "0",  Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "SmoothMouseXCurve",
                            Kind = RegistryValueKind.Binary,
                            Value = new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00, 0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00,
                                               0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00, 0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00,
                                               0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00} },
                    new() { Hive = RegistryHive.CurrentUser, Sub = MOUSE, Name = "SmoothMouseYCurve",
                            Kind = RegistryValueKind.Binary,
                            Value = new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00, 0x00,0x00,0x38,0x00,0x00,0x00,0x00,0x00,
                                               0x00,0x00,0x70,0x00,0x00,0x00,0x00,0x00, 0x00,0x00,0xA8,0x00,0x00,0x00,0x00,0x00,
                                               0x00,0x00,0xE0,0x00,0x00,0x00,0x00,0x00} },
                }
            },
            new RegistryTweak
            {
                Id = "mk-mouse-queue", Group = "Мышь и клава", Risk = TweakRisk.Caution,
                Title = "Уменьшить очередь ввода мыши",
                Desc = "Меньше буфер пакетов мыши до обработки — чуть меньше задержка клика. Для скоростных мышей.",
                Note = "после перезагрузки",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.LocalMachine, Sub = @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters",
                            Name = "MouseDataQueueSize", Value = 20, Kind = RegistryValueKind.DWord, Label = "MouseDataQueueSize" },
                }
            },
            new RegistryTweak
            {
                Id = "mk-kbd-queue", Group = "Мышь и клава", Risk = TweakRisk.Caution,
                Title = "Уменьшить очередь ввода клавиатуры",
                Desc = "Меньше буфер нажатий клавиш до обработки — чуть отзывчивее клава.",
                Note = "после перезагрузки",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.LocalMachine, Sub = @"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters",
                            Name = "KeyboardDataQueueSize", Value = 20, Kind = RegistryValueKind.DWord, Label = "KeyboardDataQueueSize" },
                }
            },
            new ActionTweak
            {
                Id = "mk-usb-hid-nosleep", Group = "Мышь и клава", Risk = TweakRisk.Safe,
                Title = "Запретить USB усыплять мышь и клаву",
                Desc = "Снимает «разрешить отключение устройства для экономии энергии» со всех USB-хабов — "
                     + "мышь/клава не «подлагивают» после простоя.",
                Detector = () =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "$n=(Get-CimInstance -Namespace root\\wmi -ClassName MSPower_DeviceEnable -ErrorAction SilentlyContinue | " +
                        "Where-Object { $_.Enable -eq $true }).Count; if ($null -eq $n) { 'NA' } else { $n }", 30000);
                    string c = (outp ?? "").Replace("\r", "").Trim();
                    if (c == "NA" || c.Length == 0) return new TweakState { Status = TweakStatus.Unknown, Detail = "состояние USB-питания не прочитано" };
                    bool on = c == "0";
                    return new TweakState { Status = on ? TweakStatus.Applied : TweakStatus.NotApplied,
                                            Detail = on ? "усыпление отключено" : $"ещё спящих USB-устройств: {c}" };
                },
                Action = log =>
                {
                    var (code, _) = ProcessRunner.PowerShell(
                        "Get-CimInstance -Namespace root\\wmi -ClassName MSPower_DeviceEnable -ErrorAction SilentlyContinue | " +
                        "ForEach-Object { $_.Enable = $false; Set-CimInstance -InputObject $_ -ErrorAction SilentlyContinue }; 'OK'", 60000);
                    log("      USB power management для устройств снято");
                    return code == 0;
                },
                Undo = (snap, log) => { log("      (USB-питание можно вернуть в Диспетчере устройств; системного вреда нет)"); return true; }
            },

            // ═══════════════ 1.0: M1 ПК / СИСТЕМА ═══════════════════════
            RegTweak("perf-svchost-split", "Система", "Разделение служб svchost",
                "Windows отделяет службы по процессам на мощных ПК — сбой одной службы меньше влияет на игру.",
                TweakRisk.Safe, RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 256),
            RegTweak("ui-anim-off", "Система", "Отключить анимации окон",
                "Убирает анимации интерфейса Windows — окна открываются сразу, без лишней работы композитора.",
                TweakRisk.Safe, RegistryHive.CurrentUser,
                @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0"),
            new ActionTweak
            {
                Id = "net-eee-off", Group = "Сеть", Risk = TweakRisk.Caution,
                Title = "Отключить Energy Efficient Ethernet (EEE)",
                Desc = "Просит драйвер сетевой карты не усыплять линию в простое. " +
                       "Применяется к конкретным устройствам, чей драйвер поддерживает ключ; " +
                       "если ни один драйвер его не знает — твик помечается «недоступно», а не «включено».",
                Note = "зависит от драйвера сетевой карты",
                Detector = () =>
                {
                    try
                    {
                        int supported = 0, on = 0;
                        using var cls = Reg.Open(RegistryHive.LocalMachine, EeeClass, false);
                        if (cls == null)
                            return new TweakState { Status = TweakStatus.Unsupported, Detail = "класс сетевых адаптеров не найден" };
                        foreach (var sub in cls.GetSubKeyNames())
                        {
                            using var dev = cls.OpenSubKey(sub);
                            var v = dev?.GetValue("EnergyEfficientEthernet");
                            if (v == null) continue;
                            supported++;
                            try { if (Convert.ToInt32(v) != 0) on++; } catch { on++; }
                        }
                        if (supported == 0)
                            return new TweakState { Status = TweakStatus.Unsupported, Detail = "драйверы сетевых карт не поддерживают ключ EEE" };
                        return new TweakState
                        {
                            Status = on == 0 ? TweakStatus.Applied : (on < supported ? TweakStatus.Partial : TweakStatus.NotApplied),
                            Detail = $"устройств с ключом: {supported}, EEE включён на: {on}"
                        };
                    }
                    catch
                    {
                        return new TweakState { Status = TweakStatus.Unknown, Detail = "нужны права администратора, чтобы читать драйвер сетевой карты" };
                    }
                },
                Action = log =>
                {
                    int done = 0;
                    using var cls = Reg.Open(RegistryHive.LocalMachine, EeeClass, true);
                    if (cls == null)
                    {
                        log("      нет доступа к ключу класса сетевых адаптеров — нужны права администратора");
                        return false;
                    }
                    {
                        foreach (var sub in cls.GetSubKeyNames())
                        {
                            using var dev = cls.OpenSubKey(sub, true);
                            if (dev == null || dev.GetValue("EnergyEfficientEthernet") == null) continue;
                            string full = EeeClass + "\\" + sub;
                            if (BackupManager.WriteTracked(RegistryHive.LocalMachine, full, "EnergyEfficientEthernet", 0, RegistryValueKind.DWord))
                            {
                                done++;
                                log($"      EEE -> 0: ...\\{sub}");
                            }
                        }
                    }
                    log(done > 0
                        ? $"      EEE отключён на {done} устройств(е)"
                        : "      драйверы не поддерживают ключ EEE — пропущено, ничего не изменено");
                    return true;
                }
            },

            new ActionTweak
            {
                Id = "sys-autostart-cleanup", Group = "Система", Risk = TweakRisk.Caution,
                Title = "Убрать мусорную автозагрузку",
                Desc = "Удаляет из HKCU Run автозапуск браузеров и оверлеев (Yandex, Chrome, Firefox, Roblox, Riot Client, Epic, Overwolf, Electron-мусор). " +
                       "Steam, Discord, античиты и всё остальное не трогаются. Откат — по твику или профилем «Вернуть к заводским».",
                Detector = () =>
                {
                    using var k = Reg.Open(RegistryHive.CurrentUser,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                    var names = k?.GetValueNames() ?? Array.Empty<string>();
                    int junk = names.Count(n => SystemTweaks.IsJunkStartup(n));
                    return new TweakState
                    {
                        Status = junk == 0 ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = junk == 0
                            ? "мусора в автозагрузке нет"
                            : $"лишних записей: {junk} ({string.Join(", ", names.Where(n => SystemTweaks.IsJunkStartup(n)).Take(3))})"
                    };
                },
                Action = log => { SystemTweaks.CleanStartup(log); return true; }
            },

            // ═══════════════ 1.0: M2 МЫШЬ И КЛАВА ═══════════════════════
            new RegistryTweak
            {
                Id = "mk-filter-keys-off", Group = "Мышь и клава", Risk = TweakRisk.Safe,
                Title = "Отключить фильтрацию клавиш",
                Desc = "Убирает задержку и игнорирование быстрых повторных нажатий клавиш.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Accessibility\Keyboard Response", Name = "Flags", Value = "510", Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Accessibility\Keyboard Response", Name = "UserConfigFilterKeys", Value = 0, Kind = RegistryValueKind.DWord }
                }
            },
            new RegistryTweak
            {
                Id = "mk-sticky-keys-off", Group = "Мышь и клава", Risk = TweakRisk.Safe,
                Title = "Отключить залипание клавиш",
                Desc = "Не включает Sticky Keys от случайных многократных нажатий Shift.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Accessibility\StickyKeys", Name = "Flags", Value = "510", Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Accessibility\StickyKeys", Name = "UserConfigStickyKeys", Value = 0, Kind = RegistryValueKind.DWord }
                }
            },
            new RegistryTweak
            {
                Id = "mk-kbd-response", Group = "Мышь и клава", Risk = TweakRisk.Safe,
                Title = "Быстрый отклик клавиатуры",
                Desc = "Ставит минимальную задержку автоповтора и максимальную скорость повтора клавиш.",
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Keyboard", Name = "KeyboardResponse", Value = "1", Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Keyboard", Name = "KeyboardDelay", Value = "0", Kind = RegistryValueKind.String },
                    new() { Hive = RegistryHive.CurrentUser, Sub = @"Control Panel\Keyboard", Name = "KeyboardSpeed", Value = "31", Kind = RegistryValueKind.String }
                }
            },
            new ActionTweak
            {
                Id = "mk-bt-off", Group = "Мышь и клава", Risk = TweakRisk.Caution,
                Title = "Отключить Bluetooth-радио",
                Desc = "Отключает Bluetooth-устройства до отката — беспроводной ввод может получить дополнительную задержку.",
                Note = "не применять с Bluetooth-мышью/клавиатурой",
                Detector = () =>
                {
                    var (_, output) = ProcessRunner.PowerShell(
                        "@(Get-PnpDevice -Class Bluetooth -ErrorAction SilentlyContinue | Where-Object Status -eq 'OK').Count", 30000);
                    int.TryParse((output ?? "").Trim(), out int count);
                    return new TweakState
                    {
                        Status = count == 0 ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = count == 0 ? "активных Bluetooth-устройств нет" : $"активных устройств: {count}"
                    };
                },
                Action = log =>
                {
                    var (_, output) = ProcessRunner.PowerShell(
                        "Get-PnpDevice -Class Bluetooth -Status OK -ErrorAction SilentlyContinue | ForEach-Object { $_.InstanceId }", 30000);
                    var ids = (output ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                    BackupManager.Current.Meta["bt-devices"] = string.Join("\n", ids);
                    foreach (var id in ids)
                        ProcessRunner.PowerShell($"Disable-PnpDevice -InstanceId '{id.Replace("'", "''")}' -Confirm:$false -ErrorAction SilentlyContinue", 30000);
                    log($"      Bluetooth-устройств отключено: {ids.Length}");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    string stored = snap?.Meta != null && snap.Meta.TryGetValue("bt-devices", out string ids) ? ids : "";
                    var list = stored.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var id in list)
                        ProcessRunner.PowerShell($"Enable-PnpDevice -InstanceId '{id.Replace("'", "''")}' -Confirm:$false -ErrorAction SilentlyContinue", 30000);
                    log($"      Bluetooth-устройств включено обратно: {list.Length}");
                    return true;
                }
            },

            // ═══════════════ 1.0: ПК / ОТЗЫВЧИВОСТЬ ═══════════════
            RegTweak("perf-priority-sep", "Система", "Приоритет активной игре (Win32PrioritySeparation)",
                "Windows отдаёт больше процессорного времени окну, с которым работаешь — то есть игре в фокусе.",
                TweakRisk.Safe, RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 0x26),
            new RegistryTweak
            {
                Id = "perf-power-throttling-off", Group = "Система", Risk = TweakRisk.Caution,
                Title = "Отключить Power Throttling",
                Desc = "Windows перестаёт «придушивать» фоновые потоки ради энергосбережения — ровнее работа на всех ядрах.",
                Note = "на ноутбуке сильнее расход батареи",
                NotForReason = rig => rig.IsLaptop ? "на ноутбуке ускорит разряд батареи (можно, но осознанно)" : null,
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.LocalMachine,
                            Sub = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                            Name = "PowerThrottlingOff", Value = 1, Kind = RegistryValueKind.DWord, Label = "PowerThrottlingOff" },
                }
            },
            new ActionTweak
            {
                Id = "perf-ntfs-speed", Group = "Система", Risk = TweakRisk.Safe,
                Title = "Ускорить NTFS (8dot3 + LastAccess off)",
                Desc = "Отключает создание коротких DOS-имён и отметок «время последнего доступа» — "
                     + "меньше лишней работы диска при каждом открытии файла.",
                Detector = () =>
                {
                    var (_, o1) = ProcessRunner.Run("fsutil.exe", "behavior query disable8dot3");
                    var (_, o2) = ProcessRunner.Run("fsutil.exe", "behavior query disablelastaccess");
                    bool a = (o1 ?? "").Contains("= 1") || (o1 ?? "").Contains(": 1");
                    bool b = (o2 ?? "").Contains("= 1") || (o2 ?? "").Contains("= 3") || (o2 ?? "").Contains(": 1") || (o2 ?? "").Contains(": 3");
                    return new TweakState { Status = (a && b) ? TweakStatus.Applied : (a || b ? TweakStatus.Partial : TweakStatus.NotApplied),
                                            Detail = $"8dot3={(a ? "off" : "on")}, lastaccess={(b ? "off" : "on")}" };
                },
                Action = log =>
                {
                    ProcessRunner.Run("fsutil.exe", "behavior set disable8dot3 1");
                    ProcessRunner.Run("fsutil.exe", "behavior set disablelastaccess 1");
                    log("      8dot3-имена и LastAccess отключены");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    ProcessRunner.Run("fsutil.exe", "behavior set disable8dot3 2");
                    ProcessRunner.Run("fsutil.exe", "behavior set disablelastaccess 0");
                    log("      NTFS-поведение возвращено по умолчанию");
                    return true;
                }
            },

            // ═══════════════ 1.0: GPU MSI-MODE (только дискретная) ═══════════════
            new ActionTweak
            {
                Id = "gpu-msi-mode", Group = "GPU", Risk = TweakRisk.Caution,
                Title = "MSI-mode для видеокарты (меньше микрофризов)",
                Desc = "Переводит видеокарту на сигнальные прерывания (MSI) вместо линейных — "
                     + "частая причина падения DPC-латентности и микрофризов. Откат вернёт как было.",
                Note = "применяется после перезагрузки",
                NotForReason = rig => rig.GpuVendor != "NVIDIA" && rig.GpuVendor != "AMD" && rig.GpuVendor != "Intel"
                                      ? "видеокарта не определена" : null,
                Detector = () =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(MsiModeReadScript(), 40000);
                    string s = (outp ?? "").Replace("\r", "").Trim();
                    if (s.Length == 0) return new TweakState { Status = TweakStatus.Unknown, Detail = "не удалось прочитать прерывания GPU" };
                    bool on = s.StartsWith("1");
                    return new TweakState { Status = on ? TweakStatus.Applied : TweakStatus.NotApplied,
                                            Detail = on ? "MSI включён" : "MSI выключен (линейные прерывания)" };
                },
                Action = log =>
                {
                    var (code, _) = ProcessRunner.PowerShell(MsiModeWriteScript(1), 40000);
                    log("      MSI-mode включён для видеокарты (нужна перезагрузка)");
                    return code == 0;
                },
                Undo = (snap, log) =>
                {
                    ProcessRunner.PowerShell(MsiModeWriteScript(0), 40000);
                    log("      MSI-mode возвращён в линейный режим");
                    return true;
                }
            },

            // ═══════════════ 1.0: СЕТЬ+ ═══════════════
            new ActionTweak
            {
                Id = "net-nagle-all", Group = "Сеть", Risk = TweakRisk.Safe,
                Title = "Убрать Nagle на всех интерфейсах",
                Desc = "Пакеты уходят сразу, не копятся в буфере — ниже задержка в онлайн-играх. "
                     + "Проходит по ВСЕМ сетевым интерфейсам, а не по одному.",
                Detector = () =>
                {
                    var (a, t, detail) = TcpParams.Read("TcpAckFrequency", 1);
                    if (t == 0) return new TweakState { Status = TweakStatus.Unsupported, Detail = detail };
                    return new TweakState { Status = a == t ? TweakStatus.Applied : (a > 0 ? TweakStatus.Partial : TweakStatus.NotApplied), Detail = detail };
                },
                Action = log =>
                {
                    int n1 = TcpParams.Write("TcpAckFrequency", 1, log);
                    int n2 = TcpParams.Write("TCPNoDelay", 1, log);
                    log($"      Nagle снят на интерфейсах: TcpAckFrequency({n1}), TCPNoDelay({n2})");
                    return n1 > 0 || n2 > 0;
                }
            },
            new ActionTweak
            {
                Id = "net-offload-off", Group = "Сеть", Risk = TweakRisk.Caution,
                Title = "Отключить сетевые разгрузки (RSC/LSO)",
                Desc = "Receive Segment Coalescing и Large Send Offload склеивают пакеты ради пропускной "
                     + "способности, но добавляют задержку — для игр лучше без них.",
                Detector = () =>
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "$r=(Get-NetAdapterRsc -ErrorAction SilentlyContinue | Where-Object { $_.IPv4Enabled }).Count; if($null -eq $r){'0'}else{$r}", 30000);
                    string c = (outp ?? "").Replace("\r", "").Trim();
                    bool off = c == "0";
                    return new TweakState { Status = off ? TweakStatus.Applied : TweakStatus.NotApplied,
                                            Detail = off ? "RSC выключен" : $"RSC ещё включён на адаптерах: {c}" };
                },
                Action = log =>
                {
                    ProcessRunner.PowerShell("Disable-NetAdapterRsc -Name '*' -ErrorAction SilentlyContinue; " +
                        "Disable-NetAdapterLso -Name '*' -ErrorAction SilentlyContinue; 'OK'", 60000);
                    log("      RSC и LSO отключены на всех адаптерах");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    ProcessRunner.PowerShell("Enable-NetAdapterRsc -Name '*' -ErrorAction SilentlyContinue; " +
                        "Enable-NetAdapterLso -Name '*' -ErrorAction SilentlyContinue; 'OK'", 60000);
                    log("      RSC и LSO возвращены");
                    return true;
                }
            },

            // ═══════════════ 1.0: РИСК-ТВИКИ (по умолчанию выкл) ═══════════════
            new ActionTweak
            {
                Id = "risk-mitigations-off", Group = "Система", Risk = TweakRisk.Risky,
                Title = "Отключить защиты ЦП (Spectre/Meltdown mitigations)",
                Desc = "Даёт прибавку FPS на процессоре, НО снижает защиту от уязвимостей Spectre/Meltdown. "
                     + "Только для изолированной игровой машины. Откат включает защиты обратно.",
                Note = "СНИЖАЕТ безопасность · нужна перезагрузка",
                RequiresReboot = true,
                Detector = () =>
                {
                    int ov = ToInt(Reg.Read(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride"));
                    int om = ToInt(Reg.Read(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask"));
                    bool on = ov == 3 && om == 3;
                    return new TweakState { Status = on ? TweakStatus.Applied : TweakStatus.NotApplied,
                                            Detail = $"Override={ov}, Mask={om}" };
                },
                Action = log =>
                {
                    BackupManager.WriteTracked(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3, RegistryValueKind.DWord);
                    BackupManager.WriteTracked(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
                    log("      Защиты ЦП отключены (нужна перезагрузка)");
                    return true;
                }
            },
            new RegistryTweak
            {
                Id = "risk-memory-compression-off", Group = "Система", Risk = TweakRisk.Risky,
                Title = "Отключить сжатие памяти",
                Desc = "Меньше работы ЦП по сжатию ОЗУ (плюс к latency на мощных системах с запасом памяти). "
                     + "На системах с малым ОЗУ может чаще уходить в файл подкачки.",
                Note = "лучше при 16+ ГБ ОЗУ",
                NotForReason = rig => rig.RamTotalGb > 0 && rig.RamTotalGb < 16
                                      ? "не рекомендуется при ОЗУ меньше 16 ГБ" : null,
                Writes = new List<RegWrite>
                {
                    new() { Hive = RegistryHive.LocalMachine,
                            Sub = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                            Name = "DisablePagingExecutive", Value = 1, Kind = RegistryValueKind.DWord, Label = "DisablePagingExecutive" },
                }
            },
        };

        // ── helpers ──────────────────────────────────────────────────────
        private static RegistryTweak RegTweak(string id, string group, string title, string desc, TweakRisk risk,
            RegistryHive hive, string sub, string name, object value, bool reboot = false, string note = "")
        {
            return new RegistryTweak
            {
                Id = id, Group = group, Title = title, Desc = desc, Risk = risk,
                Note = note, RequiresReboot = reboot,
                Writes = new List<RegWrite>
                {
                    new() { Hive = hive, Sub = sub, Name = name, Value = value }
                }
            };
        }

        /// <summary>
        /// TCP latency parameter. Windows reads these per interface, so the tweak
        /// resolves the active ones instead of only writing the legacy global key.
        /// </summary>
        private static ActionTweak TcpTweak(string id, string title, string desc,
            string valueName, int value, TweakRisk risk, string note = "")
        {
            return new ActionTweak
            {
                Id = id, Group = "Латентность", Title = title, Desc = desc, Risk = risk, Note = note,
                Detector = () =>
                {
                    var (applied, total, detail) = TcpParams.Read(valueName, value);
                    if (total == 0)
                        return new TweakState { Status = TweakStatus.Unsupported, Detail = detail };
                    return new TweakState
                    {
                        Status = applied == total ? TweakStatus.Applied
                               : applied > 0     ? TweakStatus.Partial
                               : TweakStatus.NotApplied,
                        Detail = detail
                    };
                },
                Action = log => TcpParams.Write(valueName, value, log) > 0
            };
        }

        private static ActionTweak PowerTweak(string id, string title, string desc,
            string sub, string setting, uint onValue, uint offValue, TweakRisk risk)
        {
            return new ActionTweak
            {
                Id = id, Group = "Питание", Title = title, Desc = desc, Risk = risk,
                Detector = () =>
                {
                    var v = ReadPowerSetting(sub, setting);
                    if (v == null) return new TweakState { Status = TweakStatus.Unsupported, Detail = "настройка недоступна" };
                    return new TweakState
                    {
                        Status = v.Value == onValue ? TweakStatus.Applied : TweakStatus.NotApplied,
                        Detail = $"текущее значение: {v.Value}"
                    };
                },
                Action = log =>
                {
                    // remember what it actually was, not a hard-coded default
                    var before = ReadPowerSetting(sub, setting);
                    if (before.HasValue)
                        BackupManager.Current.Meta["power:" + id] = before.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    ProcessRunner.Run("powercfg.exe", $"/setacvalueindex SCHEME_CURRENT {sub} {setting} {onValue}");
                    ProcessRunner.Run("powercfg.exe", $"/setdcvalueindex SCHEME_CURRENT {sub} {setting} {onValue}");
                    ProcessRunner.Run("powercfg.exe", "/setactive SCHEME_CURRENT");
                    log($"      {title}: {(before.HasValue ? before.Value.ToString() : "?")} -> {onValue}");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    uint back = offValue;
                    if (snap?.Meta != null && snap.Meta.TryGetValue("power:" + id, out string stored) &&
                        uint.TryParse(stored, out uint parsed)) back = parsed;

                    ProcessRunner.Run("powercfg.exe", $"/setacvalueindex SCHEME_CURRENT {sub} {setting} {back}");
                    ProcessRunner.Run("powercfg.exe", $"/setdcvalueindex SCHEME_CURRENT {sub} {setting} {back}");
                    ProcessRunner.Run("powercfg.exe", "/setactive SCHEME_CURRENT");
                    log($"      {title} -> {back} (как было)");
                    return true;
                }
            };
        }

        private static ActionTweak BcdTweak(string id, string group, string title, string desc,
            string key, string value, TweakRisk risk, bool reboot = false)
        {
            return new ActionTweak
            {
                Id = id, Group = group, Title = title, Desc = desc, Risk = risk, RequiresReboot = reboot,
                Detector = () =>
                {
                    var (_, text) = ProcessRunner.Run("bcdedit.exe", "/enum {current}");
                    foreach (var line in (text ?? "").Split('\n'))
                    {
                        var t = line.Trim();
                        if (!t.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
                        var parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        string v = parts.Length >= 2 ? parts[1] : "";
                        return new TweakState
                        {
                            Status = string.Equals(v, value, StringComparison.OrdinalIgnoreCase)
                                     ? TweakStatus.Applied : TweakStatus.NotApplied,
                            Detail = $"{key} = {v}"
                        };
                    }
                    return new TweakState { Status = TweakStatus.NotApplied, Detail = $"{key} не задан" };
                },
                Action = log =>
                {
                    // store the pre-existing value (may be absent) so undo is exact
                    var (_, before) = ProcessRunner.Run("bcdedit.exe", "/enum {current}");
                    string prev = "";
                    foreach (var line in (before ?? "").Split('\n'))
                    {
                        var t = line.Trim();
                        if (!t.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
                        var parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) prev = parts[1];
                    }
                    BackupManager.Current.Meta["bcd:" + key] = prev;

                    ProcessRunner.Run("bcdedit.exe", $"/set {key} {value}");
                    log($"      bcdedit /set {key} {value}   (было: {(string.IsNullOrEmpty(prev) ? "не задано" : prev)})");
                    return true;
                },
                Undo = (snap, log) =>
                {
                    string prev = null;
                    snap?.Meta?.TryGetValue("bcd:" + key, out prev);

                    if (!string.IsNullOrEmpty(prev))
                    {
                        ProcessRunner.Run("bcdedit.exe", $"/set {key} {prev}");
                        log($"      bcdedit /set {key} {prev} (как было)");
                    }
                    else
                    {
                        ProcessRunner.Run("bcdedit.exe", $"/deletevalue {key}");
                        log($"      bcdedit /deletevalue {key} (как было: не задано)");
                    }
                    return true;
                }
            };
        }

        private static TweakState FileState(string path)
        {
            bool configured = GameConfigWriter.IsConfigured(path);
            return new TweakState
            {
                Status = configured ? TweakStatus.Applied : TweakStatus.NotApplied,
                Detail = File.Exists(path) ? $"файл: {path}" : "файл autoexec.cfg не найден"
            };
        }

        /// <summary>
        /// powercfg prints four hex values in this order:
        /// minimum possible, maximum possible, current AC, current DC.
        /// The current AC index is therefore the third one — taking the first
        /// would always read the minimum possible value.
        /// </summary>
        private static uint? ReadPowerSetting(string sub, string setting)
        {
            var (code, output) = ProcessRunner.Run("powercfg.exe", $"/query SCHEME_CURRENT {sub} {setting}");
            if (code != 0 || string.IsNullOrWhiteSpace(output)) return null;

            var matches = Regex.Matches(output, @"0x([0-9a-fA-F]{8})");
            if (matches.Count == 0) return null;

            // 5 values = min, max, increment, AC, DC  -> AC is index 3
            // 4 values = min, max, AC, DC            -> AC is index 2
            // 3 values = min, max, AC                -> AC is index 2
            int index = matches.Count >= 5 ? 3 : (matches.Count >= 3 ? 2 : 0);
            return Convert.ToUInt32(matches[index].Groups[1].Value, 16);
        }

        private static int ToInt(object v)
        {
            try { return v == null ? 0 : Convert.ToInt32(v); }
            catch { return 0; }
        }

        // ── 1.0: MSI-mode для видеокарты через Interrupt Management ──────────
        private const string MsiFindGpu =
            "$g = Get-PnpDevice -Class Display -Status OK -ErrorAction SilentlyContinue | Select-Object -First 1; " +
            "if ($null -eq $g) { return $null }; " +
            "$k = \"HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\$($g.InstanceId)\\Device Parameters\\Interrupt Management\\MessageSignaledInterruptProperties\"; ";

        private static string MsiModeReadScript() =>
            MsiFindGpu +
            "if (Test-Path $k) { (Get-ItemProperty -Path $k -Name MSISupported -ErrorAction SilentlyContinue).MSISupported } else { '' }";

        private static string MsiModeWriteScript(int value) =>
            MsiFindGpu +
            $"New-Item -Path $k -Force | Out-Null; New-ItemProperty -Path $k -Name MSISupported -Value {value} -PropertyType DWord -Force | Out-Null; 'OK'";


        /// <summary>
        /// Steam rewrites localconfig.vdf when it exits, so the edit is only
        /// durable if Steam is not running — close it first.
        /// </summary>
        private static bool WriteLaunchOptions(string appId, string value, Action<string> log)
        {
            string cfg = SteamLaunchOptions.ActiveConfig();
            if (string.IsNullOrEmpty(cfg))
            {
                log("      Steam не найден — установлен ли он вообще?");
                return false;
            }
            log($"      конфиг Steam: {cfg}");

            if (SteamLaunchOptions.IsSteamRunning())
            {
                log("      Steam запущен — закрываю, иначе он перезапишет правку");
                if (!SteamLaunchOptions.ShutdownSteam(log))
                {
                    log("      закрой Steam вручную и повтори");
                    return false;
                }
            }

            bool ok = SteamLaunchOptions.Write(cfg, appId, value, log);
            if (ok) log($"      проверка: \"{SteamLaunchOptions.Read(cfg, appId)}\"");
            return ok;
        }

        /// <summary>
        /// Discord keeps its overlay switch in settings.json. If Discord is running
        /// it may overwrite the file on exit, so the log says so plainly.
        /// </summary>
        private static void DisableDiscordOverlay(Action<string> log)
        {
            try
            {
                string file = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "discord", "settings.json");

                if (!File.Exists(file)) { log("      Discord не установлен — пропуск"); return; }

                string text = File.ReadAllText(file);
                if (Regex.IsMatch(text, "\"enableOverlay\"\\s*:\\s*false"))
                {
                    log("      оверлей Discord уже выключен в настройках");
                    return;
                }

                BackupManager.TrackFile(file);

                if (Regex.IsMatch(text, "\"enableOverlay\"\\s*:\\s*true"))
                    text = Regex.Replace(text, "\"enableOverlay\"\\s*:\\s*true", "\"enableOverlay\":false");
                else
                {
                    int brace = text.IndexOf('{');
                    if (brace < 0) { log("      неожиданный формат настроек Discord — пропуск"); return; }
                    text = text.Insert(brace + 1, "\"enableOverlay\":false,");
                }

                File.WriteAllText(file, text);
                bool running = System.Diagnostics.Process.GetProcessesByName("Discord").Length > 0;
                log(running
                    ? "      оверлей Discord выключен в файле, но Discord запущен — " +
                      "он может перезаписать настройку при выходе (надёжнее выключить в самом Discord)"
                    : "      оверлей Discord выключен в настройках");
            }
            catch (Exception ex) { log($"      оверлей Discord: {ex.Message}"); }
        }

        /// <summary>Class key for network adapters — EEE is a per-device value inside 0000/0001/... subkeys.</summary>
        private const string EeeClass =
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

        private static string NicForeach(string body) =>
            "Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -and $_.Name -notlike 'vEthernet*' } | ForEach-Object { " + body + " }";

        private static bool RestoreNic(Action<string> log)
        {
            string snap = Path.Combine(BackupManager.Root, "nic-advanced-original.txt");
            string legacy = Path.Combine(BackupManager.Root, "nic-advanced-properties.txt");
            if (!File.Exists(snap) && File.Exists(legacy)) { try { File.Copy(legacy, snap); } catch { } }
            if (!File.Exists(snap)) { log("      исходный снимок сетевой карты не найден"); return false; }

            int restored = 0;
            foreach (var line in File.ReadAllLines(snap))
            {
                var p = line.Split('|');
                if (p.Length < 4) continue;
                string adapter = p[0].Trim(), keyword = p[1].Trim(), value = p[2].Trim();
                if (string.IsNullOrWhiteSpace(adapter) || string.IsNullOrWhiteSpace(keyword)) continue;
                var (_, outp) = ProcessRunner.PowerShell(
                    $"Set-NetAdapterAdvancedProperty -Name '{adapter}' -RegistryKeyword '{keyword}' " +
                    $"-RegistryValue {value} -ErrorAction SilentlyContinue; 'OK'", 30000);
                if (outp != null && outp.Contains("OK")) restored++;
            }
            log($"      параметров сетевой карты восстановлено: {restored}");
            return restored > 0;
        }

        private static string Nv(string args)
        {
            var (code, o) = ProcessRunner.Run("nvidia-smi.exe", args, 15000);
            return code == 0 && !string.IsNullOrWhiteSpace(o) ? o.Trim() : null;
        }

        private static bool NvRun(string args, Action<string> log, string message)
        {
            var (code, o) = ProcessRunner.Run("nvidia-smi.exe", args, 20000);
            if (code == 0) { log($"      {message}"); return true; }
            log($"      не удалось: nvidia-smi {args} -> {(o ?? "").Trim().Split('\n').FirstOrDefault()}");
            return false;
        }

        private static double ParseD(string s) =>
            double.TryParse((s ?? "").Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0;
    }
}
