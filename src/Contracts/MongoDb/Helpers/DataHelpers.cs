using MongoDB.Bson;
using MongoDB.Driver;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Helpers
{
    public class DataHelpers
    {
        public static FilterDefinition<BsonDocument> X(string entityId)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", entityId);
        }

        public static FilterDefinition<BsonDocument> CreateDocumentIdFilter(string entityId)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", entityId);
        }

        public static FilterDefinition<BsonDocument> CreateDocumentIdFilter(object entityObject)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", GetObjectId(entityObject));
        }

        public static string GetObjectId(object @object)
        {
            return (string)@object.GetType().GetProperty("ItemId")?.GetValue(@object);
        }
    }
}
