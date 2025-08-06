using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class ValidateShiftInfoQueryHandler : IQueryHandler<ValidateShiftInfo, QueryHandlerResponse>
    {
        private readonly IPraxisShiftService _praxisShiftService;
        public ValidateShiftInfoQueryHandler(IPraxisShiftService praxisShiftService)
        {
            _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(ValidateShiftInfo query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var response = new ValidateShiftInfoResponse
            {
                IsShiftInfoValid = _praxisShiftService.ValidateShiftInfo(query)
            };

            return new QueryHandlerResponse
            {
                Data = response,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(ValidateShiftInfo query)
        {
            throw new NotImplementedException();
        }
    }
}
