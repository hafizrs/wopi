using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsInterfaceSession : EntityBase
    {
        public string Provider { get; set; }
        public string UserId { get; set; }
        public string RefreshTokenSessionId { get; set; }
    }
}
