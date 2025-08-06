using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateLibraryGroupCommand
    {
        public string OrganizationId { get; set; }
        public string GroupName { get; set; }
        public string SubGroupName { get; set; }
        public string SubSubGroupName { get; set; }
    }
}
