using System;
using System.Collections.Generic;
using System.Linq;
using GameOptimizer.Core.Hardware;

namespace GameOptimizer.Core.Health
{
    public sealed class HealthIssue
    {
        public string Title    { get; set; } = string.Empty;
        public string Detail   { get; set; } = string.Empty;
        public int    Penalty  { get; set; }
        public bool   Critical { get; set; }
        public string FixTweakId { get; set; } = string.Empty;
    }

    public sealed class HealthReport
    {
        public int Score { get; set; } = 100;
        public List<HealthIssue> Issues { get; set; } = new();

        public string Grade => Score >= 90 ? "S" : Score >= 80 ? "A"
                             : Score >= 70 ? "B" : Score >= 55 ? "C" : "D";

        public string Verdict => Score >= 90 ? "Система настроена отлично"
                              : Score >= 80 ? "Хорошо, но есть что добрать"
                              : Score >= 70 ? "Средне — заметный потенциал"
                              : Score >= 55 ? "Много неиспользованного потенциала"
                              : "Система работает далеко не на максимум";

        public static HealthReport Evaluate(RigReport rig, int startupCount = 0, bool overlaysRunning = false)
        {
            var r = new HealthReport();

            void Ding(string title, string detail, int penalty, bool critical = false)
            {
                r.Issues.Add(new HealthIssue { Title = title, Detail = detail, Penalty = penalty, Critical = critical });
                r.Score -= penalty;
            }

            if (rig.SingleChannel)
                Ding("Память в одном канале",
                     "Одна планка вместо двух — пропускная способность памяти вдвое ниже. " +
                     "Главный ограничитель FPS в Dota 2 и любых CPU-сценах.",
                     18, true);

            if (rig.VbsRunning)
                Ding("VBS включён",
                     "Защита виртуализацией может влиять на производительность в отдельных CPU-зависимых играх, но её отключение снижает безопасность Windows.",
                     rig.HvciRunning ? 6 : 4, false);

            if (rig.HvciRunning)
                Ding("Memory Integrity (HVCI) включён",
                     "Изоляция ядра повышает безопасность. Отключай только осознанно и после собственного бенчмарка.",
                     3);

            if (rig.NicLinkedAt100Mbit)
                Ding("Сетевой линк 100 Мбит/с",
                     "Ожидается 1000. Обычно это кабель или порт роутера — софт это не лечит.",
                     8);

            bool performancePlan = rig.PowerPlanGuid == "e9a42b02-d5df-448d-aa00-03f14749eb61" ||
                                   rig.PowerPlanGuid == "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            if (!performancePlan && !rig.IsLaptop)
                Ding("Энергосберегающая схема питания",
                     "Для игрового ПК от сети профиль высокой производительности может уменьшить скачки частоты. Сравни frametime до/после.",
                     3);

            if (!rig.HagsEnabled && (rig.GpuVendor == "NVIDIA" || rig.GpuVendor == "AMD"))
                Ding("HAGS выключен",
                     "На современной дискретной видеокарте HAGS стоит протестировать в обеих позициях: универсально лучшего варианта нет.",
                     2);

            if (startupCount > 8)
                Ding($"Автозагрузка: {startupCount} записей",
                     "Каждая лишняя программа в автозапуске ест память и мешает планировщику.",
                     Math.Min(10, (startupCount - 8) * 2));

            if (overlaysRunning)
                Ding("Оверлеи работают",
                     "Discord и NVIDIA Overlay заметно портят 1% low в CS2 и Dota 2.",
                     5);

            // parenthesised explicitly: the original mixed && / || precedence read
            // as "gpu is NVIDIA and date empty, OR date == '?'" and fired wrongly
            bool gpuKnown = !string.IsNullOrWhiteSpace(rig.Gpu) && rig.Gpu != "не определена";
            bool driverReadable = !string.IsNullOrWhiteSpace(rig.GpuDriver) && rig.GpuDriver != "?"
                                  && !string.IsNullOrWhiteSpace(rig.GpuDriverDate)
                                  && rig.GpuDriverDate != "?";
            if (gpuKnown && !driverReadable)
                Ding("Драйвер GPU не читается",
                     "Проверь, что установлен актуальный Game Ready драйвер.",
                     3);

            foreach (var issue in r.Issues)
            {
                issue.FixTweakId = issue.Title switch
                {
                    "Память в одном канале" => "",
                    "VBS включён" => "sys-vbs-off",
                    "Memory Integrity (HVCI) включён" => "sys-vbs-off",
                    "Сетевой линк 100 Мбит/с" => "net-nic-powersave",
                     "Энергосберегающая схема питания" => "power-ultimate",

                     "HAGS выключен" => "",

                    _ => ""
                };
            }

            r.Score = Math.Max(0, Math.Min(100, r.Score));
            r.Issues = r.Issues.OrderByDescending(i => i.Penalty).ToList();
            return r;
        }

        public List<HealthIssue> Critical => Issues.Where(i => i.Critical).ToList();
    }
}
