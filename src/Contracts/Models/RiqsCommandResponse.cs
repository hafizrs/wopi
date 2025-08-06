using FluentValidation.Results;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsCommandResponse : CommandResponse
    {
        public RiqsCommandResponse() 
        {
            CustomErrors = new List<string>();
        }

        public RiqsCommandResponse(ValidationResult result)
        {
            Errors = result;
            CustomErrors = new List<string>();
        }

        public object Result { get; set; }
        public long TotalCount { get; set; }
        public List<string> CustomErrors { get; set; }
    }
}
