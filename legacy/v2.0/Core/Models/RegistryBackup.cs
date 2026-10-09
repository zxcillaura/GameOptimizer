using System;

namespace GameOptimizer.Core.Models
{
    /// <summary>
    /// Бэкап одного значения реестра перед изменением
    /// </summary>
    public class RegistryBackup
    {
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public object OldValue { get; set; }
        public DateTime BackupDate { get; set; }
    }
}