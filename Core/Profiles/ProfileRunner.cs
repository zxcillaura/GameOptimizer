using System;
using System.Collections.Generic;
using System.Linq;
using GameOptimizer.Core.Backup;
using GameOptimizer.Core.Hardware;
using GameOptimizer.Core.Tweaks;

namespace GameOptimizer.Core.Profiles
{
    /// <summary>Профиль применения для 1.0.</summary>
    public enum ProfileKind { Factory, Safe, Balanced, Extreme }

    /// <summary>Категория оптимизации — то, что пользователь включает галочкой.</summary>
    public sealed class OptCategory
    {
        public string Key    { get; init; }
        public string Title  { get; init; }
        public string Desc   { get; init; }
        public string[] Groups { get; init; }
    }

    /// <summary>Фактический dry-run профиля: что войдёт, что будет пропущено и нужна ли перезагрузка.</summary>
    public sealed class ProfilePreview
    {
        public List<TweakItem> Items { get; init; } = new();
        public List<string> Skipped { get; init; } = new();
        public int SafeCount => Items.Count(t => t.Risk == TweakRisk.Safe);
        public int CautionCount => Items.Count(t => t.Risk == TweakRisk.Caution);
        public int RiskyCount => Items.Count(t => t.Risk == TweakRisk.Risky);
        public int RebootCount => Items.Count(t => t.RequiresReboot);
    }

    /// <summary>
    /// 1.0: профили Заводские / Баланс / Экстрим с выбором категорий.
    /// Работает поверх TweakCatalog: категория = набор групп твиков.
    /// Баланс = Safe+Caution; Экстрим = + Risky. Заводские = откат к сохранённому исходному состоянию.
    /// </summary>
    public static class ProfileRunner
    {
        public static readonly OptCategory[] Categories =
        {
            new() { Key = "pc",  Title = "ПК и производительность",
                    Desc = "Питание, планировщик ЦП, графика, память, GPU — максимум FPS и отзывчивости.",
                    Groups = new[] { "Питание", "Латентность", "Память", "Графика", "GPU", "Игры" } },
            new() { Key = "net", Title = "Сеть и пинг",
                    Desc = "Задержка, Nagle, QoS, DNS, буферы — ниже пинг и лаг в онлайне.",
                    Groups = new[] { "Сеть" } },
            new() { Key = "mk",  Title = "Мышь и клавиатура",
                    Desc = "Инпут-лаг: мышь 1:1, очереди ввода, USB без усыпления.",
                    Groups = new[] { "Мышь и клава" } },
            new() { Key = "sys", Title = "Система и фон",
                    Desc = "Телеметрия, фоновые службы и задачи, визуальные эффекты, VBS (в Экстриме).",
                    Groups = new[] { "Система", "Службы", "Задачи" } },
        };

        public static string Title(ProfileKind p) => p switch
        {
            ProfileKind.Factory  => "Вернуть к заводским",
            ProfileKind.Safe     => "Безопасный",
            ProfileKind.Balanced => "Баланс",
            ProfileKind.Extreme  => "Экстрим",
            _ => "?"
        };

        public static string Describe(ProfileKind p) => p switch
        {
            ProfileKind.Factory  => "Откат к точке восстановления и исходным значениям всех наших твиков в выбранных категориях — всё встаёт на место.",
            ProfileKind.Safe     => "Только твики с уровнем «безопасно». Минимум вмешательства, но результат всё равно проверяй бенчмарком.",
            ProfileKind.Balanced => "Безопасные и умеренные твики. Рекомендуется большинству после просмотра dry-run.",
            ProfileKind.Extreme  => "Всё из Баланса + рискованные твики (mitigations off, VBS off и т.п.). Для изолированной игровой машины.",
            _ => ""
        };

        private static bool RiskAllowed(ProfileKind p, TweakRisk r) => p switch
        {
            ProfileKind.Safe     => r == TweakRisk.Safe,
            ProfileKind.Balanced => r == TweakRisk.Safe || r == TweakRisk.Caution,
            ProfileKind.Extreme  => true,
            _ => true
        };

