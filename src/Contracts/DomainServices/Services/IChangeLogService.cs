using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IChangeLogService
    {
        Task<bool> UpdateChange(string entityName, FilterDefinition<BsonDocument> dataFilters, Dictionary<string, object> updates);
    }
}
