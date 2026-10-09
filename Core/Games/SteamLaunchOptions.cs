using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameOptimizer.Core.Backup;
using GameOptimizer.Utils;

namespace GameOptimizer.Core.Games
{
    /// <summary>
    /// Reads and writes Steam launch options in
    /// userdata\&lt;account&gt;\config\localconfig.vdf.
    ///
    /// The file is Valve's KeyValues text format, so it is edited textually with
    /// brace matching — never re-serialised, because that would rewrite hundreds
    /// of unrelated cloud-sync tokens.
    /// </summary>
    public static class SteamLaunchOptions
    {
        public static string SteamPath
        {
            get
            {
                try
                {
                    var (_, outp) = ProcessRunner.PowerShell(
                        "(Get-ItemProperty 'HKCU:\\Software\\Valve\\Steam' -Name SteamPath).SteamPath");
                    string p = (outp ?? "").Replace("\r", "").Trim();
                    return Directory.Exists(p) ? p : "";
                }
                catch { return ""; }
            }
        }

        public static string UserDataDir(string steamPath) =>
            string.IsNullOrEmpty(steamPath) ? "" : Path.Combine(steamPath, "userdata");

        /// <summary>All localconfig.vdf files, newest first (the active account is first).</summary>
        public static List<string> ConfigFiles()
        {
            var result = new List<string>();
            try
            {
                string steam = SteamPath;
                string ud = UserDataDir(steam);
                if (!Directory.Exists(ud)) return result;

                foreach (var dir in Directory.GetDirectories(ud))
                {
                    string f = Path.Combine(dir, "config", "localconfig.vdf");
                    if (File.Exists(f)) result.Add(f);
                }
                return result.OrderByDescending(File.GetLastWriteTime).ToList();
            }
            catch { return result; }
        }

        /// <summary>The config of the account that is actually logged in.</summary>
        public static string ActiveConfig() => ConfigFiles().FirstOrDefault() ?? "";

        public static bool IsSteamRunning()
        {
            try { return Process.GetProcessesByName("steam").Length > 0; }
            catch { return false; }
        }

        /// <summary>Asks Steam to close so it cannot overwrite our edit on exit.</summary>
        public static bool ShutdownSteam(Action<string> log)
        {
            try
            {
                string steam = SteamPath;
                string exe = Path.Combine(steam.Replace('/', '\\'), "steam.exe");
                if (!File.Exists(exe))
                {
                    log?.Invoke("      steam.exe не найден — закрой Steam вручную и повтори");
                    return false;
                }

                Process.Start(new ProcessStartInfo(exe, "-shutdown") { UseShellExecute = false });
                for (int i = 0; i < 30; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (!IsSteamRunning()) { log?.Invoke("      Steam закрыт"); return true; }
                }
                log?.Invoke("      Steam не закрылся за 30 секунд");
                return false;
            }
            catch (Exception ex)
            {
                log?.Invoke($"      не удалось закрыть Steam: {ex.Message}");
                return false;
            }
        }

        // ── reading ──────────────────────────────────────────────────────
        public static string Read(string configFile, string appId)
        {
            try
            {
                if (!File.Exists(configFile)) return null;
                string text = File.ReadAllText(configFile, Encoding.UTF8);
                if (!FindAppsBlock(text, out int appsOpen, out int appsClose)) return "";
                if (!FindAppBlock(text, appsOpen, appsClose, appId, out int open, out int close)) return "";

                var inner = text.Substring(open, close - open);
                var m = Regex.Match(inner, @"""LaunchOptions""\s*""((?:[^""\\]|\\.)*)""");
                if (!m.Success) return "";
                return Regex.Unescape(m.Groups[1].Value);
            }
            catch { return null; }
        }

