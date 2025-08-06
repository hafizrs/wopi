using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices;
using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using SeliseBlocks.GraphQL.Infrastructure;

namespace Selise.Ecap.SC.Wopi.Domain.DomainServices.Services
{
    public class CommonUtilService : ICommonUtilService
    {
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;

        public CommonUtilService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
        }

        public async Task<EntityQueryResponse<T>> GetEntityQueryResponse<T>(
            string filter,
            string sort = "{_id: 1}",
            string collectionName = "",
            bool usePagination = false,
            int pageNumber = 0,
            int pageSize = 100,
            bool hideDeleted = true,
            bool useImpersonation = true,
            List<BsonElement> groupingElements = null,
            List<PipelineStageDefinition<BsonDocument, BsonDocument>> additionalStages = null
        )
        {
            if (string.IsNullOrEmpty(collectionName))
                collectionName = $"{typeof(T).Name}s";
            if (hideDeleted && !filter.Contains("IsMarkedToDelete"))
            {
                if (filter == "{}" || filter == "{ }")
                {
                    filter = "{IsMarkedToDelete: false}";
                }
                else
                {
                    filter = filter.Insert(
                        filter.LastIndexOf("}", StringComparison.InvariantCulture),
                        ", IsMarkedToDelete: false"
                    );
                }
            }

            if (string.IsNullOrEmpty(sort) || sort == "{}" || sort == "{ }")
            {
                sort = "{_id: 1}";
            }
            

            return await Task.Run(
                async () =>
                {
                    FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                    if (!string.IsNullOrEmpty(filter)) queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);

                    var securityContext = _securityContextProvider.GetSecurityContext();

                    if (useImpersonation)
                    {
                        queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                            PdsActionEnum.Read,
                            securityContext,
                            securityContext.Roles.ToList()
                        );
                    }

                    var collection = _mongoDbDataContextProvider
                        .GetTenantDataContext()
                        .GetCollection<BsonDocument>(collectionName)
                        .Aggregate()
                        .Match(queryFilter);
                    
                    if (additionalStages?.Any() == true)
                    {
                        foreach (var stage in additionalStages)
                        {
                            collection = collection.AppendStage(stage);
                        }
                    }

                    if (groupingElements != null)
                    {
                        collection = collection.Group(new BsonDocument(groupingElements));
                    }


                    var dataFacetStages = new BsonArray
                    {
                        new BsonDocument { { "$sort", BsonDocument.Parse(sort) } }
                    };

                    var facetBody = new BsonDocument
                    {
                        { "data", dataFacetStages }
                    };

                    if (usePagination && pageSize > 0)
                    {
                        int skip = pageNumber * pageSize;
                        dataFacetStages.Add(new BsonDocument { { "$skip", skip } });
                        dataFacetStages.Add(new BsonDocument { { "$limit", pageSize } });
                        facetBody.Add("metadata", new BsonArray
                        {
                            new BsonDocument { { "$count", "count" } }
                        });
                    }

                    var facetDoc = new BsonDocument
                    {
                        { "$facet", facetBody }
                    };


                    var pipeline = collection.AppendStage<BsonDocument>(facetDoc);

                    var facetResult = await pipeline.FirstOrDefaultAsync();

                    var resultDocs = facetResult["data"]
                        .AsBsonArray
                        .Select(doc => BsonSerializer.Deserialize<T>(doc.AsBsonDocument));

                    int totalCount = usePagination && facetResult.Contains("metadata") ?
                                        (facetResult["metadata"].AsBsonArray.FirstOrDefault()?["count"].AsInt32 ?? 0)
                                        : (resultDocs?.Count() ?? 0);

                    return new EntityQueryResponse<T>
                    {
                        Results = resultDocs,
                        TotalRecordCount = totalCount
                    };

                }
            );
        }

        public int GenerateRandomInvoiceId()
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }

        public void UpdateMany<T>(IEnumerable<T> data) where T : EntityBase
        {
            foreach (var datum in data)
            {
                _repository.Update(d => d.ItemId.Equals(datum.ItemId), datum);
            }
        }
    }
}