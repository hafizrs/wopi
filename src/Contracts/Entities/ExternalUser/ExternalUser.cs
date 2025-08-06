using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ExternalUser
{
    public class ExternalUser : EntityBase
    {
        public string Email { get; set; }
        public string SupplierId { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public string PraxisClientId { get; set; }
        public string[] Roles { get; set; }
        public string ClientSecretId { get; set; }
        public string ClientSecret { get; set; }
    }
}
