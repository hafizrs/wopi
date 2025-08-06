using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class DeleteFileFromVectorDBCommand
    {
        public string file_id { get; set; }
        public List<IFilterKeyValue> filter_key_value_pair { get; set; }
    }

    public class IFilterKeyValue
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
