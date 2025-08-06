using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetShiftPlanQueryHandler : IQueryHandler<GetShiftPlanQuery, QueryHandlerResponse>
    {
        private readonly IPraxisShiftService _praxisShiftService;
        public GetShiftPlanQueryHandler(IPraxisShiftService praxisShiftService)
        {
            _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(GetShiftPlanQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var data = _praxisShiftService.GetShiftPlans(query);

            return new QueryHandlerResponse
            {
                Data = data
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetShiftPlanQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
