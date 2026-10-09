using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace GameOptimizer.Core.Diagnostics
{
    public sealed record NetworkTestResult(string Adapter, string Gateway, long GatewayMs, long DnsMs, int PacketLoss, long JitterMs, string Summary, System.Collections.Generic.IReadOnlyList<double> Samples)
    {
        public long InternetMs { get; init; } = -1;
        public string StageSummary { get; init; } = "";
    }

    public sealed record NetworkComparison(NetworkTestResult Before, NetworkTestResult After, string Verdict);

    public static class NetworkDiagnostics
    {
        public static NetworkTestResult Run()
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            if (adapter == null) return new("Нет активного адаптера", "-", -1, -1, 100, -1, "Активное подключение не найдено.", Array.Empty<double>());

            var gateway = adapter.GetIPProperties().GatewayAddresses.FirstOrDefault()?.Address.ToString() ?? "";
            var gatewayStats = Probe(gateway, 8);
            var dnsStats = Probe("1.1.1.1", 8);
            var internetStats = Probe("8.8.8.8", 8);
            var jitter = gatewayStats.jitter >= 0 ? gatewayStats.jitter : dnsStats.jitter;
            var samples = gatewayStats.samples.Count > 0 ? gatewayStats.samples : dnsStats.samples;
            var summary = gatewayStats.loss == 100 || dnsStats.loss == 100
                ? "Есть потеря пакетов или узел не отвечает. Проверь кабель, Wi‑Fi и роутер."
                : $"До роутера {gatewayStats.avg} мс, DNS {dnsStats.avg} мс, интернет {internetStats.avg} мс, jitter {jitter} мс, потеря {dnsStats.loss}%. Ping зависит от маршрута и сервера игры.";
            return new(adapter.Name, gateway == "" ? "Не определён" : gateway, gatewayStats.avg, dnsStats.avg, dnsStats.loss, jitter, summary, samples.Select(v => (double)v).ToList())
            {
                InternetMs = internetStats.avg,
                StageSummary = $"LAN {(gatewayStats.avg >= 0 ? gatewayStats.avg + " мс" : "нет ответа")} → DNS {(dnsStats.avg >= 0 ? dnsStats.avg + " мс" : "нет ответа")} → интернет {(internetStats.avg >= 0 ? internetStats.avg + " мс" : "нет ответа")}"
            };
        }

        public static NetworkComparison Compare(NetworkTestResult before, NetworkTestResult after)
        {
            if (before == null || after == null) return new(before, after, "Недостаточно данных для сравнения.");
            long b = before.InternetMs >= 0 ? before.InternetMs : before.DnsMs;
            long a = after.InternetMs >= 0 ? after.InternetMs : after.DnsMs;
            if (b < 0 || a < 0) return new(before, after, "Не удалось получить оба замера.");
            long delta = a - b;
            double pct = b == 0 ? 0 : delta * 100.0 / b;
            string verdict = delta < 0
                ? $"Стало лучше: {b} → {a} мс ({pct:0.#}%)."
                : delta > 0
                    ? $"Стало хуже: {b} → {a} мс (+{Math.Abs(pct):0.#}%)."
                    : $"Без заметной разницы: {a} мс.";
            return new(before, after, verdict);
        }

        private static (long avg, int loss, long jitter, System.Collections.Generic.List<long> samples) Probe(string host, int count)
        {
            var empty = new System.Collections.Generic.List<long>();
            if (string.IsNullOrWhiteSpace(host)) return (-1, 100, -1, empty);
            var values = new System.Collections.Generic.List<long>();
            using var ping = new Ping();
            for (var i = 0; i < count; i++)
            {
                try
                {
                    var reply = ping.Send(host, 1000);
                    if (reply?.Status == IPStatus.Success) values.Add(reply.RoundtripTime);
                }
                catch { }
            }
            if (values.Count == 0) return (-1, 100, -1, empty);
            var average = (long)values.Average();
            var jitter = values.Count < 2 ? 0 : (long)values.Zip(values.Skip(1), (a, b) => Math.Abs(b - a)).Average();
            return (average, (count - values.Count) * 100 / count, jitter, values);
        }
    }
}
