using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetShiftPlanByIdQueryHandler : IQueryHandler<GetShiftPlanByIdQuery, QueryHandlerResponse>
    {
        private readonly IPraxisShiftService _praxisShiftService;
        public GetShiftPlanByIdQueryHandler(IPraxisShiftService praxisShiftService)
        {
            _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(GetShiftPlanByIdQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var data = _praxisShiftService.GetShiftPlanById(query.ShiftPlanId);

            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetShiftPlanByIdQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
