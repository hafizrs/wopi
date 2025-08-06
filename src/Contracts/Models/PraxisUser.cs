using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Models
{
    public class PraxisUser : EntityBase
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<PraxisClient> ClientList { get; set; } = new List<PraxisClient>();
    }

    public class PraxisClient
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
} 