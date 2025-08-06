using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class GetQuickTaskPlanByIdQueryHandler : IQueryHandler<GetQuickTaskPlanByIdQuery, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _quickTaskService;
        public GetQuickTaskPlanByIdQueryHandler(IQuickTaskService quickTaskService)
        {
            _quickTaskService = quickTaskService;
        }

        public QueryHandlerResponse Handle(GetQuickTaskPlanByIdQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var data = _quickTaskService.GetQuickTaskPlanById(query.QuickTaskPlanId);

            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetQuickTaskPlanByIdQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 