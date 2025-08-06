using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class EntitySignatureMappingResponse : EntitySignatureMapping
    {
        public bool IsLinkExpired { get; set; }
    }
}
