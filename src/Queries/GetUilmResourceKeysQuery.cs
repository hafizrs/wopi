using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetUilmResourceKeysQuery
    {
        public List<string> KeyNameList { get; set; }
        public List<string> AppIds { get; set; }
    }
}