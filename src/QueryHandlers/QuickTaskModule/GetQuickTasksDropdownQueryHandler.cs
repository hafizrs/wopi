using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class GetQuickTasksDropdownQueryHandler : IQueryHandler<GetQuickTasksDropdownQuery, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _quickTaskService;
        public GetQuickTasksDropdownQueryHandler(IQuickTaskService quickTaskService)
        {
            _quickTaskService = quickTaskService;
        }

        public QueryHandlerResponse Handle(GetQuickTasksDropdownQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var quickTasks = _quickTaskService.GetQuickTaskDropdown(query.DepartmentId);

            return new QueryHandlerResponse
            {
                Data = quickTasks,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetQuickTasksDropdownQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
} 