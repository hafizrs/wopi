using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetUilmResourceKeysQueryHandler : IQueryHandler<GetUilmResourceKeysQuery, QueryHandlerResponse>
    {
        private readonly IUilmResourceKeyService _uilmResourceKeyService;

        public GetUilmResourceKeysQueryHandler(IUilmResourceKeyService uilmResourceKeyService)
        {
            _uilmResourceKeyService = uilmResourceKeyService;
        }

        public QueryHandlerResponse Handle(GetUilmResourceKeysQuery query)
        {
            var response = new QueryHandlerResponse();
            try
            {
                var resourceKeys = _uilmResourceKeyService.GetUilmResourceKeys(query.KeyNameList, query.AppIds);
                response.Results = resourceKeys;
                response.TotalCount = resourceKeys.Count;
            }
            catch (Exception e)
            {
                response.StatusCode = 1;
                response.ErrorMessage = e.StackTrace;
            }

            return response;
        }

        public Task<QueryHandlerResponse> HandleAsync(GetUilmResourceKeysQuery query)
        {
            return Task.FromResult(Handle(query));
        }
    }
}