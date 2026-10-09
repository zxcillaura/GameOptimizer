using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GameOptimizer.Core.Bench
{
    public sealed class BenchRun
    {
        public string Id          { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Date        { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        public string Game        { get; set; } = "CS2";
        public string Label       { get; set; } = "";
        public string Source      { get; set; } = "";
        public double AvgFps      { get; set; }
        public double OnePercentLow    { get; set; }
        public double PointOnePercentLow { get; set; }
        public double AvgFrameMs  { get; set; }
        public double MedianFrameMs { get; set; }
        public int    Frames      { get; set; }
        public double DurationSec { get; set; }
        public int    Stutters    { get; set; }

        public string Short => $"{Date}  {Game}  {Label}";
        public string FpsLine => $"{AvgFps:F0} FPS avg / {OnePercentLow:F0} 1% low";
    }

    /// <summary>
    /// Imports CapFrameX / PresentMon CSV captures and turns them into
    /// avg FPS, 1% low and 0.1% low — the metrics that actually describe
    /// how a game feels.
    /// </summary>
    public static class BenchmarkStore
    {
        public static string Dir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GAMEOPTIMIZv7", "bench");

        public static List<BenchRun> History { get; private set; } = new();
        public static List<double> LastFrametimes { get; private set; } = new();

        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        static BenchmarkStore() => Load();

        public static void Load()
        {
            try
            {
                string f = Path.Combine(Dir, "history.json");
                if (!File.Exists(f)) return;
                History = JsonSerializer.Deserialize<List<BenchRun>>(File.ReadAllText(f), Json) ?? new();
                History = History.OrderByDescending(h => h.Date).ToList();
            }
            catch { History = new(); }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(Path.Combine(Dir, "history.json"), JsonSerializer.Serialize(History, Json));
            }
            catch { }
        }

        public static void Add(BenchRun run)
        {
            History.Insert(0, run);
            if (History.Count > 200) History = History.Take(200).ToList();
            Save();
        }

        public static void Remove(string id)
        {
            History.RemoveAll(h => h.Id == id);
            Save();
        }

        /// <summary>Parses a frame-capture CSV. Returns null and sets error on failure.</summary>
        public static BenchRun ImportCsv(string path, string game, string label, out string error)
        {
            error = "";
            try
            {
                var lines = File.ReadAllLines(path);
                if (lines.Length < 5) { error = "Файл слишком короткий"; return null; }

                char delim = DetectDelimiter(lines[0]);
                var header = Split(lines[0], delim);
                int valueCol = FindFrametimeColumn(header, out bool isFps);
                int startRow = 1;

                if (valueCol < 0)
                {
                    // no header — single column of numbers
                    var probe = Split(lines[0], delim);
                    if (probe.Length >= 1 && double.TryParse(probe[0], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out _))
                    {
                        // No header: treat the column as frame times in milliseconds.
                        // Guessing "FPS" from a short number would turn a 7.5 ms frame
                        // into 7.5 FPS and wreck every derivative metric.
                        valueCol = 0;
                        startRow = 0;
                        isFps = false;
                    }
                    else
                    {
                        error = "Не нашёл колонку с frametime (ищу MsBetweenPresents / FrameTime / FPS)";
                        return null;
                    }
                }

                var frameTimes = new List<double>();
                for (int i = startRow; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var cells = Split(lines[i], delim);
                    if (valueCol >= cells.Length) continue;
                    if (!double.TryParse(cells[valueCol], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out double v)) continue;
                    if (v <= 0) continue;
                    frameTimes.Add(isFps ? 1000.0 / v : v);
                }

                if (frameTimes.Count < 100)
                {
                    error = $"Слишком мало кадров распознано ({frameTimes.Count})";
                    return null;
                }

                double totalMs = frameTimes.Sum();
                double avgMs = totalMs / frameTimes.Count;

                var sorted = frameTimes.OrderByDescending(x => x).ToList();
                double oneLow = Mean(sorted, Math.Max(1, (int)(sorted.Count * 0.01)));
                double pointOneLow = Mean(sorted, Math.Max(1, (int)(sorted.Count * 0.001)));
                var med = frameTimes.OrderBy(x => x).ToList();

                var run = new BenchRun
                {
                    Game = game,
                    Label = string.IsNullOrWhiteSpace(label) ? Path.GetFileNameWithoutExtension(path) : label,
                    Source = path,
                    AvgFrameMs = avgMs,
                    MedianFrameMs = med[med.Count / 2],
                    AvgFps = 1000.0 / avgMs,
                    OnePercentLow = 1000.0 / Math.Max(0.001, oneLow),
                    PointOnePercentLow = 1000.0 / Math.Max(0.001, pointOneLow),
                    Frames = frameTimes.Count,
                    DurationSec = totalMs / 1000.0,
                    Stutters = frameTimes.Count(x => x > med[med.Count / 2] * 2.0)
                };

                LastFrametimes = Downsample(frameTimes, 240);
                return run;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static double Mean(List<double> sortedDesc, int count)
        {
            count = Math.Max(1, Math.Min(count, sortedDesc.Count));
            double sum = 0;
            for (int i = 0; i < count; i++) sum += sortedDesc[i];
            return sum / count;
        }

        private static List<double> Downsample(List<double> src, int target)
        {
            if (src.Count <= target) return new List<double>(src);
            var outp = new List<double>(target);
            double step = (double)src.Count / target;
            for (int i = 0; i < target; i++)
                outp.Add(src[(int)Math.Min(src.Count - 1, i * step)]);
            return outp;
        }

        private static char DetectDelimiter(string line)
        {
            int c = line.Count(ch => ch == ','), s = line.Count(ch => ch == ';'), t = line.Count(ch => ch == '\t');
            if (t >= c && t >= s && t > 0) return '\t';
            if (s > c) return ';';
            return ',';
        }

        private static string[] Split(string line, char delim)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var cur = new System.Text.StringBuilder();
            foreach (char ch in line)
            {
                if (ch == '"') { inQuotes = !inQuotes; continue; }
                if (ch == delim && !inQuotes) { parts.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(ch);
            }
            parts.Add(cur.ToString());
            for (int i = 0; i < parts.Count; i++) parts[i] = parts[i].Trim().Trim('"');
            return parts.ToArray();
        }

        private static int FindFrametimeColumn(string[] header, out bool isFps)
        {
            isFps = false;
            string[] ft =
            {
                "msbetweenpresents", "frametime", "frametimems", "frametimes",
                "msbetweendisplaychange", "msuntilrendercomplete", "msuntilpresentcomplete",
                "ft", "frame_time"
            };
            string[] fps = { "fps", "framerate", "frame rate" };

            for (int i = 0; i < header.Length; i++)
            {
                var h = Normalise(header[i]);
                if (ft.Any(k => h.Contains(k))) return i;
            }
            for (int i = 0; i < header.Length; i++)
            {
                var h = Normalise(header[i]);
                if (fps.Any(k => h == k || h.Contains(k)))
                {
                    isFps = true;
                    return i;
                }
            }
            return -1;
        }

        private static string Normalise(string s)
            => (s ?? "").ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "").Trim();

        /// <summary>Finds likely CapFrameX / PresentMon captures in the usual folders.</summary>
        public static List<string> FindLikelyCaptures()
        {
            var found = new List<string>();
            string[] roots =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CapFrameX"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PresentMon"),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };
            foreach (var root in roots)
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    found.AddRange(Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories).Take(40));
                }
                catch { }
            }
            return found.Distinct().OrderByDescending(f => File.GetLastWriteTime(f)).Take(40).ToList();
        }
    }
}
