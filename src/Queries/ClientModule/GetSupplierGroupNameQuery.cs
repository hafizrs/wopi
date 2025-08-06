using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Queries
{
    public class GetSupplierGroupNameQuery
    {
        public string PraxisClientId { get; set; }
        public SupplierGroupNameModel SupplierGroupName { get; set; }
    }
}
