using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using GameOptimizer.Core.Backup;

namespace GameOptimizer.Core.Games
{
    /// <summary>
    /// Writes the convar blocks for CS2 and Dota 2. The player's own autoexec
    /// content is preserved — v6 only replaces its own marked block.
    /// </summary>
    public static class GameConfigWriter
    {
        private const string Begin = "// ==== GAMEOPTIMIZv7 BEGIN ====";
        private const string End   = "// ==== GAMEOPTIMIZv7 END ====";
        // also strips blocks written by earlier versions so nothing duplicates
        private const string AnyBlock = @"//\s*====\s*GAMEOPTIMIZv\d+\s*BEGIN\s*====[\s\S]*?//\s*====\s*GAMEOPTIMIZv\d+\s*END\s*====";

        public static string SteamPath
        {
            get
            {
                var v = Reg.Read(RegistryHive.CurrentUser, @"Software\Valve\Steam", "SteamPath")?.ToString();
                if (!string.IsNullOrWhiteSpace(v)) return v.Replace('/', '\\').TrimEnd('\\');
                return @"C:\Program Files (x86)\Steam";
            }
        }

        public static string CommonDir => Path.Combine(SteamPath, "steamapps", "common");

        public static string Cs2Autoexec => Path.Combine(
            CommonDir, "Counter-Strike Global Offensive", "game", "csgo", "cfg", "autoexec.cfg");

        public static string DotaAutoexec => Path.Combine(
            CommonDir, "dota 2 beta", "game", "dota", "cfg", "autoexec.cfg");

        /// <summary>True when our marked block is present (any version).</summary>
        public static bool IsConfigured(string path)
        {
            try
            {
                return File.Exists(path) &&
                       Regex.IsMatch(File.ReadAllText(path), @"GAMEOPTIMIZv\d+\s*BEGIN");
            }
            catch { return false; }
        }

        // ── CS2 convar block ─────────────────────────────────────────────
        private static string Cs2Block() => string.Join(Environment.NewLine, new[]
        {
            "// FPS: потолок под G-Sync (165 Гц - 3). Кадры стабильнее, чем без потолка.",
            "fps_max \"162\"",
            "// Reflex: 2 = On + Boost (оптимально, когда упор в видеокарту)",
            "r_low_latency \"2\"",
            "engine_low_latency_sleep_after_client_tick \"1\"",
            "engine_no_focus_sleep \"0\"",
            "",
            "// Сеть",
            "rate \"786432\"",
            "cl_net_buffer_ticks \"0\"",
            "mm_dedicated_search_maxping \"50\"",
            "",
            "// Звук",
            "snd_mixahead \"0.025\"",
            "snd_headphone_eq \"0\"",
            "snd_spatialize_lerp \"1\"",
            "snd_steamaudio_enable_reverb \"0\"",
            "",
            "// 6-ядерный i5-12400F: многопоточный рендер даёт лучше 1% low",
            "mat_queue_mode \"2\"",
            "",
            "cl_hide_avatar_images \"1\"",
            "con_enable \"1\"",
            "",
            "host_writeconfig"
        });

        // ── Dota 2 convar block ──────────────────────────────────────────
        private static string DotaBlock() => string.Join(Environment.NewLine, new[]
        {
            "// Кадры: потолок под G-Sync 165 Гц. fps_max 0 даёт более рваный frametime.",
            "fps_max 162",
            "fps_max_menu 120",
            "fps_vsync 0",
            "",
            "// Сеть",
            "rate 120000",
            "cl_updaterate 60",
            "cl_cmdrate 60",
            "net_maxrtt 150",
            "",
            "// Звук",
            "snd_mixahead 0.05",
            "",
            "// Рендер",
            "r_dynamic 0",
            "r_skin 0",
            "mat_antialias 0",
            "mat_queue_mode 2",
            "",
            "// Эффекты, которые не влияют на читаемость игры",
            "dota_ambient_creatures 0",
            "dota_ambient_cloth 0",
            "dota_cheap_water 1",
            "dota_portrait_animate 0",
            "dota_terrain_quality 0",
            "dota_weather_effects 0",
            "",
            "engine_no_focus_sleep 0",
            "con_enable 1"
        });

        /// <summary>Merge the v6 block into an autoexec file, keeping user content.</summary>
        private static bool Merge(string path, string block, Action<string> log)
        {
            try
            {
                string original = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
                BackupManager.TrackFile(path);

                string userPart = Regex.Replace(original, AnyBlock, string.Empty).TrimEnd();

                var sb = new StringBuilder();
                if (userPart.Length > 0) sb.Append(userPart).Append(Environment.NewLine).Append(Environment.NewLine);
                sb.Append(Begin).Append(Environment.NewLine)
                  .Append(block).Append(Environment.NewLine)
                  .Append(End).Append(Environment.NewLine);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                log($"      записан {path}");
                return true;
            }
            catch (Exception ex)
            {
                log($"      [ОШИБКА] {path}: {ex.Message}");
                return false;
            }
        }

