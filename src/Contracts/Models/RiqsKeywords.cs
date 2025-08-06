using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsKeyword : EntityBase
    {
        public string OrganizationId { get; set; }
        public string ModuleName { get; set; }
        public string KeyName { get; set; }
        public string[] Values { get; set; }
    }
}
