using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
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
    public class DepartmentSubscriptionService : IDepartmentSubscriptionService
    {
        private readonly IRepository _repository;
        private readonly ILogger<DepartmentSubscriptionService> _logger;
        private readonly IChangeLogService _changeLogService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IMongoClientRepository _mongoClientRepository;

        public DepartmentSubscriptionService(
            IRepository repository,
            ILogger<DepartmentSubscriptionService> logger,
            IChangeLogService changeLogService,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IMongoClientRepository mongoClientRepository
        )
        {
            _repository = repository;
            _logger = logger;
            _changeLogService = changeLogService;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _mongoClientRepository = mongoClientRepository;
        }

        public async Task<DepartmentSubscriptionResponse> GetDepartmentSubscription(GetDepartmentSubscriptionQuery query)
        {
            try
            {
                var client = await _repository.GetItemAsync<DepartmentSubscription>(d => d.PraxisClientId == query.PraxisClientId);

                var response = client != null ? new DepartmentSubscriptionResponse
                {
                    PraxisClientId = client.PraxisClientId,
                    TotalTokenUsed = client.TotalTokenUsed,
                    TotalTokenSize = client.TotalTokenSize,
                    TotalStorageUsed = client.TotalStorageUsed,
                    TotalStorageSize = client.TotalStorageSize,
                    TokenFromOrganization = client.TokenFromOrganization,
                    StorageFromOrganization = client.StorageFromOrganization,
                    TokenOfUnit = client.TokenOfUnit,
                    StorageOfUnit = client.StorageOfUnit,
                    SubscriptionDate = client.SubscriptionDate,
                    SubscriptionExpirationDate = client.SubscriptionExpirationDate,
                    IsTokenApplied = client.IsTokenApplied,
                    TotalManualTokenUsed = client.TotalManualTokenUsed,
                    TotalManualTokenSize = client.TotalManualTokenSize,
                    TotalUsageManualTokensSum = client.TotalUsageManualTokensSum,
                    TotalPurchasedManualTokensSum = client.TotalPurchasedManualTokensSum,
                    ManualTokenFromOrganization = client.ManualTokenFromOrganization,
                    ManualTokenOfUnit = client.ManualTokenOfUnit,
                    IsManualTokenApplied = client.IsManualTokenApplied
                } : null;

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
                return await Task.FromResult(new DepartmentSubscriptionResponse());
            }
        }

        public void IncrementSubscriptionTokenUsage(DepartmentSubscription payload, double IncToken)
        {
            try
            {
                if (payload != null)
                {
                    var collection = _mongoClientRepository.GetCollection(nameof(DepartmentSubscription));
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", payload.ItemId);

                    var update = Builders<BsonDocument>.Update.Inc(nameof(DepartmentSubscription.TotalTokenUsed), IncToken);
                    _ = collection.UpdateOne(filter, update);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public void IncrementSubscriptionManualTokenUsage(double deptToken)
        {
            try
            {
                string praxisClientId = GetUserClientId();

                var existingDeptSubs = GetDepartmentSubscriptionAsync(praxisClientId).GetAwaiter().GetResult();

                if (existingDeptSubs != null)
                {
                    var IncToken = existingDeptSubs.TotalManualTokenUsed + deptToken;

                    var updateFields = new Dictionary<string, object>
                    {
                        { nameof(DepartmentSubscription.TotalManualTokenUsed), IncToken }
                    };

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", existingDeptSubs.ItemId);

                    _changeLogService.UpdateChange(nameof(DepartmentSubscription), filter, updateFields).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }


        public async Task<DepartmentSubscription> GetDepartmentSubscriptionAsync(string praxisClientId)
        {
            return await _repository.GetItemAsync<DepartmentSubscription>(
               ds => ds.PraxisClientId == praxisClientId
           );
        }

        public async Task SaveDepartmentSubscription(string clientId, PraxisClientSubscription clientSubs)
        {
            var client = await _repository.GetItemAsync<PraxisClient>(c => c.ItemId == clientId);

            var tokensInMillion = 1_000_000;
            var bytesInGigabyte = Math.Pow(1024, 3);

            var totalTokenSize = (clientSubs?.TokenSubscription?.IncludedTokenInMillion ?? 0) + (clientSubs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
            totalTokenSize *= tokensInMillion;
            var tokenFromOrganization = clientSubs?.TokenSubscription?.IncludedTokenInMillion ?? 0;
            tokenFromOrganization *= tokensInMillion;
            var tokenOfUnit = clientSubs?.TokenSubscription?.TotalAdditionalTokenInMillion ?? 0;
            tokenOfUnit *= tokensInMillion;

            var totalManualTokenSize = (clientSubs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0) + (clientSubs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0);
            totalManualTokenSize *= tokensInMillion;
            var manualTokenFromOrganization = clientSubs?.ManualTokenSubscription?.IncludedTokenInMillion ?? 0;
            manualTokenFromOrganization *= tokensInMillion;
            var manualTokenOfUnit = clientSubs?.ManualTokenSubscription?.TotalAdditionalTokenInMillion ?? 0;
            manualTokenOfUnit *= tokensInMillion;

            var totalStorageSize = (clientSubs?.StorageSubscription?.IncludedStorageInGigaBites ?? 0) + (clientSubs?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0);
            totalStorageSize *= bytesInGigabyte;
            var storageFromOrganization = clientSubs?.StorageSubscription?.IncludedStorageInGigaBites ?? 0;
            storageFromOrganization *= bytesInGigabyte;
            var storageOfUnit = clientSubs?.StorageSubscription?.TotalAdditionalStorageInGigaBites ?? 0;
            storageOfUnit *= bytesInGigabyte;

            if (client != null)
            {
                var deptSubscription = await GetDepartmentSubscriptionAsync(clientId);
                if (deptSubscription == null)
                {
                    deptSubscription = new DepartmentSubscription()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        PraxisClientId = clientId,
                        TotalTokenUsed = 0,
                        TotalTokenSize = totalTokenSize,
                        TotalStorageUsed = 0,
                        TotalStorageSize = totalStorageSize,
                        TokenFromOrganization = tokenFromOrganization,
                        StorageFromOrganization = storageFromOrganization,
                        TokenOfUnit = tokenOfUnit,
                        StorageOfUnit = storageOfUnit,
                        TotalUsageTokensSum = 0,
                        TotalPurchasedTokensSum = 0,
                        SubscriptionDate = clientSubs.SubscriptionDate,
                        SubscriptionExpirationDate = clientSubs.SubscriptionExpirationDate,
                        IsTokenApplied = clientSubs.IsTokenApplied,
                        TotalManualTokenUsed = 0,
                        TotalManualTokenSize = totalManualTokenSize,
                        TotalPurchasedManualTokensSum = 0,
                        TotalUsageManualTokensSum = 0,
                        ManualTokenFromOrganization = manualTokenFromOrganization,
                        ManualTokenOfUnit = manualTokenOfUnit,
                        IsManualTokenApplied = clientSubs.IsManualTokenApplied
                    };
                    await _repository.SaveAsync(deptSubscription);
                }
                else
                {
                    deptSubscription.TotalTokenSize = totalTokenSize;
                    deptSubscription.TokenFromOrganization = tokenFromOrganization;
                    deptSubscription.TokenOfUnit = tokenOfUnit;
                    deptSubscription.TotalStorageSize = totalStorageSize;
                    deptSubscription.StorageFromOrganization = storageFromOrganization;
                    deptSubscription.StorageOfUnit = storageOfUnit;
                    deptSubscription.SubscriptionDate = clientSubs.SubscriptionDate;
                    deptSubscription.SubscriptionExpirationDate = clientSubs.SubscriptionExpirationDate;
                    deptSubscription.IsTokenApplied = clientSubs.IsTokenApplied;
                    deptSubscription.TotalManualTokenSize = totalManualTokenSize;
                    deptSubscription.ManualTokenFromOrganization = manualTokenFromOrganization;
                    deptSubscription.ManualTokenOfUnit = manualTokenOfUnit;
                    deptSubscription.IsManualTokenApplied = clientSubs.IsManualTokenApplied;

                    await _repository.UpdateAsync(s => s.ItemId == deptSubscription.ItemId, deptSubscription);
                }
            }

        }

        public async Task<bool> IncrementDepartmentSubscriptionStorageUsage(ObjectArtifact objectArtifact)
        {
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentIdForSubscription(objectArtifact.MetaData);
            var departmentSubscription = await GetDepartmentSubscriptionAsync(departmentId);

            if (!string.IsNullOrWhiteSpace(departmentId) && departmentSubscription != null)
            {
                var totalStorageUsed = departmentSubscription.TotalStorageUsed + objectArtifact.FileSizeInByte;
                var updateFields = new Dictionary<string, object>
                    {
                        { nameof(DepartmentSubscription.TotalStorageUsed), totalStorageUsed },
                    };

                var filter = Builders<BsonDocument>.Filter.Eq("_id", departmentSubscription.ItemId);

                await _changeLogService.UpdateChange(nameof(DepartmentSubscription), filter, updateFields);
            }

            return true;
        }

        public async Task<bool> DeleteStorageFromDepartmentSubscriptionAsync(string praxisClientId, double fileSizeInBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(praxisClientId)) return false;

                var deptSubscription = await GetDepartmentSubscriptionAsync(praxisClientId);

                if (deptSubscription != null)
                {
                    var updatedTotalStorage = Math.Max(0, deptSubscription.TotalStorageUsed - fileSizeInBytes);

                    var updates = new Dictionary<string, object>
                    {
                        { "TotalStorageUsed", updatedTotalStorage }
                    };

                    await _repository.UpdateAsync<DepartmentSubscription>(ds => ds.ItemId == deptSubscription.ItemId, updates);

                    _logger.LogInformation("Updated previous departmentId subscription from latest for departmentId: {PraxisClientId}", praxisClientId);
                }
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

        public async Task UpdateTokenBalanceOnSubscriptionExpiryAsync(string clientId)
        {
            try
            {
                var deptSubscription = await GetDepartmentSubscriptionAsync(clientId);

                if (deptSubscription != null)
                {
                    var updateFields = new Dictionary<string, object>
                    {
                        { nameof(DepartmentSubscription.TotalTokenUsed), 0 },
                        { nameof(DepartmentSubscription.TotalUsageTokensSum), deptSubscription.TotalUsageTokensSum + deptSubscription.TotalTokenUsed },
                        { nameof(DepartmentSubscription.TotalTokenSize), 0 },
                        { nameof(DepartmentSubscription.TotalPurchasedTokensSum), deptSubscription.TotalPurchasedTokensSum + deptSubscription.TotalTokenSize }
                    };

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", deptSubscription.ItemId);

                    await _changeLogService.UpdateChange(nameof(DepartmentSubscription), filter, updateFields);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public async Task<CheckValidUploadFileRequestResponse> GetValidUploadFileRequestInDepartmentSubscription(GetValidFileUploadRequestQuery query)
        {
            var result = new CheckValidUploadFileRequestResponse();
            if (string.IsNullOrEmpty(query.PraxisClientId))
            {
                query.PraxisClientId = GetUserClientId();
            }

            var deptSubscription = await GetDepartmentSubscriptionAsync(query.PraxisClientId);

            if (deptSubscription == null)
            {
                result.IsValid = false;
                result.PraxisClientId = query.PraxisClientId;
                return result;
            }

            if (query.FileSizeInBytes + deptSubscription.TotalStorageUsed > deptSubscription.TotalStorageSize)
            {
                result.IsValid = false;
                result.PraxisClientId = query.PraxisClientId;
                return result;
            }

            result.IsValid = !string.IsNullOrWhiteSpace(query.PraxisClientId);
            result.PraxisClientId = query.PraxisClientId;
            return result;
        }

        public async Task<bool> CheckSubscriptionTokenLimit()
        {
            if (_securityHelperService.IsAAdmin()) { return true; }

            string praxisClientId = GetUserClientId();

            if (string.IsNullOrEmpty(praxisClientId)) { return false; }

            var deptSubscription = await GetDepartmentSubscriptionAsync(praxisClientId);

            if (deptSubscription == null)
            {
                return false;
            }

            bool tokenLimitExits = deptSubscription.TotalManualTokenUsed < deptSubscription.TotalManualTokenSize;

            return tokenLimitExits;
        }

        public string GetUserClientId()
        {
            if (_securityHelperService.IsAAdmin()) return string.Empty;

            var userId = _securityContextProvider.GetSecurityContext().UserId;

            if (string.IsNullOrEmpty(userId)) return string.Empty;

            var primaryClient = _repository.GetItem<PraxisUser>(pu => pu.UserId == userId)?
                .ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
            var deptId = primaryClient?.ClientId ?? string.Empty;

            if (_securityHelperService.IsADepartmentLevelUser())
            {
                deptId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() ?? deptId;
            }

            return deptId;
        }
    }
}
