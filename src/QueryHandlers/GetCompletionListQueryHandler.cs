using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetCompletionListQueryHandler : IQueryHandler<GetCompletionListQuery, QueryHandlerResponse>
    {
        private readonly IPraxisOpenItemService praxisOpenItemService;

        public GetCompletionListQueryHandler(IPraxisOpenItemService praxisOpenItemService)
        {
            this.praxisOpenItemService = praxisOpenItemService;
        }

        public QueryHandlerResponse Handle(GetCompletionListQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetCompletionListQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            
            var result = await praxisOpenItemService.GetOpenItemCompletionDetails(query);
            return new QueryHandlerResponse
            {
                Results = result,
                TotalCount = 1
            };
        }
    }
}