using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule
{
    public class CreateReportTemplateSectionCommand
    {
        public List<PraxisReportTemplateSection> Sections { get; set; }
    }

}
