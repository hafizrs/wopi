using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class ValidateQuickTaskInfoQueryHandler : IQueryHandler<ValidateQuickTaskInfo, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _quickTaskService;
        public ValidateQuickTaskInfoQueryHandler(IQuickTaskService quickTaskService)
        {
            _quickTaskService = quickTaskService;
        }

        public QueryHandlerResponse Handle(ValidateQuickTaskInfo query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new ValidateQuickTaskInfoResponse
            {
                IsQuickTaskInfoValid = _quickTaskService.ValidateQuickTaskInfo(query)
            };

            return new QueryHandlerResponse
            {
                Data = response,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(ValidateQuickTaskInfo query)
        {
            throw new NotImplementedException();
        }
    }
} 