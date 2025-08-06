using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class EntityQueryResponse<T>
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public long TotalRecordCount { get; set; }
        public IEnumerable<T> Results { get; set; }
    }
}
