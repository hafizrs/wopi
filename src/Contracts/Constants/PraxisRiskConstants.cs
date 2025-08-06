using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class PraxisRiskConstants
    {
        public static IDictionary<string, int> RiskAssessmentImpactKeys { get; set; } = new Dictionary<string, int>
        {
            { "INSIGNIFICANT", 1 },
            { "LOW", 2 },
            { "NOTICEABLE", 3 },
            { "CRITICAL", 4 },
            { "CATASTROPHIC", 5 },
        };

        public static IDictionary<string, int> RiskAssessmentProbabilityKeys { get; set; } = new Dictionary<string, int>
        {
            { "UNLIKELY", 1 },
            { "VERY_RARE", 2 },
            { "RARE", 3 },
            { "POSSIBLE", 4 },
            { "FREQUENTLY", 5 },
        };
    }
}