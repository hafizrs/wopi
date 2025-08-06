using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.CirsReports
{
    class GetFaultReportByEquipmentIdQueryHandler : AbstractQueryHandler<GetFaultReportByEquipmentIdQuery>
    {
        private readonly ICirsReportQueryService _cirsReportQueryService;

        public GetFaultReportByEquipmentIdQueryHandler(ICirsReportQueryService cirsReportQueryService)
        {
            _cirsReportQueryService = cirsReportQueryService;
        }
        public override async Task<QueryHandlerResponse> HandleAsync(GetFaultReportByEquipmentIdQuery Query)
        {
            try
            {
                var reports = await _cirsReportQueryService.GetFaultReportsAsync(Query);
                return new QueryHandlerResponse
                {
                    Data = reports,
                    StatusCode = 0,
                    TotalCount = reports.Count
                };
            }
            catch(Exception ex)
            {
                return new QueryHandlerResponse
                {
                    StatusCode = 1,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
