using System.Collections.Generic;
using Newtonsoft.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class GqlEvent<T>
    {
        public string TypeName { get; set; }
        public string TypeFullName { get; set; }
        public T EntityData { get; set; }

        private string filter;
        public string Filter
        {
            get => filter;
            set
            {
                Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
                filter = dict["_id"];
            }
        }
        public string EventName { get; set; }
        public string EventData { get; set; }
    }
}
