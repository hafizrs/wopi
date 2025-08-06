using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions
{
    public interface IDepartmentSubscriptionService
    {
        Task<DepartmentSubscriptionResponse> GetDepartmentSubscription(GetDepartmentSubscriptionQuery query);
        Task<DepartmentSubscription> GetDepartmentSubscriptionAsync(string praxisClientId); 
        void IncrementSubscriptionTokenUsage(DepartmentSubscription payload, double IncToken);
        Task SaveDepartmentSubscription(string clientId, PraxisClientSubscription clientSubs);
        Task<bool> DeleteStorageFromDepartmentSubscriptionAsync(string praxisClientId, double fileSizeInBytes); 
        Task<CheckValidUploadFileRequestResponse> GetValidUploadFileRequestInDepartmentSubscription(GetValidFileUploadRequestQuery query);
        Task<bool> IncrementDepartmentSubscriptionStorageUsage(ObjectArtifact objectArtifact);
        Task UpdateTokenBalanceOnSubscriptionExpiryAsync(string clientId);
        Task<bool> CheckSubscriptionTokenLimit();
        void IncrementSubscriptionManualTokenUsage(double IncToken);
    }
}
