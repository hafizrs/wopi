using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions
{
    public interface IOrganizationSubscriptionService
    {
        Task<OrganizationSubscriptionResponse> GetOrganizationSubscription(GetOrganizationSubscriptionQuery query);
        Task<OrganizationSubscription> GetOrganizationSubscriptionAsync(string organizationId);
        void IncrementSubscriptionTokenUsage(OrganizationSubscription payload, double IncToken);
        Task SaveOrganizationSubscription(OrganizationSubscription organizationSubs); 
        Task<bool> DeleteStorageFromOrganizationSubscriptionAsync(string organizationId, double fileSizeInBytes);
        Task<bool> IncrementOrganizationSubscriptionStorageUsage(ObjectArtifact objectArtifact);
        Task UpdateTokenBalanceOnSubscriptionExpiryAsync(string organizationId);
        void IncrementSubscriptionManualTokenUsage(double IncToken);
        Task<bool> CheckSubscriptionExpired();
    }
}
