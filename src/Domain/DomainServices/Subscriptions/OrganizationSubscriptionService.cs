using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Subscriptions
{
    public class OrganizationSubscriptionService : IOrganizationSubscriptionService
    {
        private readonly IRepository _repository;
        private readonly ILogger<OrganizationSubscriptionService> _logger;
        private readonly IChangeLogService _changeLogService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IMongoClientRepository _mongoClientRepository;

        public OrganizationSubscriptionService(
            IRepository repository,
            ILogger<OrganizationSubscriptionService> logger,
            IChangeLogService changeLogService,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IMongoClientRepository mongoClientRepository
        )
        {
            _repository = repository;
            _logger = logger;
            _changeLogService = changeLogService;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _mongoClientRepository = mongoClientRepository;
        }

        public async Task<OrganizationSubscriptionResponse> GetOrganizationSubscription(GetOrganizationSubscriptionQuery query)
        {
            try
            {
                var subscription = await _repository.GetItemAsync<OrganizationSubscription>(d => d.OrganizationId == query.OrganizationId);

                var response = subscription != null ? new OrganizationSubscriptionResponse
                {
                    OrganizationId = subscription.OrganizationId,
                    TotalTokenUsed = subscription.TotalTokenUsed,
                    TotalTokenSize = subscription.TotalTokenSize,
                    TotalStorageUsed = subscription.TotalStorageUsed,
                    TotalStorageSize = subscription.TotalStorageSize,
                    TokenOfOrganization = subscription.TokenOfOrganization,
                    StorageOfOrganization = subscription.StorageOfOrganization,
                    TokenOfUnits = subscription.TokenOfUnits,
                    StorageOfUnits = subscription.StorageOfUnits,
                    SubscriptionDate = subscription.SubscriptionDate,
                    SubscriptionExpirationDate = subscription.SubscriptionExpirationDate,
                    IsTokenApplied = subscription.IsTokenApplied,
                    IsSubscriptionExpired = subscription.SubscriptionExpirationDate < DateTime.UtcNow,
                    TotalManualTokenUsed = subscription.TotalManualTokenUsed,
                    TotalManualTokenSize = subscription.TotalManualTokenSize,
                    TotalUsageManualTokensSum = subscription.TotalUsageManualTokensSum,
                    TotalPurchasedManualTokensSum = subscription.TotalPurchasedManualTokensSum,
                    ManualTokenOfOrganization = subscription.ManualTokenOfOrganization,
                    ManualTokenOfUnits = subscription.ManualTokenOfUnits,
                    IsManualTokenApplied = subscription.IsManualTokenApplied
                } : null;

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
                return await Task.FromResult(new OrganizationSubscriptionResponse());
            }
        }

        public void IncrementSubscriptionTokenUsage(OrganizationSubscription payload, double IncToken)
        {
            try
            {
                if (payload != null)
                {
                    var collection = _mongoClientRepository.GetCollection(nameof(OrganizationSubscription));
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", payload.ItemId);

                    var update = Builders<BsonDocument>.Update.Inc(nameof(OrganizationSubscription.TotalTokenUsed), IncToken);
                    _ = collection.UpdateOne(filter, update);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public void IncrementSubscriptionManualTokenUsage(double orgToken)
        {
            try
            {
                string organizationId = GetUserOrgId();

                var existingOrgSubs = GetOrganizationSubscriptionAsync(organizationId).GetAwaiter().GetResult();

                if (existingOrgSubs != null)
                {
                    var IncToken = existingOrgSubs.TotalManualTokenUsed + orgToken;

                    var updateFields = new Dictionary<string, object>
                    {
                        { nameof(OrganizationSubscription.TotalManualTokenUsed), IncToken }
                    };

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", existingOrgSubs.ItemId);

                    _changeLogService.UpdateChange(nameof(OrganizationSubscription), filter, updateFields).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }


        public async Task<OrganizationSubscription> GetOrganizationSubscriptionAsync(string organizationId)
        {
            return await _repository.GetItemAsync<OrganizationSubscription>(
               ds => ds.OrganizationId == organizationId
           );
        }

        public async Task SaveOrganizationSubscription(OrganizationSubscription organizationSubs)
        {
            var praxisOrganization = await _repository.GetItemAsync<PraxisOrganization>(c => c.ItemId == organizationSubs.OrganizationId);

            var tokensInMillion = 1_000_000;
            var bytesInGigabyte = Math.Pow(1024, 3);
            organizationSubs.TotalTokenSize *= tokensInMillion;
            organizationSubs.TokenOfOrganization *= tokensInMillion;
            organizationSubs.TokenOfUnits *= tokensInMillion;

            organizationSubs.TotalManualTokenSize *= tokensInMillion;
            organizationSubs.ManualTokenOfOrganization *= tokensInMillion;
            organizationSubs.ManualTokenOfUnits *= tokensInMillion;

            organizationSubs.TotalStorageSize *= bytesInGigabyte;
            organizationSubs.StorageOfOrganization *= bytesInGigabyte;
            organizationSubs.StorageOfUnits *= bytesInGigabyte;


            if (praxisOrganization != null)
            {
                var existingOrganizationSubs = await GetOrganizationSubscriptionAsync(organizationSubs.OrganizationId);
                if (existingOrganizationSubs == null)
                {
                    existingOrganizationSubs = new OrganizationSubscription()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        OrganizationId = organizationSubs.OrganizationId,
                        TotalTokenUsed = 0,
                        TotalTokenSize = organizationSubs.TotalTokenSize,
                        TotalStorageUsed = 0,
                        TotalStorageSize = organizationSubs.TotalStorageSize,
                        TokenOfOrganization = organizationSubs.TokenOfOrganization,
                        StorageOfOrganization = organizationSubs.StorageOfOrganization,
                        TokenOfUnits = organizationSubs.TokenOfUnits,
                        StorageOfUnits = organizationSubs.StorageOfUnits,
                        TotalUsageTokensSum = 0,
                        TotalPurchasedTokensSum = 0,
                        SubscriptionDate = organizationSubs.SubscriptionDate,
                        SubscriptionExpirationDate = organizationSubs.SubscriptionExpirationDate,
                        IsTokenApplied = organizationSubs.IsTokenApplied,
                        TotalManualTokenUsed = 0,
                        TotalManualTokenSize = organizationSubs.TotalManualTokenSize,
                        TotalUsageManualTokensSum = 0,
                        TotalPurchasedManualTokensSum = 0,
                        ManualTokenOfOrganization = organizationSubs.ManualTokenOfOrganization,
                        ManualTokenOfUnits = organizationSubs.ManualTokenOfUnits,
                        IsManualTokenApplied = organizationSubs.IsManualTokenApplied
                    };
                    await _repository.SaveAsync(existingOrganizationSubs);
                }
                else
                {
                    existingOrganizationSubs.TotalTokenSize = organizationSubs.TotalTokenSize;
                    existingOrganizationSubs.TokenOfOrganization = organizationSubs.TokenOfOrganization;
                    existingOrganizationSubs.TokenOfUnits = organizationSubs.TokenOfUnits;
                    existingOrganizationSubs.TotalStorageSize = organizationSubs.TotalStorageSize;
                    existingOrganizationSubs.StorageOfOrganization = organizationSubs.StorageOfOrganization;
                    existingOrganizationSubs.StorageOfUnits = organizationSubs.StorageOfUnits;
                    existingOrganizationSubs.SubscriptionDate = organizationSubs.SubscriptionDate;
                    existingOrganizationSubs.SubscriptionExpirationDate = organizationSubs.SubscriptionExpirationDate;
                    existingOrganizationSubs.IsTokenApplied = organizationSubs.IsTokenApplied;
                    existingOrganizationSubs.TotalManualTokenSize = organizationSubs.TotalManualTokenSize;
                    existingOrganizationSubs.ManualTokenOfOrganization = organizationSubs.ManualTokenOfOrganization;
                    existingOrganizationSubs.ManualTokenOfUnits = organizationSubs.ManualTokenOfUnits;
                    existingOrganizationSubs.IsManualTokenApplied = organizationSubs.IsManualTokenApplied;

                    await _repository.UpdateAsync(s => s.ItemId == existingOrganizationSubs.ItemId, existingOrganizationSubs);
                }
            }
        }

        public async Task<bool> IncrementOrganizationSubscriptionStorageUsage(ObjectArtifact objectArtifact)
        {
            var orgSubscription = await GetOrganizationSubscriptionAsync(objectArtifact.OrganizationId);

            if (!string.IsNullOrWhiteSpace(objectArtifact.OrganizationId) && orgSubscription != null)
            {
                var totalStorageUsed = orgSubscription.TotalStorageUsed + objectArtifact.FileSizeInByte;
                var updateFields = new Dictionary<string, object>
                    {
                        { nameof(OrganizationSubscription.TotalStorageUsed), totalStorageUsed },
                    };

                var filter = Builders<BsonDocument>.Filter.Eq("_id", orgSubscription.ItemId);

                await _changeLogService.UpdateChange(nameof(OrganizationSubscription), filter, updateFields);
            }

            return true;
        }

        public async Task<bool> DeleteStorageFromOrganizationSubscriptionAsync(string organizationId, double fileSizeInBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(organizationId)) return false;

                var orgSubscription = await GetOrganizationSubscriptionAsync(organizationId);

                if (orgSubscription != null)
                {
                    var updatedTotalStorage = Math.Max(0, orgSubscription.TotalStorageUsed - fileSizeInBytes);
                    var updates = new Dictionary<string, object>
                    {
                        {"TotalStorageUsed", updatedTotalStorage},
                    };

                    await _repository.UpdateAsync<OrganizationSubscription>(ds => ds.ItemId == orgSubscription.ItemId, updates);

                    _logger.LogInformation("Updated previous organizationId subscription from latest for organizationId: {OrganizationId}", organizationId);
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                   $"Exception occured in update previous department subscription data" +
                   $"Exception Message: {ex.Message}. " +
                   $"Exception Details: {ex.StackTrace}.");

                return false;
            }

            return true;
        }

        public async Task UpdateTokenBalanceOnSubscriptionExpiryAsync(string organizationId)
        {
            try
            {
                var orgSubscription = await GetOrganizationSubscriptionAsync(organizationId);

                if (orgSubscription != null)
                {
                    var updateFields = new Dictionary<string, object>
                    {
                        { nameof(OrganizationSubscription.TotalTokenUsed), 0 },
                        { nameof(OrganizationSubscription.TotalUsageTokensSum), orgSubscription.TotalUsageTokensSum + orgSubscription.TotalTokenUsed },
                        { nameof(OrganizationSubscription.TotalTokenSize), 0 },
                        { nameof(OrganizationSubscription.TotalPurchasedTokensSum), orgSubscription.TotalPurchasedTokensSum + orgSubscription.TotalTokenSize }
                    };

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", orgSubscription.ItemId);

                    await _changeLogService.UpdateChange(nameof(OrganizationSubscription), filter, updateFields);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public async Task<bool> CheckSubscriptionExpired()
        {
            if (_securityHelperService.IsAAdmin()) { return false; }

            string orgId = GetUserOrgId();

            if (string.IsNullOrEmpty(orgId)) { return true; }

            var orgSubscription = await GetOrganizationSubscriptionAsync(orgId);

            if (orgSubscription == null)
            {
                return true;
            }

            bool isExpired = orgSubscription.SubscriptionExpirationDate < DateTime.UtcNow;

            return isExpired;
        }

        public string GetUserOrgId()
        {
            if (_securityHelperService.IsAAdmin()) return string.Empty;

            var userId = _securityContextProvider.GetSecurityContext().UserId;

            if (string.IsNullOrEmpty(userId)) return string.Empty;

            var primaryClient = _repository.GetItem<PraxisUser>(pu => pu.UserId == userId)?
                .ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
            var orgId = primaryClient.ParentOrganizationId ?? string.Empty;

            if (_securityHelperService.IsADepartmentLevelUser())
            {
                orgId = _securityHelperService.ExtractOrganizationFromOrgLevelUser() ?? orgId;
            }

            return orgId;
        }
    }
}