        /// <summary>Формирует честный dry-run без системных изменений.</summary>
        public static ProfilePreview Preview(ProfileKind profile, IEnumerable<string> categoryKeys, RigReport rig)
        {
            var keys = new HashSet<string>(categoryKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var groups = new HashSet<string>(
                Categories.Where(c => keys.Contains(c.Key)).SelectMany(c => c.Groups),
                StringComparer.OrdinalIgnoreCase);
            var preview = new ProfilePreview();

            foreach (var t in TweakCatalog.Build())
            {
                if (!groups.Contains(t.Group)) continue;
                if (profile != ProfileKind.Factory && !RiskAllowed(profile, t.Risk))
                {
                    preview.Skipped.Add($"{t.Title}: уровень риска не входит в профиль «{Title(profile)}»");
                    continue;
                }

                string notFor = profile == ProfileKind.Factory ? null : t.Applicability(rig);
                if (!string.IsNullOrWhiteSpace(notFor))
                {
                    preview.Skipped.Add($"{t.Title}: {notFor}");
                    continue;
                }

                preview.Items.Add(t);
            }
            return preview;
        }

        /// <summary>Твики выбранных категорий, подходящие железу и уровню риска профиля.</summary>
        public static List<TweakItem> Select(ProfileKind profile, IEnumerable<string> categoryKeys, RigReport rig)
            => Preview(profile, categoryKeys, rig).Items;

        /// <summary>
        /// Применяет профиль к выбранным категориям.
        /// Перед Баланс/Экстрим — точка восстановления (always). Заводские — откат.
        /// </summary>
        public static string Run(ProfileKind profile, IEnumerable<string> categoryKeys,
                                 bool withRestorePoint, Action<string> log)
        {
            var keys = categoryKeys?.ToArray() ?? Array.Empty<string>();
            void Say(string s) => log?.Invoke(s);

            Say("══════════════════════════════════════════════════════════");
            Say($"  Optimization by zxcillaura — профиль «{Title(profile)}»");
            Say($"  Категории: {string.Join(", ", Categories.Where(c => keys.Contains(c.Key)).Select(c => c.Title))}");
            Say($"  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Say("══════════════════════════════════════════════════════════");

            if (!Tweaks.SystemTweaks.IsAdmin())
            {
                Say("  !! НЕТ ПРАВ АДМИНИСТРАТОРА — профиль не запущен, чтобы не получить частично изменённую систему.");
                return "";
            }

            if (keys.Length == 0) { Say("  Не выбрано ни одной категории — нечего делать."); return ""; }

            var rig = HardwareScanner.Scan();
            var preview = Preview(profile, keys, rig);
            var items = preview.Items;
            Say($"  Dry-run: {items.Count} действий; безопасно {preview.SafeCount}; осторожно {preview.CautionCount}; риск {preview.RiskyCount}; перезагрузка {preview.RebootCount}.");
            if (preview.Skipped.Count > 0) Say($"  Не подходят этой системе/профилю: {preview.Skipped.Count}.");

            if (profile == ProfileKind.Factory)
            {
                Say("");
                Say("  ВОЗВРАТ К ЗАВОДСКИМ (откат наших изменений в выбранных категориях):");
                int reverted = 0;
                foreach (var t in items)
                {
                    if (TweakRunner.HasBackup(t.Id)) { TweakRunner.RevertOne(t, log); reverted++; }
                }
                Say("");
                Say($"  Откатано твиков: {reverted}. Значения вернулись к исходным.");
                Say("  Если делал точку восстановления — её всегда можно применить через «Восстановление системы» Windows.");
                return "";
            }

            // Баланс / Экстрим
            BackupManager.Begin(Title(profile));
            Say("");
            Say($"  [0] Бэкап: {BackupManager.BackupDir}");
            if (withRestorePoint) Tweaks.SystemTweaks.CreateRestorePoint(Say);

            Say("");
            Say($"  ПРИМЕНЕНИЕ ({items.Count} твиков):");
            var (ok, bad) = TweakRunner.ApplyMany(items, log);

            string backupPath = BackupManager.Save();
            Say("");
            Say("══════════════════════════════════════════════════════════");
            Say($"  Применено: {ok}, с ошибкой: {bad}");
            Say($"  Бэкап: {backupPath}");
            Say("  Откат: профиль «Вернуть к заводским» с теми же категориями.");
            if (items.Any(i => i.RequiresReboot)) Say("  ПЕРЕЗАГРУЗКА: нужна (часть твиков вступит в силу после неё).");
            Say("══════════════════════════════════════════════════════════");
            return backupPath;
        }
    }
}
