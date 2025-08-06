using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class GetInterfaceToken
    {
        public string Code { get; set; }
        public string State { get; set; }
        public string Provider { get; set; }
        public string Refreshtoken { get; set; }
        public string RefreshtokenId { get; set; }
    }
}
