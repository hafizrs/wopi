using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
   public class PaymentProcessingResult
    {
        public string RedirectUrl { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string PaymentDetailId { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
