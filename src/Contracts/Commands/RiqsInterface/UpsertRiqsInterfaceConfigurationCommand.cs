using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class UpsertRiqsInterfaceConfigurationCommand
    {
        public string Provider { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public List<string> Scopes { get; set; }
        public string AuthorizationUrl { get; set; }
        public string TokenUrl { get; set; }
    }
}
