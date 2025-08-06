using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PersonInformation
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string CountryCode { get; set; }
        public string[] Tags { get; set; }
        public string MotherTongue { get; set; }
        public string Salutation { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ItemId { get; set; }
        public string MailPurpose { get; set; }
        public string[] CopyEmailTo { get; set; }
        public string DefaultPassword { get; set; }
        public string HostDomain { get; set; }
        public int? RegisteredBy { get; set; }
        public Dictionary<string, object> PersonInfo { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool SignUp { get; set; }
        public bool PersonaEnabled { get; set; }
        public string Title { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string CustomerId { get; set; }
        public string MarketName { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public string Designation { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string PostNominalTitle { get; set; }
        public string ManagerNr { get; set; }
        public string ClientStatus { get; set; }
        public string ProfileImageId { get; set; }
        public string[] OtherLanguage { get; set; }
        public string[] OrganizationNames { get; set; }
        public string UserId { get; set; }
        public string Language { get; set; }
    }
}
