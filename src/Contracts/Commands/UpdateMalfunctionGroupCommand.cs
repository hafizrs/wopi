using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateMalfunctionGroupCommand
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
    }
}
