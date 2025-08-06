using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using SeliseBlocks.GraphQL.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetEntityQueryHandler : IQueryHandler<GetEntityQuery, QueryHandlerResponse>
    {
        private readonly ICommonUtilService _commonUtilService;
        private readonly IAssemblyLoaderService _assemblyLoaderService;
        private readonly ILogger<GetEntityQueryHandler> _logger;
        public GetEntityQueryHandler(
            ICommonUtilService commonUtilService,
            IAssemblyLoaderService assemblyLoaderService,
            ILogger<GetEntityQueryHandler> logger
        )
        {
            _commonUtilService = commonUtilService;
            _assemblyLoaderService = assemblyLoaderService;
            _logger = logger;
        }

        public QueryHandlerResponse Handle(GetEntityQuery query)
        {
            throw new System.NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetEntityQuery query)
        {
            var response = await GetEntityQueryResponse(query);
            return response;
        }

        private async Task<QueryHandlerResponse>  GetEntityQueryResponse(GetEntityQuery query)
        {
            var queryResponse = await _commonUtilService.GetEntityQueryResponse<dynamic>(
               query.Filter ?? "{}",
               query.Sort,
               query.EntityName + 's',
               true,
               query.PageNumber ?? 0,
               query.PageSize ?? 10,
               !(query.ShowDeleted ?? false),
               false
           );

            return new QueryHandlerResponse
            {
                Results = queryResponse?.Results,
                TotalCount = queryResponse.TotalRecordCount,
                ErrorMessage = queryResponse.ErrorMessage,
                StatusCode = queryResponse.StatusCode
            };
        }

        private EntityBase ParseToEntityBase(string entityName, object entityObject)
        {
            try
            {
                var entityType = _assemblyLoaderService.GetEntityType(entityName);

                if (entityType == null)
                {
                    _logger.LogInformation("Entity Assembly {EntityName} not found", nameof(entityName));

                    return null;
                }

                var json = JObject.FromObject(entityObject);
                json["ItemId"] = json["_id"];
                return (EntityBase)json.ToObject(entityType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in {HandlerName} at method: {MethodName}. Error Message: {Message}. Error Details: {StackTrace}",
                    nameof(GetEntityQueryHandler), nameof(ParseToEntityBase), e.Message, e.StackTrace);

                return null;
            }
        }
    }
}