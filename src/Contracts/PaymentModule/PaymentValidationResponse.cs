namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class PaymentValidationResponse
    {
        public string PaymentDetailId { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
