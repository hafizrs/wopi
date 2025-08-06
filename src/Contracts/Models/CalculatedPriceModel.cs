using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class CalculatedPriceModel
    {
        public string SubscriptionPackage { get; set; }
        public double CalculatedPrice { get; set; }
        public double CalculatedPriceWithoutDuration { get; set; }
        public double PerUserMonthlyPrice { get; set; }
        public double TotalUserMonthlyPrice { get; set; }
        public int TotalUserNumber { get; set; }
        public int DurationOfSubscription { get; set; }
    }
    public enum SubscriptionPeriod
    {
        Annual,
        SemiAnnual,
        Quarterly
    }
}
