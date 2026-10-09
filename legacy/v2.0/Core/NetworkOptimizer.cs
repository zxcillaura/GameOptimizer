using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace GameOptimizer.Core
{
    public static class NetworkOptimizer
    {
        /// <summary>
        /// Устанавливает кастомный DNS для всех активных сетевых адаптеров
        /// </summary>
        public static void SetCustomDNS(string primaryDns, string secondaryDns, Action<string> log)
        {
            log($"-> Подготовка к установке DNS: {primaryDns} / {secondaryDns}");
            int updatedAdapters = 0;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                // Ищем только активные подключения (Проводной интернет или Wi-Fi)
                if (adapter.OperationalStatus == OperationalStatus.Up && 
                   (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    try
                    {
                        log($"Настройка адаптера: {adapter.Name}...");
                        
                        // Установка Primary DNS
                        RunCmd($"/c netsh interface ipv4 set dns name=\"{adapter.Name}\" static {primaryDns} primary");
                        
                        // Установка Secondary DNS
                        if (!string.IsNullOrEmpty(secondaryDns))
                        {
                            RunCmd($"/c netsh interface ipv4 add dns name=\"{adapter.Name}\" {secondaryDns} index=2");
                        }
                        updatedAdapters++;
                    }
                    catch (Exception ex)
                    {
                        log($"[Ошибка] Не удалось настроить {adapter.Name}: {ex.Message}");
                    }
                }
            }

            if (updatedAdapters > 0)
            {
                RunCmd("/c ipconfig /flushdns");
                log($"[УСПЕХ] DNS успешно применен к {updatedAdapters} адаптерам. Кэш DNS сброшен.");
            }
            else
            {
                log("[ОШИБКА] Не найдено активных сетевых адаптеров для применения DNS.");
            }
        }

        /// <summary>
        /// Возвращает автоматическое получение DNS (DHCP)
        /// </summary>
        public static void ResetDNSToAuto(Action<string> log)
        {
            log("-> Сброс настроек DNS в автоматический режим (DHCP)...");
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up && 
                   (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet || adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    RunCmd($"/c netsh interface ipv4 set dns name=\"{adapter.Name}\" dhcp");
                }
            }
            RunCmd("/c ipconfig /flushdns");
            log("[УСПЕХ] Настройки DNS сброшены на по умолчанию.");
        }

        /// <summary>
        /// Жесткий сброс всего сетевого стека (помогает при высоких loss'ах и лагах)
        /// </summary>
        public static void ResetNetworkStack(Action<string> log)
        {
            log("-> Запуск глубокого сброса сетевого стека Windows...");
            
            RunCmd("/c ipconfig /release");
            RunCmd("/c ipconfig /flushdns");
            RunCmd("/c ipconfig /renew");
            RunCmd("/c netsh winsock reset");
            RunCmd("/c netsh int ip reset");

            log("[УСПЕХ] Сетевой кэш, Winsock и IP-стек полностью сброшены!");
            log("ВНИМАНИЕ: Для окончательного применения настроек Winsock требуется перезагрузка ПК.");
        }

        /// <summary>
        /// Проверка пинга до указанного IP
        /// </summary>
        public static long PingServer(string ipAddress)
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(ipAddress, 1500); // Тайм-аут 1.5 сек
                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch { }
            return -1; // Ошибка или таймаут
        }

        private static void RunCmd(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                Verb = "runas"
            };
            Process.Start(psi)?.WaitForExit();
        }
    }
}