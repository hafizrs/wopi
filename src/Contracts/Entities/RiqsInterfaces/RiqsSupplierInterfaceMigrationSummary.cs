using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces
{
    public class RiqsSupplierInterfaceMigrationSummary : EntityBase
    {
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsDraft { get; set; } = false;
        public List<TempSupplierInterfacePastData> AdditionalInfos { get; set; }
        public long TotalRecord { get; set; } = 0;
        public bool IsUpdate { get; set; } = false;
    }

    public class TempSupplierInterfacePastData : SupplierInfo
    {
        public string MigrationSummeryId { get; set; }
        public string ClientId { get; set; }
        public bool IsExist { get; set; } = false;
    }

    public class SupplierInfo : BlocksRootEntityBase
    {
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public PraxisAddress Address { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string VatNumber { get; set; }
        public string CustomerNumber { get; set; }
        public bool? IsBillingAddressDifferent { get; set; }
        public PraxisAddress BillingAddress { get; set; }
        public IEnumerable<ContactPerson> ContactPersons { get; set; }
        public IEnumerable<PraxisDocument> FileAttachments { get; set; }
        public IEnumerable<PraxisSupplierContactPerson> SupplierContactPersons { get; set; }
    }
}
