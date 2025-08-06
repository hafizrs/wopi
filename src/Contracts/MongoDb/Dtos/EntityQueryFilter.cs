using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos
{
    public class EntityQueryFilter
    {
        public string PropertyName { get; set; }
        public List<string> Values { get; set; }
        public Operators Operator { get; set; }
    }
}
