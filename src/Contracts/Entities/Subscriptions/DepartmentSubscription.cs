using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions
{
    public class DepartmentSubscription : EntityBase
    {
        public string PraxisClientId { get; set; }
        public double TotalTokenUsed { get; set; }
        public double TotalTokenSize { get; set; }
        public double TotalStorageUsed { get; set; }
        public double TotalStorageSize { get; set; }
        public double TokenFromOrganization { get; set; }
        public double StorageFromOrganization { get; set; }
        public double TokenOfUnit { get; set; }
        public double StorageOfUnit { get; set; }
        public double TotalUsageTokensSum { get; set; }
        public double TotalPurchasedTokensSum { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public DateTime SubscriptionExpirationDate { get; set; }
        public bool IsTokenApplied { get; set; }
        public double TotalManualTokenUsed { get; set; }
        public double TotalManualTokenSize { get; set; }
        public double TotalUsageManualTokensSum { get; set; }
        public double TotalPurchasedManualTokensSum { get; set; }
        public double ManualTokenFromOrganization { get; set; }
        public double ManualTokenOfUnit { get; set; }
        public bool IsManualTokenApplied { get; set; }
    }
}
