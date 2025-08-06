using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetShiftsQueryHandler : IQueryHandler<GetShiftQuery, QueryHandlerResponse> 
    { 
        private readonly IPraxisShiftService _praxisShiftService;
        public GetShiftsQueryHandler(IPraxisShiftService praxisShiftService)
        {
        _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(GetShiftQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var shifts = _praxisShiftService.GetShifts(query.DepartmentId);

            return new QueryHandlerResponse
            {
                Data = shifts,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetShiftQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
