using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class ValidateShiftPlanInfoQueryHandler : IQueryHandler<ValidateShiftPlanInfoQuery, QueryHandlerResponse>
    {
        private readonly IPraxisShiftService _praxisShiftService;
        public ValidateShiftPlanInfoQueryHandler(IPraxisShiftService praxisShiftService)
        {
            _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(ValidateShiftPlanInfoQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new ValidateShiftPlanInfoResponse
            {
                IsShiftPlanInfoValid = _praxisShiftService.ValidateShiftPlanInfo(query)
            };

            var shifts = _praxisShiftService.ValidateShiftPlanInfo(query);

            return new QueryHandlerResponse
            {
                Data = response,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(ValidateShiftPlanInfoQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
