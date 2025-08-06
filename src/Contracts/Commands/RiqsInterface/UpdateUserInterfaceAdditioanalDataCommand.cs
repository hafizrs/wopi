using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class UpdateUserInterfaceAdditioanalDataCommand
    {
        [Required]
        public List<string> UserIds { get; set; }
        [Required]
        public string MigrationSummaryId { get; set; }
        public List<ClientList> ClientList { get; set; }
        public string MotherTongue { get; set; }
        public List<string> OtherLanguage { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public List<string> Roles { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; } = new AdditionalInfo();
    }

    public class ClientList
    {
        public string ParentOrganizationId { get; set; }
        public string ParentOrganizationName { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public bool IsPrimaryDepartment { get; set; }
        public List<string> Roles { get; set; }
        public string Designation { get; set; }
        public DateTime DateOfJoining { get; set; }
        public string Phone { get; set; }
        public string PhoneExtensionNumber { get; set; }
        public bool IsLatest { get; set; }
        public bool IsCreateProcessGuideEnabled { get; set; }
    }

    public class AdditionalInfo
    {
        public string ItemId { get; set; }
        public string Title { get; set; }
        public string ClientId { get; set; }
    }
}
