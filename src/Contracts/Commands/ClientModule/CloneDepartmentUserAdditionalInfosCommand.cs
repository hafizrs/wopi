using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CloneDepartmentUserAdditionalInfosCommand
    {
        public string DepartmentId { get; set; }
        public List<string> AdditionalIds { get; set; }
    }
}
