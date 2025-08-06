using Microsoft.Extensions.Configuration;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EmailServices
{
    public class PraxisEmailDataBuilders : IPraxisEmailDataBuilders
    {
        private readonly IConfiguration _configuration;

        public PraxisEmailDataBuilders(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Dictionary<string, string> BuildCreateOrganizationEmailData(string paymentInitializeId, string personName)
        {
            return new Dictionary<string, string>
            {
                { "DisplayName", personName },
                { "CreateOrganizationUrl", GetCreateOrganizationUrl(paymentInitializeId) }
            };
        }

        private string GetCreateOrganizationUrl(string paymentInitializeId)
        {
            return _configuration["PraxisWebUrl"] + "/purchase/create-organization-and-admin?PaymentInitializeId=" + paymentInitializeId;
        }

    }
}
