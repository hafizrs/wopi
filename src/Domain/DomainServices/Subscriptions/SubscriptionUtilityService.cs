
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.Entities.PrimaryEntities.Giraffe;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Subscriptions
{
    public class SubscriptionUtilityService : ISubscriptionUtilityService
    {
        private readonly IRepository _repository;
        private readonly ILogger<SubscriptionUtilityService> _logger;
        

        public SubscriptionUtilityService(
            IRepository repository,
            ILogger<SubscriptionUtilityService> logger
        )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SubscriptionsInfoResponse> GetSubscriptionsInfo(GetSubscriptionsInfoQuery query)
        {
            try
            {
                var deptSubsTask = string.IsNullOrEmpty(query.PraxisClientId)
                    ? Task.FromResult<DepartmentSubscription>(null)
                    : _repository.GetItemAsync<DepartmentSubscription>(d => d.PraxisClientId == query.PraxisClientId);

                var orgSubsTask = string.IsNullOrEmpty(query.OrganizationId)
                    ? Task.FromResult<OrganizationSubscription>(null)
                    : _repository.GetItemAsync<OrganizationSubscription>(d => d.OrganizationId == query.OrganizationId);

                await Task.WhenAll(deptSubsTask, orgSubsTask);

                var deptSubs = deptSubsTask.Result;
                var orgSubs = orgSubsTask.Result;

                var response = new SubscriptionsInfoResponse();

                if (deptSubs != null)
                {
                    response.DepartmentSubscription = new DepartmentSubscriptionResponse
                    {
                        PraxisClientId = deptSubs.PraxisClientId,
                        TotalTokenUsed = deptSubs.TotalTokenUsed,
                        TotalTokenSize = deptSubs.TotalTokenSize,
                        TotalStorageUsed = deptSubs.TotalStorageUsed,
                        TotalStorageSize = deptSubs.TotalStorageSize,
                        TokenFromOrganization = deptSubs.TokenFromOrganization,
                        StorageFromOrganization = deptSubs.StorageFromOrganization,
                        TokenOfUnit = deptSubs.TokenOfUnit,
                        StorageOfUnit = deptSubs.StorageOfUnit,
                        SubscriptionDate = deptSubs.SubscriptionDate,
                        SubscriptionExpirationDate = deptSubs.SubscriptionExpirationDate,
                        IsTokenApplied = deptSubs.IsTokenApplied,
                        TotalManualTokenSize = deptSubs.TotalManualTokenSize,
                        TotalManualTokenUsed = deptSubs.TotalManualTokenUsed,
                        IsManualTokenApplied = deptSubs.IsManualTokenApplied
                    };
                }

                if (orgSubs != null)
                {
                    response.OrganizationSubscription = new OrganizationSubscriptionResponse
                    {
                        OrganizationId = orgSubs.OrganizationId,
                        TotalTokenUsed = orgSubs.TotalTokenUsed,
                        TotalTokenSize = orgSubs.TotalTokenSize,
                        TotalStorageUsed = orgSubs.TotalStorageUsed,
                        TotalStorageSize = orgSubs.TotalStorageSize,
                        TokenOfOrganization = orgSubs.TokenOfOrganization,
                        StorageOfOrganization = orgSubs.StorageOfOrganization,
                        TokenOfUnits = orgSubs.TokenOfUnits,
                        StorageOfUnits = orgSubs.StorageOfUnits,
                        SubscriptionDate = orgSubs.SubscriptionDate,
                        SubscriptionExpirationDate = orgSubs.SubscriptionExpirationDate,
                        IsTokenApplied = orgSubs.IsTokenApplied,
                        IsSubscriptionExpired = orgSubs.SubscriptionExpirationDate < DateTime.UtcNow,
                        TotalManualTokenUsed = orgSubs.TotalManualTokenUsed,
                        TotalManualTokenSize = orgSubs.TotalManualTokenSize,
                        IsManualTokenApplied = orgSubs.IsManualTokenApplied
                    };
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
                return new SubscriptionsInfoResponse();
            }
        }
    }
}
