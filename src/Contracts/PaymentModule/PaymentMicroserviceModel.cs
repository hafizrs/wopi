namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class PaymentMicroserviceModel
    {
        public string ProviderName { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string OrderId { get; set; }
        public string Description { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string NotificationUrl { get; set; }
    }
}
