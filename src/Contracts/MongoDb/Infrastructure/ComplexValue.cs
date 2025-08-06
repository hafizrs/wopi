namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Infrastructure
{
    public class ComplexValue
    {
        public string Value { get; set; }
        public FilterType FilterType { get; set; }
        public ComplexValue()
        {
            FilterType = FilterType.Simple;
        }
    }
}
