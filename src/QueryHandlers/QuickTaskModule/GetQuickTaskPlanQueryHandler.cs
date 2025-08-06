using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class GetQuickTaskPlanQueryHandler : IQueryHandler<GetQuickTaskPlanQuery, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _service;
        public GetQuickTaskPlanQueryHandler(IQuickTaskService service)
        {
            _service = service;
        }
        public QueryHandlerResponse Handle(GetQuickTaskPlanQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            var data = _service.GetQuickTaskPlans(query);
            return new QueryHandlerResponse
            {
                Data = data
            };
        }
        public Task<QueryHandlerResponse> HandleAsync(GetQuickTaskPlanQuery query)
        {
            throw new NotImplementedException();
        }
    }
} 