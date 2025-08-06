using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsInterfaceConfiguration : EntityBase
    {
        public string Code { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string Provider { get; set; }
        public string AuthorizationUrl { get; set; }
        public string TokenUrl { get; set; }
        public List<string> Scopes { get; set; }
    }
}
