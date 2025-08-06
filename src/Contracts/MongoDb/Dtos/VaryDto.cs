using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos
{
    public class VaryDto
    {
        public int Value { get; set; }
        public string ItemId { get; set; }
        public string EntityName { get; set; }
        public string Field { get; set; }
        public OperationType Operation { get; set; }
    }
}
