using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class DeleteSubscriptionPricingCustomPackageCommand
    {
        public string ItemId { get; set; }
    }
}
