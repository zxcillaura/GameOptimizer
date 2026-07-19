using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOptimizer.Core.Models
{
    public enum CheckStatus
    {
        Ok,
        Warning,
        Error,
        NotApplicable
    }

    public class CheckResult
    {
        public CheckStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Instruction { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }
}