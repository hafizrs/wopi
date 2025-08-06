using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CloneDepartmentCategoriesCommand
    {
        public string DepartmentId { get; set; }
        public List<string> CategoryIds { get; set; }
    }
}
