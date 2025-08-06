using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetDistinctTaskListQueryHandler : IQueryHandler<GetDistinctTaskListQuery, QueryHandlerResponse>
    {
        private readonly IPraxisTaskService praxisTaskService;

        public GetDistinctTaskListQueryHandler(IPraxisTaskService praxisTaskService)
        {
            this.praxisTaskService = praxisTaskService;
        }

        public QueryHandlerResponse Handle(GetDistinctTaskListQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetDistinctTaskListQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var results = await praxisTaskService.GetDistinctTaskListByFilter(query.Filter, query.Sort, query.PageNumber, query.PageSize);

            return new QueryHandlerResponse
            {
                Results = results.Results,
                TotalCount = results.TotalRecordCount
            };
        }
    }
}