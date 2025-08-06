using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ExternalUser
{
    public class ClientCredential : EntityBase
    {
        public string[] Roles { get; set; }
        public string ClientSecret { get; set; }
        public string OrganizationId { get; set; }
    }
}
