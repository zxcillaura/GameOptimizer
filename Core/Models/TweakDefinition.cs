using Microsoft.Win32;

namespace GameOptimizer.Core.Models
{
    public class RegistryOperation
    {
        public string KeyPath         { get; set; } = string.Empty;
        public string ValueName       { get; set; } = string.Empty;
        public object? Value          { get; set; }
        public RegistryValueKind ValueKind { get; set; } = RegistryValueKind.DWord;
    }

    public class TweakDefinition
    {
        public string Id              { get; set; } = string.Empty;
        public string DisplayName     { get; set; } = string.Empty;
        public string Description     { get; set; } = string.Empty;
        public string Category        { get; set; } = string.Empty;

        public string CheckRegistryPath  { get; set; } = string.Empty;
        public string CheckValueName     { get; set; } = string.Empty;
        public object? ExpectedValue     { get; set; }

        public List<RegistryOperation> ApplyOperations  { get; set; } = new();
        public List<RegistryOperation> RevertOperations { get; set; } = new();
    }
}
