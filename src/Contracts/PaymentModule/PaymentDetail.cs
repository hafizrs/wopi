using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule
{
    public class PaymentDetail : EntityBase
    {
        public string ProviderName { get; set; }
        public string ProviderKey { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime ExpirationDate { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string Token { get; set; }
        public string RequestId { get; set; }
        public string TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string AcquirerName { get; set; }
        public string AcquirerReference { get; set; }
        public string SIXTransactionReference { get; set; }
        public string ApprovalCode { get; set; }
        public string PaymentMethod { get; set; }
        public CardInformationModel CardDetails { get; set; }
        public string PayerIpAddress { get; set; }
        public string PayerIpLocation { get; set; }
        public AddressModel BillingAddress { get; set; }
        public AddressModel DeliveryAddress { get; set; }
    }

    public class CardInformationModel
    {
        public string MaskedNumber { get; set; }
        public string ExpYear { get; set; }
        public string ExpMonth { get; set; }
        public string HolderName { get; set; }
        public string HolderSegment { get; set; }
        public string CountryCode { get; set; }
        public string HashValue { get; set; }
    }

    public class AddressModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public string Gender { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string CountryCode { get; set; }
        public string Email { get; set; }
        public string DateOfBirth { get; set; }
        public string LegalForm { get; set; }
        public string Street2 { get; set; }
        public string CountrySubdivisionCode { get; set; }
        public string Phone { get; set; }
    }
}
