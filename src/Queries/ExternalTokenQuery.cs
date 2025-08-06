using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.Wopi.Queries
{
    public class ExternalTokenQuery
    {
        public string Email { get; set; }
        public string ExternalUserId { get; set; }
        public string ExternalUserItemId { get; set; }
    }
}
