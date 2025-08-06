using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.Services
{
    public class ChangeLogService : IChangeLogService
    {
        private readonly ILogger<ChangeLogService> _logger;
        private readonly IBlocksMongoDbDataContextProvider ecapRepository;

        public ChangeLogService(ILogger<ChangeLogService> logger,
            IBlocksMongoDbDataContextProvider ecapRepository)
        {
            _logger = logger;
            this.ecapRepository = ecapRepository;
        }

        public async Task<bool> UpdateChange(string entityName, FilterDefinition<BsonDocument> dataFilters, Dictionary<string, object> updates)
        {
            try
            {
                var updateDocument = new BsonDocument(allowDuplicateNames: true);

                foreach (var update in updates)
                {
                    BsonDocument change;

                    if (update.Value == null)
                    {
                        change = new BsonDocument(update.Key, BsonValue.Create(null));
                    }
                    else
                    {
                        var type = update.Value.GetType();
                        var typename = update.Value.GetType().Namespace;

                        var isarray = type.IsArray;

                        if (typename != null && !typename.ToLower().StartsWith("system"))
                        {
                            if (isarray)
                            {
                                var jsonDoc = JsonConvert.SerializeObject(update.Value);
                                var bsonDoc = BsonSerializer.Deserialize<BsonValue>(jsonDoc);
                                change = new BsonDocument(update.Key, bsonDoc);
                            }
                            else
                            {
                                var jsonDoc = JsonConvert.SerializeObject(update.Value);
                                var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
                                change = new BsonDocument(update.Key, bsonDoc);
                            }
                        }
                        else if (typename != null && typename == "System.Collections.Generic")
                        {
                            if (type.Name.ToLower().StartsWith("list"))
                            {
                                var jsonDoc = JsonConvert.SerializeObject(update.Value);
                                var bsonDoc = BsonSerializer.Deserialize<BsonArray>(jsonDoc);
                                change = new BsonDocument(update.Key, bsonDoc);
                            }
                            else
                            {
                                var jsonDoc = JsonConvert.SerializeObject(update.Value);
                                var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
                                change = new BsonDocument(update.Key, bsonDoc);
                            }
                        }
                        else
                        {
                            change = new BsonDocument(update.Key, BsonValue.Create(update.Value));
                        }
                    }

                    updateDocument.Add("$set", change);
                }

                var collection = ecapRepository.GetTenantDataContext().GetCollection<BsonDocument>($"{entityName}s");

                var updateResult = await collection.UpdateManyAsync(dataFilters, updateDocument);

                var updateString = string.Join(Environment.NewLine, updates.Select(update => "{" + update.Key + ": " + JsonConvert.SerializeObject(update.Value) + "}"));

                if (updateResult.ModifiedCount > 0)
                {
                    _logger.LogInformation("Applied updates: {UpdateString} -> to entity: {EntityName}.", updateString, entityName);
                    return true;
                }

                _logger.LogWarning("No changes applied to the entity: {EntityName} with updates: {UpdateString}.", entityName, updateString);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while updating {EntityName} with updates: {UpdatesJson}. Exception: {ExceptionMessage}.",
                    entityName, JsonConvert.SerializeObject(updates), ex.Message);

                return false;
            }
        }
    }
}
