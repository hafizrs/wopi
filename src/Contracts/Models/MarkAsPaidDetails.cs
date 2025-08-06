using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class MarkAsPaidDetails
    {
        public DateTime PaymentDate { get; set; }
        public string ImageFileId { get; set; }
        public string Remarks { get; set; }
    }
}
