using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using RabbitMQ.Client.Impl;
using Selise.Ecap.Entities.PrimaryEntities.AppCatalogue;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.StorageService;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using SkiaSharp;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class UilmResourceKeyService : IUilmResourceKeyService
    {
        private readonly IRepository _repository;
        private readonly ILogger<UilmResourceKeyService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly PraxisFileService _blocksFileService;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly IStorageDataService _storageDataService;

        public UilmResourceKeyService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<UilmResourceKeyService> logger,
            PraxisFileServiceFactory praxisFileServiceFactory,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IStorageDataService storageDataService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
            _blocksFileService = praxisFileServiceFactory.Create(true);
            _ecapRepository = ecapRepository;
            _storageDataService = storageDataService;
        }

        public void InsertResourceKey(UilmResourceKey uilmResourceKey)
        {
            try
            {
                var resourceKey = _repository.GetItem<UilmResourceKey>(r => r.ItemId.Equals(uilmResourceKey.ItemId));
                if (resourceKey != null)
                {
                    _repository.Update(r => r.ItemId.Equals(uilmResourceKey.ItemId), uilmResourceKey);
                }
                else
                {
                    _repository.Save(uilmResourceKey);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error while inserting resource key with ItemId: {ItemId}, KeyName: {KeyName}. Message: {ErrorMessage}, Stacktrace: {StackTrace}",
                    uilmResourceKey.ItemId, uilmResourceKey.KeyName, e.Message, e.StackTrace);
            }
        }

        public async Task InsertResourceKeys(List<UilmResourceKey> resourceKeys)
        {
            try
            {
                foreach (var uilmResourceKey in resourceKeys)
                {
                    var resourceKeyExists =
                        await _repository.ExistsAsync<UilmResourceKey>(r => r.ItemId.Equals(uilmResourceKey.ItemId));
                    _logger.LogInformation("{Operation} the UilmResourceKey with KeyName: {KeyName} and ItemId: {ItemId}",
                        resourceKeyExists ? "Updating" : "Saving", uilmResourceKey.KeyName, uilmResourceKey.ItemId);

                    if (resourceKeyExists)
                    {
                        await _repository.UpdateAsync(r => r.ItemId.Equals(uilmResourceKey.ItemId), uilmResourceKey);
                    }
                    else
                    {
                        await _repository.SaveAsync(uilmResourceKey);
                    }
                }

                FixUilmApplicationsKeyCount();
            }
            catch (Exception e)
            {
                _logger.LogError("Error while inserting resource keys. Message: {ErrorMessage}, Stacktrace: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        public void FixUilmApplicationsKeyCount()
        {
            var uilmApplications = _repository.GetItems<UilmApplication>().ToList();
            foreach (var uilmApplication in uilmApplications)
            {
                var keyCount = _repository.GetItems<UilmResourceKey>(r => r.AppId.Equals(uilmApplication.ItemId))
                    .Count();

                if (keyCount == uilmApplication.NumberOfKeys) continue;

                uilmApplication.NumberOfKeys = keyCount;
                _repository.Update(app => app.ItemId.Equals(uilmApplication.ItemId), uilmApplication);
            }
        }

        public string GetResourceValueByKeyName(string keyName, string language = null)
        {
            try
            {
                var langKey = GetLangKey(language);
                if (keyName.Length > 0)
                {
                    var uilmResourceKey = _repository.GetItem<UilmResourceKey>(o => o.KeyName.Equals(keyName));
                    var resource = uilmResourceKey?.Resources.FirstOrDefault(o => o.Culture.Equals(langKey));
                    if (resource != null)
                        return resource.Value;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in UilmResourceKeyService: {ErrorMessage}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return keyName;
        }

        public Dictionary<string, string> GetResourceValueByKeyName(List<string> keyList, string language = null)
        {
            try
            {
                var langKey = GetLangKey(language);
                if (keyList.Count > 0)
                {
                    keyList = keyList.Distinct().ToList();
                    var uilmResourceKeyList = _repository
                        .GetItems<UilmResourceKey>(resourceKey => keyList.Contains(resourceKey.KeyName))
                        .ToList();

                    return keyList.ToDictionary(
                        key => key,
                        key => uilmResourceKeyList.FirstOrDefault(resourceKey => key.Equals(resourceKey.KeyName))
                            ?.Resources.FirstOrDefault(o => o.Culture.Equals(langKey))
                            ?.Value ?? key
                    );
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in UilmResourceKeyService: {ErrorMessage}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return new Dictionary<string, string>() { };
        }

        public List<UilmResourceKey> GetUilmResourceKeys(List<string> keyNameList, List<string> appIds = null)
        {
            try
            {
                Expression<Func<UilmResourceKey, bool>> filter = key => keyNameList.Contains(key.KeyName);
                if ((appIds ?? new List<string>()).Count != 0)
                {
                    Expression<Func<UilmResourceKey, bool>> appIdFilter = key => appIds.Contains(key.AppId);
                    filter = ExpressionBuilder.AndAlso(filter, appIdFilter);
                }

                var uilmResourceKeys = _repository.GetItems(filter);
                return uilmResourceKeys.Any()
                    ? uilmResourceKeys.OrderBy(key => key.AppId).ToList()
                    : new List<UilmResourceKey>();
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in UilmResourceKeyService. Message: {ErrorMessage}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
                throw;
            }
        }

        private string GetLangKey(string language)
        {
            return (string.IsNullOrEmpty(language) ? _securityContextProvider.GetSecurityContext().Language : language)
                .Split("-")[0];
        }

        public async Task<string> DownloadUilmResourceKeysAsJsonAsync()
        {
            try
            {
                DateTime lastMonthDate = DateTime.UtcNow.AddMonths(-1);

                var pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "$or", new BsonArray
                            {
                                new BsonDocument { { "CreatedDate", new BsonDocument { { "$gte", lastMonthDate } } } },
                                new BsonDocument { { "ModifiedDate", new BsonDocument { { "$gte", lastMonthDate } } } }
                            }
                        }
                    })
                };

                var documents = await _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("UilmResourceKeys")
                    .Aggregate<BsonDocument>(pipeline)
                    .ToListAsync();

                var dataList = documents.Select(doc => BsonSerializer.Deserialize<UilmResourceKey>(doc)).ToList();

                if (dataList != null && dataList.Count() > 0)
                {
                    string jsonData = dataList.ToJson(new JsonWriterSettings { Indent = true });
                    byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
                    string fileName = $"UilmResourceKeys_{lastMonthDate:dd-MM-yyyy}_to_{lastMonthDate.AddMonths(1):dd-MM-yyyy}.json";
                    var fileId = Guid.NewGuid().ToString();
                    var success = await _storageDataService.UploadFileAsync(fileId, fileName, bytes);
                    if(success) return fileId;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in DownloadUlimResourceKeysAsJsonAsync. Message: {ErrorMessage}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return string.Empty;
        }

        public async Task<bool> UpsertUilmResoucekeysFromJsonAsync(string fileId)
        {
            try
            {
                var fileInfo = await _blocksFileService.GetFileInfoFromStorage(fileId);

                var fileUrl = fileInfo?.Url ?? string.Empty;

                var jsonString = await _storageDataService.GetFileContentString(fileUrl);
                jsonString = Regex.Replace(jsonString, @"ISODate\(""([^""]+)""\)", "\"$1\"");

                if (string.IsNullOrEmpty(jsonString)) return false;

                var uilmResourceKeys = JsonSerializer.Deserialize<List<UilmResourceKey>>(jsonString, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("UilmResourceKeys");

                var bulkOperations = uilmResourceKeys.Select(equipment =>
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", equipment.ItemId);
                    var update = equipment.ToBsonDocument();
                    return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                }).ToList();

                if (bulkOperations != null && bulkOperations.Count() > 0)
                {
                    var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);

                    _logger.LogInformation("Upserted {MatchedCount} matched, {ModifiedCount} modified, {InsertedCount} inserted documents.",
                        bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, bulkWriteResult.Upserts.Count);

                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in UpsertUlimResoucekeysFromJson. Message: {ErrorMessage}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return false;
        }
    }
}