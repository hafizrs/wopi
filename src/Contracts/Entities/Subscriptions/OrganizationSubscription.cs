using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions
{
    public class OrganizationSubscription : EntityBase
    {
        public string OrganizationId { get; set; }
        public double TotalTokenUsed { get; set; }
        public double TotalTokenSize { get; set; }
        public double TotalStorageUsed { get; set; }
        public double TotalStorageSize { get; set; }
        public double TokenOfOrganization { get; set; }
        public double StorageOfOrganization { get; set; }
        public double TokenOfUnits { get; set; }
        public double StorageOfUnits { get; set; }
        public double TotalUsageTokensSum { get; set; }
        public double TotalPurchasedTokensSum { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public DateTime SubscriptionExpirationDate { get; set; }
        public bool IsTokenApplied { get; set; }
        public double TotalManualTokenUsed { get; set; }
        public double TotalManualTokenSize { get; set; }
        public double TotalUsageManualTokensSum { get; set; }
        public double TotalPurchasedManualTokensSum { get; set; }
        public double ManualTokenOfOrganization { get; set; }
        public double ManualTokenOfUnits { get; set; }
        public bool IsManualTokenApplied { get; set; }
    }
}
