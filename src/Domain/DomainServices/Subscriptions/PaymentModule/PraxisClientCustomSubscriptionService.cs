using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class PraxisClientCustomSubscriptionService : IPraxisClientCustomSubscriptionService
    {
        private readonly ILogger<PraxisClientCustomSubscriptionService> _logger;
        private readonly IProcessClientData _processClientDataService;
        private readonly IRepository _repository;
        public PraxisClientCustomSubscriptionService(ILogger<PraxisClientCustomSubscriptionService> logger,
            IRepository repository,
            IProcessClientData processClientDataService)
        {
            _processClientDataService = processClientDataService;
            _repository = repository;
            _logger = logger;
        }
        public bool SaveSubscriptionData(PraxisClient client, int numberOfUser, int durationOfSubscription, string paymentMethod, int additionalStorage)
        {
            try
            {
                var subscriptionPackageInfo = _processClientDataService.GetSubscriptionPackageInfo(client);
                if (subscriptionPackageInfo == null)
                {
                    _logger.LogInformation("Subscription package seed not found for clientId -> {ClientId}", client.ItemId);
                    return false;
                }
                var rolesAllowToRead = new List<string>()
                {
                    $"{RoleNames.PoweruserPayment}_{client.ItemId}",
                    $"{RoleNames.PowerUser_Dynamic}_{client.ItemId}",
                    $"{RoleNames.Leitung_Dynamic}_{client.ItemId}",
                    $"{RoleNames.MpaGroup_Dynamic}_{client.ItemId}",
                    $"{RoleNames.Admin}",
                    $"{RoleNames.TaskController}"
                };
                var clientSubscription = new PraxisClientSubscription
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow.ToLocalTime(),
                    RolesAllowedToRead = rolesAllowToRead.ToArray(),
                    NumberOfUser = numberOfUser,
                    DurationOfSubscription = durationOfSubscription,
                    SubscriptionPackage = subscriptionPackageInfo.Title,
                    ModuleList = subscriptionPackageInfo.ModuleList,
                    ClientEmail = client.ContactEmail,
                    SubscriptionDate = DateTime.UtcNow.ToLocalTime(),
                    SubscriptionExpirationDate = DateTime.UtcNow.Date.AddMonths(durationOfSubscription).AddSeconds(-1),
                    IsOrgTypeChangeable = true,
                    SubscritionStatus = nameof(PraxisEnums.ONGOING),
                    PaymentMethod = paymentMethod,
                    IsLatest = true,
                    IsMarkedToDelete = false,
                    LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                    ClientId = client.ItemId,
                    ClientName = client.ClientName,
                    StorageSubscription = new StorageSubscriptionInfo
                    {
                        IncludedStorageInGigaBites = numberOfUser*.5,
                        TotalAdditionalStorageInGigaBites = additionalStorage
                    },
                    Tags = new string[] { "Is-Valid-PraxisClient" }
                };

                _repository.Save(clientSubscription);
                _logger.LogInformation("Data has been successfully inserted to {EntityName}.", nameof(PraxisClientSubscription));

                _processClientDataService.ProcessPraxisClientSubscriptionNotification(client, clientSubscription);
                _logger.LogInformation("subscription added for clientId -> {ClientId}", client.ItemId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception Occured during inserting data to {EntityName} entity. Exception Message: {Message}. Exception Details: {StackTrace}.", nameof(PraxisClientSubscription), ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}
