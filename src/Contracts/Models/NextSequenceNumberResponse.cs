using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class NextSequenceNumberResponse
    {
        public string Context { get; set; }
        public int CurrentNumber { get; set; }
        public List<string> Errors { get; set; }
    }
}
