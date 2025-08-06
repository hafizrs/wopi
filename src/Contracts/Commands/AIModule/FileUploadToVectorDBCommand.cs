using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class FileUploadToVectorDBCommand
    {
        public string file_id { get; set; }
        public bool is_old_cluster { get; set; }
        public string file_url { get; set; }
        public string file_name { get; set; }
        public string tenant_id { get; set; }
        public string create_date { get; set; }
        public List<IAdditionalKeyValue> additional_key_value { get; set; }
        public ISubscriptionFilter subscription_filter { get; set; }
        public string webhook_url { get; set; }
    }

    public class IAdditionalKeyValue
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class ISubscriptionFilter
    {
        public string Context { get; set; }
        public string ActionName { get; set; }
        public string Value { get; set; }
    }

    public class FileUploadToVectorDBResponse
    {
        public string message { get; set; }
        public bool error { get; set; }
        public List<IStatuses> statuses { get; set; }
    }

    public class IStatuses
    {
        public string file_id { get; set; }
        public string status { get; set; }
        public int total_chunks { get; set; }
    }
}
