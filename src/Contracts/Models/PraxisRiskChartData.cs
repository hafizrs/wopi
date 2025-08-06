using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisRiskChartData
    {
        public string Label { get; set; }
        public List<ChartDataSet> DataSets { get; set; }
    }

    public class ChartDataSet
    {
        public int X { get; set; }
        public int Y { get; set; }
        public List<string> RiskList { get; set; }
    }
}