using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Helpers;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.MongoDb
{
    public class MongoClientRepository : IMongoClientRepository
    {
        private readonly ILogger<MongoClientRepository> ecapLogger;
        private readonly ISecurityContextProvider securityDataProvider;
        private readonly IBlocksMongoDbDataContextProvider ecapRepository;

        public MongoClientRepository(
            ILogger<MongoClientRepository> ecapLogger,
            IBlocksMongoDbDataContextProvider ecapRepository,
            ISecurityContextProvider securityDataProvider)
        {
            this.ecapLogger = ecapLogger;
            this.ecapRepository = ecapRepository;
            this.securityDataProvider = securityDataProvider;
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            string name = typeof(T).Name;
            try
            {
                return ecapRepository.GetTenantDataContext().GetCollection<T>(name + "s");
            }
            catch (Exception ex)
            {
               ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository ::  GetCollection<T> -> entity  Message {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public IMongoCollection<BsonDocument> GetCollection(string entityName)
        {
            try
            {
                return ecapRepository.GetTenantDataContext().GetCollection<BsonDocument>(entityName + "s");
            }
            catch (Exception ex)
            {
               ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository :: GetCollection -> entity {EntityName} Message {ErrorMessage}", entityName, ex.Message);
            }

            return null;
        }

        public Task Insert(string entity, object primaryEntity)
        {
            try
            {
                return GetCollection(entity).InsertOneAsync(primaryEntity.ToBsonDocument());
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository :: Insert -> entity {EntityName} Message {ErrorMessage}", entity, ex.Message);
            }

            return Task.CompletedTask;
        }

        public Task Update(string entityName, string itemId, Dictionary<string, object> updates)
        {
            try
            {
                string value = string.Empty;
                SecurityContext securityContext = securityDataProvider.GetSecurityContext();
                if (string.IsNullOrEmpty(securityContext.UserId))
                {
                    value = securityContext.UserId.ToString();
                }

                updates.Add("LastUpdateDate", DateTime.UtcNow);
                updates.Add("LastUpdatedBy", value);
                BsonDocument bsonDocument = new BsonDocument(allowDuplicateNames: true);
                foreach (KeyValuePair<string, object> update in updates)
                {
                    BsonDocument value2 = new BsonDocument(update.Key, BsonValue.Create(update.Value));
                    bsonDocument.Add("$set", value2);
                }

                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", itemId);
                return GetCollection(entityName).UpdateOneAsync(filter, bsonDocument);
            }
            catch (Exception ex)
            {
              ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository - Update :: entityName-> {EntityName} itemId -> {ItemId} updates -> {Updates}", entityName, itemId, LogHelpers.JsonToString(updates));
            }

             return Task.CompletedTask;
        }

        public Task Delete(string entity, object entityPayload)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = DataHelpers.CreateDocumentIdFilter(entityPayload);
                return GetCollection(entity).DeleteOneAsync(filter);
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository :: Delete -> entity {EntityName} Message {ErrorMessage}", entity, ex.Message);
            }

             return Task.CompletedTask;
        }

        public EntityBase GetEntityDetials<T>(string itemId, bool useImpersonation = true)
        {
            return GetEntityDetialsData(typeof(T), typeof(T).Name, itemId, null, useImpersonation);
        }

        public EntityBase GetEntityDetials(Type type, string entityName, string itemId, bool useImpersonation = true)
        {
            return GetEntityDetialsData(type, entityName, itemId, null, useImpersonation);
        }

        public EntityBase GetEntityDetials<T>(string itemId, List<string> tagNames, bool useImpersonation = true)
        {
            return GetEntityDetialsData(typeof(T), typeof(T).Name, itemId, tagNames, useImpersonation);
        }

        public EntityBase GetEntityDetials(Type type, string entityName, string itemId, List<string> tagNames, bool useImpersonation = true)
        {
            return GetEntityDetialsData(type, entityName, itemId, tagNames, useImpersonation);
        }

        private EntityBase GetEntityDetialsData(Type type, string entityName, string itemId, List<string> tagNames = null, bool useImpersonation = true)
        {
            try
            {
                FilterDefinitionBuilder<BsonDocument> filter = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter2 = filter.Eq("_id", itemId);
                if (tagNames != null && tagNames.Count > 0)
                {
                    filter2 &= filter.In("Tags", tagNames);
                }

                if (!useImpersonation)
                {
                    SecurityContext securityContext = securityDataProvider.GetSecurityContext();
                    string userId = securityContext.UserId;
                    IEnumerable<string> roles = securityContext.Roles;
                    if (!string.IsNullOrEmpty(userId.ToString()) && roles != null)
                    {
                        filter2 &= filter.In("IdsAllowedToRead", new string[1] { userId.ToString() }) | filter.In("RolesAllowedToRead", roles);
                    }
                    else if (!string.IsNullOrEmpty(userId.ToString()))
                    {
                        filter2 &= filter.In("IdsAllowedToRead", new string[1] { userId.ToString() });
                    }
                    else if (roles != null)
                    {
                        filter2 &= filter.In("RolesAllowedToRead", roles);
                    }
                }

                BsonDocument bsonDocument = GetCollection(entityName).Find(filter2).FirstOrDefault();
                if (bsonDocument == null || type == null)
                {
                    return null;
                }

                object obj = BsonSerializer.Deserialize(bsonDocument, type);
                return (EntityBase)obj;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation( "[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoClientRepository GetEntityDetialsData :: entityName {EntityName}, itemId {ItemId}, tagNames {TagNames}", entityName, itemId, tagNames);
            }

            return null;
        }
    }
}
