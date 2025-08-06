using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class GetQuickTasksQueryHandler : IQueryHandler<GetQuickTasksQuery, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _service;
        public GetQuickTasksQueryHandler(IQuickTaskService service)
        {
            _service = service;
        }
        public QueryHandlerResponse Handle(GetQuickTasksQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }
            var data = _service.GetQuickTasks(query.DepartmentId);
            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetQuickTasksQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 