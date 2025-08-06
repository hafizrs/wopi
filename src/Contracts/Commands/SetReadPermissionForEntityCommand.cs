using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class SetReadPermissionForEntityCommand
    {
        public string EntityName { get; set; }
        public List<string> AdditionalFields { get; set; }
    }
}