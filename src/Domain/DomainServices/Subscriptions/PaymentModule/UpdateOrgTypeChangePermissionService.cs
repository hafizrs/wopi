using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class UpdateOrgTypeChangePermissionService: IUpdateOrgTypeChangePermissionService
    {
        private readonly ILogger<UpdateOrgTypeChangePermissionService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        public UpdateOrgTypeChangePermissionService(ILogger<UpdateOrgTypeChangePermissionService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<bool> UpdateOrgTypeChangePermission(string clientId, string paymentDetailId)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var client = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == clientId && !c.IsMarkedToDelete);
                if (client != null)
                {
                    client.IsOrgTypeChangeable = securityContext.Roles.Contains(RoleNames.Admin);
                    client.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                    await _repository.UpdateAsync<PraxisClient>(c => c.ItemId == client.ItemId, client);
                    _logger.LogInformation("IsOrgTypeChangeable property value set to {IsOrgTypeChangeable} for {EntityName} entity.", client.IsOrgTypeChangeable, nameof(PraxisClient));
                    if (!string.IsNullOrEmpty(paymentDetailId))
                    {
                        var clientSubscriptionList = _repository.GetItems<PraxisClientSubscription>(s => s.PaymentHistoryId == paymentDetailId && s.ClientId == clientId && s.IsLatest).ToList();
                        if (clientSubscriptionList != null && clientSubscriptionList.Count > 0)
                        {
                            foreach (var clientSubscription in clientSubscriptionList)
                            {
                                clientSubscription.IsOrgTypeChangeable = securityContext.Roles.Contains(RoleNames.Admin);
                                clientSubscription.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                                await _repository.UpdateAsync<PraxisClientSubscription>(s => s.ItemId == clientSubscription.ItemId, clientSubscription);
                                _logger.LogInformation("IsOrgTypeChangeable property value set to {IsOrgTypeChangeable} for {EntityName} entity.", clientSubscription.IsOrgTypeChangeable, nameof(PraxisClientSubscription));
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                _logger.LogError("Error in update org type permission service for client id -> {ClientId}", clientId);
                return false;
            }
            return true;
        }
    }
}
