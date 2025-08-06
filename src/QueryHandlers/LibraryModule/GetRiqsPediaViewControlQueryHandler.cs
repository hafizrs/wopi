using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetRiqsPediaViewControlQueryHandler : IQueryHandler<GetRiqsPediaViewControlQuery, QueryHandlerResponse>
    {
        private readonly IRiqsPediaViewControlService _service;

        public GetRiqsPediaViewControlQueryHandler(IRiqsPediaViewControlService service)
        {
            _service = service;
        }

        public QueryHandlerResponse Handle(GetRiqsPediaViewControlQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetRiqsPediaViewControlQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var results = await _service.GetRiqsPediaViewControl();

            return new QueryHandlerResponse
            {
                Results = results,
                TotalCount = 0
            };
        }
    }
}