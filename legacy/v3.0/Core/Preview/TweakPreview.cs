using System;
using System.Collections.Generic;

namespace GameOptimizer.Core.Preview
{
    /// <summary>
    /// Тип операции в реестре
    /// </summary>
    public enum OperationType
    {
        RegistryEdit,
        ServiceStop,
        ServiceStart,
        ServiceDisable,
        FileDelete,
        DNSChange,
        ProcessKill
    }

    /// <summary>
    /// Предпросмотр одного изменения
    /// </summary>
    public class PreviewItem
    {
        public OperationType OperationType { get; set; }
        public string TargetPath { get; set; }
        public string TargetValue { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Description { get; set; }
        public bool IsRisky { get; set; } // Флаг для опасных операций
    }

    /// <summary>
    /// Менеджер предпросмотра изменений
    /// </summary>
    public class TweakPreviewManager
    {
        private List<PreviewItem> _previewItems;

        public IReadOnlyList<PreviewItem> PreviewItems => _previewItems.AsReadOnly();
        public bool HasRiskyOperations => _previewItems.Exists(item => item.IsRisky);
        public int TotalChanges => _previewItems.Count;

        public TweakPreviewManager()
        {
            _previewItems = new List<PreviewItem>();
        }

        public void AddRegistryChange(string path, string valueName, string oldValue, string newValue, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.RegistryEdit,
                TargetPath = path,
                TargetValue = valueName,
                OldValue = oldValue,
                NewValue = newValue,
                Description = $"Изменение реестра: {path}\\{valueName}",
                IsRisky = isRisky
            });
        }

        public void AddServiceStop(string serviceName, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.ServiceStop,
                TargetPath = serviceName,
                Description = $"Остановка сервиса: {serviceName}",
                IsRisky = isRisky
            });
        }

        public void AddServiceDisable(string serviceName, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.ServiceDisable,
                TargetPath = serviceName,
                Description = $"Отключение сервиса: {serviceName}",
                IsRisky = isRisky
            });
        }

        public void AddFileDelete(string filePath, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.FileDelete,
                TargetPath = filePath,
                Description = $"Удаление файла: {filePath}",
                IsRisky = isRisky
            });
        }

        public void AddDNSChange(string dnsServer, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.DNSChange,
                TargetPath = dnsServer,
                Description = $"Смена DNS на: {dnsServer}",
                IsRisky = isRisky
            });
        }

        public void AddProcessKill(string processName, bool isRisky = false)
        {
            _previewItems.Add(new PreviewItem
            {
                OperationType = OperationType.ProcessKill,
                TargetPath = processName,
                Description = $"Завершение процесса: {processName}",
                IsRisky = isRisky
            });
        }

        public void Clear()
        {
            _previewItems.Clear();
        }

        public List<PreviewItem> GetRiskyOperations()
        {
            return _previewItems.FindAll(item => item.IsRisky);
        }

        public List<PreviewItem> GetSafeOperations()
        {
            return _previewItems.FindAll(item => !item.IsRisky);
        }

        public string GetSummary()
        {
            int registry = _previewItems.FindAll(item => item.OperationType == OperationType.RegistryEdit).Count;
            int services = _previewItems.FindAll(item => 
                item.OperationType == OperationType.ServiceStop || 
                item.OperationType == OperationType.ServiceDisable).Count;
            int files = _previewItems.FindAll(item => item.OperationType == OperationType.FileDelete).Count;

            return $"Всего изменений: {TotalChanges}\n" +
                   $"Реестр: {registry}\n" +
                   $"Сервисы: {services}\n" +
                   $"Файлы: {files}";
        }
    }
}
