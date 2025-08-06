using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.QuickTaskModule
{
    public class ValidateQuickTaskPlanInfoQueryHandler : IQueryHandler<ValidateQuickTaskPlanInfoQuery, QueryHandlerResponse>
    {
        private readonly IQuickTaskService _quickTaskService;
        public ValidateQuickTaskPlanInfoQueryHandler(IQuickTaskService quickTaskService)
        {
            _quickTaskService = quickTaskService;
        }

        public QueryHandlerResponse Handle(ValidateQuickTaskPlanInfoQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new ValidateQuickTaskPlanInfoResponse
            {
                IsQuickTaskPlanInfoValid = _quickTaskService.ValidateQuickTaskPlanInfo(query)
            };

            return new QueryHandlerResponse
            {
                Data = response,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(ValidateQuickTaskPlanInfoQuery query)
        {
            throw new NotImplementedException();
        }
    }
} 