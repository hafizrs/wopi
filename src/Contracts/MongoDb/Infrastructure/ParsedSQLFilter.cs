using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Infrastructure
{
    public class ParsedSQLFilter
    {
        public string PropertyName { get; set; }
        public ComplexValue[] Values { get; set; }
        public Operators Operator { get; set; }
    }
}
