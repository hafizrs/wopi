using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices
{
    public interface ICommonUtilService
    {
        Task<EntityQueryResponse<T>> GetEntityQueryResponse<T>(
            string filter, 
            string sort = "{_id: 1}",
            string collectionName = "", 
            bool usePagination = false, 
            int pageNumber = 1, 
            int pageSize = 100, 
            bool hideDeleted = true,
            bool useImpersonation = true,
            List<BsonElement> groupingElements = null,
            List<PipelineStageDefinition<BsonDocument, BsonDocument>> additionalStages = null
        );
        int GenerateRandomInvoiceId();

        void UpdateMany<T>(IEnumerable<T> data) where T:EntityBase;
    }
}