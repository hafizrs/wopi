using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.Infrastructure
{
    public class MongoDBQueryResponse<T>
    {
        public int StatusCode { get; set; }

        public List<string> Projections { get; set; }

        public List<string> ErrorMessages { get; set; }

        public long TotalRecordCount { get; set; }

        public List<T> Results { get; set; }
    }
}