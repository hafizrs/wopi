using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb
{
    public interface IMongoClientRepository
    {
        IMongoCollection<T> GetCollection<T>();
        IMongoCollection<BsonDocument> GetCollection(string entityName);
        Task Insert(string entity, object primaryEntity);
        Task Update(string entityName, string itemId, Dictionary<string, object> updates);
        Task Delete(string entity, object entityPayload);
        EntityBase GetEntityDetials<T>(string itemId, bool useImpersonation = true);
        EntityBase GetEntityDetials(Type type, string entityName, string itemId, bool useImpersonation = true);
        EntityBase GetEntityDetials<T>(string itemId, List<string> tagNames, bool useImpersonation = true);
        EntityBase GetEntityDetials(Type type, string entityName, string itemId, List<string> tagNames, bool useImpersonation = true);
    }
}
