using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule
{
    public class DeleteReportTemplateCommand
    {
        public List<string> TemplateIds { get; set; }
    }
}