        /// <summary>Writes launch options. Returns false and logs a reason on failure.</summary>
        public static bool Write(string configFile, string appId, string value, Action<string> log)
        {
            try
            {
                if (!File.Exists(configFile))
                {
                    log?.Invoke($"      файл конфига не найден: {configFile}");
                    return false;
                }

                string text = File.ReadAllText(configFile, Encoding.UTF8);
                string nl = text.Contains("\r\n") ? "\r\n" : "\n";
                string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");

                BackupManager.TrackFile(configFile);

                if (!FindAppsBlock(text, out int appsOpen, out int appsClose))
                {
                    log?.Invoke("      в localconfig.vdf не найден раздел \"Apps\" — правка отменена");
                    return false;
                }

                string result;
                if (FindAppBlock(text, appsOpen, appsClose, appId, out int open, out int close))
                {
                    string block = text.Substring(open, close - open + 1);
                    var m = Regex.Match(block, @"""LaunchOptions""\s*""(?:[^""\\]|\\.)*""");

                    if (m.Success)
                    {
                        string newBlock = block.Remove(m.Index, m.Length)
                                               .Insert(m.Index, $"\"LaunchOptions\"\t\t\"{escaped}\"");
                        result = text.Substring(0, open) + newBlock + text.Substring(close + 1);
                        log?.Invoke($"      {appId}: заменены параметры запуска");
                    }
                    else
                    {
                        string indent = IndentOf(text, open) + "\t";
                        int insertAt = close;
                        string nlIndent = nl + indent;
                        result = text.Substring(0, insertAt) +
                                 $"\"LaunchOptions\"\t\t\"{escaped}\"" + nlIndent +
                                 text.Substring(insertAt);
                        log?.Invoke($"      {appId}: добавлены параметры запуска");
                    }
                }
                else
                {
                    // CS2 may have never been launched on this account, so the app
                    // block does not exist yet — create it inside "Apps".
                    string appsIndent = IndentOf(text, appsOpen);
                    string appIndent = appsIndent + "\t";
                    string propIndent = appIndent + "\t";

                    var sb = new StringBuilder();
                    sb.Append(appIndent).Append('"').Append(appId).Append('"').Append(nl);
                    sb.Append(appIndent).Append('{').Append(nl);
                    sb.Append(propIndent).Append("\"LaunchOptions\"\t\t\"").Append(escaped).Append('"').Append(nl);
                    sb.Append(appIndent).Append('}').Append(nl);
                    sb.Append(appsIndent);

                    result = text.Substring(0, appsClose) + sb + text.Substring(appsClose);
                    log?.Invoke($"      {appId}: создан раздел игры с параметрами запуска");
                }

                File.WriteAllText(configFile, result, new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"      ошибка записи конфига: {ex.Message}");
                return false;
            }
        }

        // ── VDF helpers ──────────────────────────────────────────────────
        private static string IndentOf(string text, int index)
        {
            int lineStart = text.LastIndexOf('\n', Math.Max(0, index - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var sb = new StringBuilder();
            for (int i = lineStart; i < text.Length && (text[i] == '\t' || text[i] == ' '); i++)
                sb.Append(text[i]);
            return sb.ToString();
        }

        private static int FindBlockEnd(string text, int openBrace)
        {
            int depth = 0;
            bool inQuotes = false;
            for (int i = openBrace; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\')) inQuotes = !inQuotes;
                if (inQuotes) continue;
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        private static bool FindAppsBlock(string text, out int open, out int close)
        {
            open = close = -1;
            foreach (Match m in Regex.Matches(text, "\"[Aa]pps\"\\s*\\{"))
            {
                int ob = text.IndexOf('{', m.Index);
                int cb = FindBlockEnd(text, ob);
                if (cb < 0) continue;

                string inner = text.Substring(ob, cb - ob);
                // the real Apps section holds one block per app id
                if (Regex.IsMatch(inner, "\"\\d+\"\\s*\\{"))
                {
                    open = ob;
                    close = cb;
                    return true;
                }
            }
            return false;
        }

        private static bool FindAppBlock(string text, int appsOpen, int appsClose,
                                         string appId, out int open, out int close)
        {
            open = close = -1;
            string key = "\"" + appId + "\"";
            int pos = appsOpen;
            while (pos >= 0 && pos < appsClose)
            {
                pos = text.IndexOf(key, pos, StringComparison.Ordinal);
                if (pos < 0 || pos >= appsClose) break;

                int after = pos + key.Length;
                int j = after;
                while (j < appsClose && char.IsWhiteSpace(text[j])) j++;

                if (j < appsClose && text[j] == '{')
                {
                    int cb = FindBlockEnd(text, j);
                    if (cb > 0 && cb <= appsClose) { open = j; close = cb; return true; }
                }
                pos = after;
            }
            return false;
        }
    }
}
