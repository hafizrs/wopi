using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class ExternalUserTokenResponse
    {
        public string scope { get; set; }
        public string token_type { get; set; }
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string refresh_token { get; set; }
        public string ip_address { get; set; }
        public string praxis_client_id { get; set; }
        public string provider { get; set; }
        public string refresh_token_id { get; set; }
    }
}
