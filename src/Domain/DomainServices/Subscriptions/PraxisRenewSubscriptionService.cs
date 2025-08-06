using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisRenewSubscriptionService : IPraxisRenewSubscriptionService
    {
        private readonly ILogger<PraxisRenewSubscriptionService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;

        public PraxisRenewSubscriptionService(
            ILogger<PraxisRenewSubscriptionService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IRepository repository)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _repository = repository;
        }

        public async Task<CommandResponse> IsAValidRenewSubscriptionRequestForOrg(string orgId) 
        {
            var result = new CommandResponse();
            try
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var currentDate = DateTime.UtcNow;

                var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.UserId == userId && !pu.IsMarkedToDelete);
                if (praxisUser == null)
                {
                    result.SetError("ExternalError", "Praxis user not found");
                    return result;
                }

                var orgData = await _repository.GetItemAsync<PraxisOrganization>(o => o.ItemId == orgId && !o.IsMarkedToDelete);

                if (orgData == null)
                {
                    result.SetError("ExternalError", "Organization data not found");
                    return result;
                }

                var praxisUserId = praxisUser.ItemId;
                if (orgData.AdminUserId == praxisUserId || orgData.DeputyAdminUserId == praxisUserId)
                {
                    var nextsubscriptionNotificationData = await GetOrganizationNextSubscriptionNotificationData(orgId);

                    var currentSubscriptionNotificationData = await GetOrganizationCurrentSubscriptionNotificationData(orgId);
                    
                    if (nextsubscriptionNotificationData != null)
                    {
                        result.SetError("ExternalError", "Subscription renewed already.");
                    }

                    if (currentSubscriptionNotificationData != null)
                    {
                        result.SetError("ExternalError", "Invalid renew subscription request");
                    }
                }
                else
                {
                    result.SetError("ExternalError", "This user is not an Admin or Deputy admin user");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurd in IsAValidRenewSubscriptionRequest with error -> {Message}", ex.Message);
                result.SetError("ExternalError", ex.Message);
                return result;
            }
        }

        public async Task<CommandResponse> IsAValidRenewSubscriptionRequestForClient(string clientId)  
        {
            var result = new CommandResponse();
            try
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var currentDate = DateTime.UtcNow;

                var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.UserId == userId && !pu.IsMarkedToDelete);
                if (praxisUser == null)
                {
                    result.SetError("ExternalError", "Praxis user not found");
                    return result;
                }

                var clientData = await _repository.GetItemAsync<PraxisClient>(o => o.ItemId == clientId && !o.IsMarkedToDelete);

                if (clientData == null)
                {
                    result.SetError("ExternalError", "Client data not found");
                    return result;
                }

                if (_securityHelperService.IsAPowerUser() || _securityHelperService.IsAAdminBUser())
                {
                    var currentDept = await GetClientCurrentSubscriptionNotificationData(clientId);
                    var currentOrg = await GetOrganizationCurrentSubscriptionNotificationData(clientData.ParentOrganizationId);
                    if (currentDept?.SubscriptionExpirationDate == currentOrg?.SubscriptionExpirationDate)
                    {
                        result.SetError("ExternalError", "Please renew subscription for organization first");
                    }
                }
                else
                {
                    result.SetError("ExternalError", "This user is not a power user");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurd in IsAValidRenewSubscriptionRequest with error -> {ex.Message}");
                result.SetError("ExternalError", ex.Message);
                return result;
            }
        }

        public async Task<PraxisClientSubscriptionNotification> GetOrganizationCurrentSubscriptionNotificationData(string organizationId)
        {
            var currentDate = DateTime.UtcNow;

            return await _repository.GetItemAsync<PraxisClientSubscriptionNotification>(s =>
                            !s.IsMarkedToDelete &&
                            s.OrganizationId == organizationId &&
                            s.IsActive &&
                            currentDate <= s.SubscriptionExpirationDate);
        }

        private async Task<PraxisClientSubscriptionNotification> GetOrganizationNextSubscriptionNotificationData(string organizationId)
        {
            return await _repository.GetItemAsync<PraxisClientSubscriptionNotification>(s =>
            !s.IsMarkedToDelete &&
            s.OrganizationId == organizationId &&
            !s.IsActive &&
            s.SubscriptionExpirationDate > DateTime.UtcNow);
        }

        public async Task<PraxisClientSubscriptionNotification> GetClientCurrentSubscriptionNotificationData(string clientId)
        {
            var currentDate = DateTime.UtcNow;

            return await _repository.GetItemAsync<PraxisClientSubscriptionNotification>(s =>
                            !s.IsMarkedToDelete &&
                            s.ClientId == clientId &&
                            s.IsActive &&
                            currentDate <= s.SubscriptionExpirationDate);
        }
    }
}
