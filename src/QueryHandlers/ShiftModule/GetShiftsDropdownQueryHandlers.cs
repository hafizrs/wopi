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
    public class GetShiftsDropdownQueryHandlers : IQueryHandler<GetShiftsDropdownQuery, QueryHandlerResponse>
    {
        private readonly IPraxisShiftService _praxisShiftService;
        public GetShiftsDropdownQueryHandlers(IPraxisShiftService praxisShiftService)
        {
            _praxisShiftService = praxisShiftService;
        }

        [Invocable]
        public QueryHandlerResponse Handle(GetShiftsDropdownQuery query)
        {
            if (query == null)
            {
                return new QueryHandlerResponse
                {
                    Results = null,
                    ErrorMessage = "Invalid Query Fields"
                };
            }

            var shifts = _praxisShiftService.GetShiftDropdown(query.DepartmentId);

            return new QueryHandlerResponse
            {
                Data = shifts,
            };
        }

        public Task<QueryHandlerResponse> HandleAsync(GetShiftsDropdownQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
