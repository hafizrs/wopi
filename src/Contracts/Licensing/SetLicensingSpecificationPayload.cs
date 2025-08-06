using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing
{
    public class SetLicensingSpecificationPayload
    {
        public List<SetLicensingSpecificationCommand> ArcFeatureLicensings { get; set; }
    }
}
