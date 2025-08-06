using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class SupplierStatusResponse: CurrentStatusResponse
    {
        public List<string> DependentIds { get; set; }
    }
}
