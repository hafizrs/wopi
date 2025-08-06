using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class OrganizationBasicInfoResponse
    {
        public string ItemId { get; set; }
        public string ClientName { get; set; }
        public PraxisAddress Address { get; set; }
        public List<PraxisKeyValue> ReportingConfigurations { get; set; }
        public bool HaveAdditionalPurchasePermission { get; set; }
        public List<ExternelReportingOffice> ExternelReportingOffices { get; set; }
        public bool HaveAdditionalAllocationPermission { get; set; }
    }
}
