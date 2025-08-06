using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CloneDepartmentSuppliersCommand
    {
        public string DepartmentId { get; set; }
        public string SupplierCategoryKey { get; set; }
        public List<string> SupplierIds { get; set; }
    }
}
