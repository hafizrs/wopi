using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Subscriptions.PaymentModule
{
    public class MarkAsPaidOfflineInvoiceComman
    {
        public string OrganizationId { get; set; }
        public string PaymentHistoryId { get; set; }
        public bool MarkAsPaid { get; set; } = false;
        public DateTime PaymentDate { get; set; }
        public string ImageFileId { get; set; }
        public string Remarks { get; set; }
    }
}
