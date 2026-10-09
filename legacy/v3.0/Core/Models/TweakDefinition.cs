using System.Collections.Generic;
using Microsoft.Win32;

namespace GameOptimizer.Core.Models
{
    public class RegistryOperation
    {
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public object Value { get; set; }
        public RegistryValueKind ValueKind { get; set; } = RegistryValueKind.DWord;
    }

    public class TweakDefinition
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        // Для проверки статуса
        public string CheckRegistryPath { get; set; }
        public string CheckValueName { get; set; }
        public object ExpectedValue { get; set; }

        // Операции
        public List<RegistryOperation> ApplyOperations { get; set; } = new();
        public List<RegistryOperation> RevertOperations { get; set; } = new();
    }
}