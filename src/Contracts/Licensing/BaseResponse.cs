using FluentValidation.Results;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing
{
    public abstract class BaseResponse
    {
        public bool ExecutionStatus { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public ValidationResult ValidationResult { get; set; }
    }
}
