using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos
{
    public class EntityDbQueryResponse<T>
    {
        public int StatusCode { get; set; }
        public List<string> Projections { get; set; }
        public List<string> ErrorMessages { get; set; }
        public long TotalRecordCount { get; set; }
        public List<T> Results { get; set; }
    }
}
