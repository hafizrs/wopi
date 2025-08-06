using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class ProvideLogoLocationService : IProvideLogoLocation
    {
        private readonly ILogger<ProvideLogoLocationService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;

        public ProvideLogoLocationService(
            ILogger<ProvideLogoLocationService> logger,
            ISecurityContextProvider securityContextProvider,
            IRepository repository)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _repository = repository;
        }

        public string GetLocation(string clientId = null)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var roleList = new List<string>() { "admin", "task_controller", "poweruser" };
                var role = securityContext.Roles.FirstOrDefault(r => roleList.Contains(r));

                if (role == RoleNames.Admin || role == RoleNames.TaskController)
                {
                    return ReportConstants.rqSystemLogo;
                }

                if (role == nameof(RoleNames.PowerUser) && !string.IsNullOrEmpty(clientId))
                {
                    var praxisClientSubscription = _repository.GetItems<PraxisClientSubscription>(s => s.ClientId == clientId).OrderByDescending(x => x.CreateDate).FirstOrDefault();
                    if (praxisClientSubscription != null)
                    {
                        return praxisClientSubscription.SubscriptionPackage switch
                        {
                            nameof(PraxisSubscriptionPackage.COMPLETE_PACKAGE) => ReportConstants.rqSystemLogo,
                            nameof(PraxisSubscriptionPackage.PROCESS_GUIDE) => ReportConstants.processGuideLogo,
                            nameof(PraxisSubscriptionPackage.RQ_MONITOR) => ReportConstants.processMonitorLogo,
                            _ => string.Empty,
                        };
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during generate Process guide overview report. Exception message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }
    }
}
