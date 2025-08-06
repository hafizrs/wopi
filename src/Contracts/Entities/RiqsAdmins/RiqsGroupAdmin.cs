using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsAdmins
{
    public class RiqsGroupAdmin : EntityBase
    {
        public string PraxisUserId { get; set; }
        public string UserId { get; set; }
        public List<string> OrganizationIds { get; set; }
        public List<string> UserCreatedOrganizationIds { get; set; }
        public bool IsGroupAdmin { get; set; }
    }
}
