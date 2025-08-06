using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class LibraryFormUpdatedEventPayload
    {
        public string ArtifactId { get; set; }
        public string OrganizationId { get; set; }
        public string ClientId { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
    }
}
