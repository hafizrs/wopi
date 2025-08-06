using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions
{
    public class RiqsSubscriptionPricingCustomPackage : EntityBase 
    {
        public string SubscriptionId { get; set; } 
        public int NumberOfUser { get; set; }
        public double? DiscountOnPerUserAmount { get; set; }
        public double? DiscountAmount { get; set; }
        public double? DiscountPercentage { get; set; }
        public DateTime ValidityDate { get; set; }
        public bool IsSubscriptionUsed { get; set; }
    }
}