        public static bool WriteAll(Action<string> log, bool cs2 = true, bool dota = true)
        {
            log("  [9] Конфиги игр:");
            bool any = false;

            if (cs2)
            {
                string csCfg = Path.GetDirectoryName(Cs2Autoexec);
                if (Directory.Exists(csCfg)) any |= Merge(Cs2Autoexec, Cs2Block(), log);
                else log("      CS2 не найден — пропуск");
            }

            if (dota)
            {
                string dotaCfg = Path.GetDirectoryName(DotaAutoexec);
                if (Directory.Exists(dotaCfg)) any |= Merge(DotaAutoexec, DotaBlock(), log);
                else log("      Dota 2 не найдена — пропуск");
            }

            if (!any) return false;

            // CS2 does not always auto-execute autoexec.cfg — tell the user how to be sure.
            var instr = new StringBuilder();
            instr.AppendLine("GAMEOPTIMIZ 1.0 — что донастроить руками");
            instr.AppendLine("=====================================");
            instr.AppendLine();
            instr.AppendLine("1) STEAM LAUNCH OPTIONS");
            instr.AppendLine("   CS2  (ПКМ по игре → Свойства → Параметры запуска):");
            instr.AppendLine("       -novid +exec autoexec.cfg");
            instr.AppendLine("   Dota 2:");
            instr.AppendLine("       -novid");
            instr.AppendLine("   Больше НИЧЕГО добавлять не нужно: -threads, +fps_max 0, -high, -freq,");
            instr.AppendLine("   +cl_forcepreload в CS2 либо не работают, либо ухудшают 1% low.");
            instr.AppendLine();
            instr.AppendLine("2) ВИДЕОКАРТА (Панель управления NVIDIA)");
            instr.AppendLine("   • Низкая задержка: Ultra  (или Reflex в самой игре)");
            instr.AppendLine("   • Режим управления электропитанием: Максимальная производительность");
            instr.AppendLine("   • Кэш шейдеров: 100 ГБ или Без ограничений");
            instr.AppendLine("   • Вертикальная синхронизация: Вкл (в паре с G-SYNC + потолок кадров)");
            instr.AppendLine("   • Максимальная частота кадров: 162");
            instr.AppendLine("   • Потоковая оптимизация: Авто   • Анизотропная фильтрация: 16x");
            instr.AppendLine("   • Кэш шейдеров, Качество фильтрации текстур: Высокая производительность");
            instr.AppendLine();
            instr.AppendLine("3) G-SYNC (Панель NVIDIA → Настройка G-SYNC)");
            instr.AppendLine("   • Включить G-SYNC  • Режим: для оконного и полноэкранного");
            instr.AppendLine("   • В Windows: Экран → Дополнительные параметры экрана → 165 Гц");
            instr.AppendLine();
            instr.AppendLine("4) CS2 — НАСТРОЙКИ ВИДЕО В ИГРЕ (2560x1440, RTX 3050)");
            instr.AppendLine("   Разрешение: 2560x1440 (если мало кадров — 1920x1440, это +20% FPS)");
            instr.AppendLine("   Режим экрана: Полноэкранный   • Вертикальная синхронизация: Выкл");
            instr.AppendLine("   NVIDIA Reflex: Вкл + Boost");
            instr.AppendLine("   Boost Player Contrast: Вкл (бесплатно по производительности)");
            instr.AppendLine("   Мультисэмплинг: CMAA2   • Сглаживание текстур: 4x");
            instr.AppendLine("   Тени: Низкие   • Детализация моделей/текстур: Низкие");
            instr.AppendLine("   Шейдеры: Низкие   • Частицы: Низкие   • Ambient Occlusion: Выкл");
            instr.AppendLine("   FidelityFX Super Resolution: Выкл (или «Наивысшее качество» — тестируй 1% low)");
            instr.AppendLine();
            instr.AppendLine("5) DOTA 2 — НАСТРОЙКИ ВИДЕО В ИГРЕ");
            instr.AppendLine("   Render Quality: 80-90%   • Vsync: Выкл   • Полноэкранный режим");
            instr.AppendLine("   Shadows: Medium/Low   • Tree/Environment: Medium");
            instr.AppendLine("   Effects: Medium   • Anti-Aliasing: Выкл/CMAA");
            instr.AppendLine();
            instr.AppendLine("6) ЧТО ДАЁТ БОЛЬШЕ ВСЕГО (по важности)");
            instr.AppendLine("   1. Вторая планка ОЗУ — двухканальный режим (сейчас один канал!)");
            instr.AppendLine("   2. Выключить VBS / Memory Integrity (профиль EXTREME)");
            instr.AppendLine("   3. Сетевой адаптер: убрать Green Ethernet / Gigabit Lite (линк 100 -> 1000 Мбит/с)");
            instr.AppendLine("   4. Оверлеи Discord и NVIDIA — выключить");
            instr.AppendLine("   5. Всё остальное, включая параметры запуска — на порядок слабее.");

            string outPath = Path.Combine(BackupManager.Root, "ЧТО-ДОНАСТРОИТЬ.txt");
            try
            {
                Directory.CreateDirectory(BackupManager.Root);
                File.WriteAllText(outPath, instr.ToString(), new UTF8Encoding(true));
                log($"      инструкция: {outPath}");
            }
            catch { }

            return true;
        }
    }
}
