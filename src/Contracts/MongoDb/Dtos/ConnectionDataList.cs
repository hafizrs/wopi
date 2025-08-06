using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos
{
    public class ConnectionDataList
    {
        public long TotalRecordCount { get; set; }
        public List<ConnectionResult> Results { get; set; }
    }
}
