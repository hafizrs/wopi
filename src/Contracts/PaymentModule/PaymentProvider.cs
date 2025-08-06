using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class PaymentProvider: EntityBase
    {
        public string ProviderName { get; set; }
        public string ProviderKey { get; set; }
        public string CustomerId { get; set; }
        public string TerminalId { get; set; }
        public string JsonApiUserName { get; set; }
        public string JsonApiPassword { get; set; }
        public string SpecVersion { get; set; }
        public int RetryIndicator { get; set; }
        public bool DisplayBillingAddressForm { get; set; }
        public string[] BillingAddressFormMandatoryFields { get; set; }
        public bool DisplayDeliveryAddressForm { get; set; }
        public string[] DeliveryAddressFormMandatoryFields { get; set; }
        public string ApiBaseUrl { get; set; }
        public string SuccessUrl { get; set; }
        public string FailUrl { get; set; }
        public string AbortUrl { get; set; }
        public string NotificationUrl { get; set; }
        public string[] PaymentMethods { get; set; }
    }
}
