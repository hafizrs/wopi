using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ConfiguratorModule
{
    public class GetReportTemplatePermissionsQueryHandler : IQueryHandler<GetReportTemplatePermissionsQuery, EntityQueryResponse<ReportTemplatePermissionRecord>>
    {
        private readonly ILogger<GetReportTemplatePermissionsQueryHandler> _logger;
        private readonly IReportTemplatePermissionService _reportTemplatePermissionService;

        public GetReportTemplatePermissionsQueryHandler(
            ILogger<GetReportTemplatePermissionsQueryHandler> logger,
            IReportTemplatePermissionService reportTemplatePermissionService)
        {
            _logger = logger;
            _reportTemplatePermissionService = reportTemplatePermissionService;
        }
        public EntityQueryResponse<ReportTemplatePermissionRecord> Handle(GetReportTemplatePermissionsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<ReportTemplatePermissionRecord>> HandleAsync(GetReportTemplatePermissionsQuery query)
        {
            _logger.LogInformation("Entered {HandlerName} with query: {Query}", nameof(GetReportTemplatePermissionsQueryHandler), query);
            var response = new EntityQueryResponse<ReportTemplatePermissionRecord>();
            try
            {
                var result = await _reportTemplatePermissionService.ReportTemplatePermissions(query.ClientId, query.OrganizationId);
                response.Results = new List<ReportTemplatePermissionRecord> { result };
                response.TotalRecordCount = 1;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occurred in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetReportTemplatePermissionsQueryHandler), e.Message, e.StackTrace);
                response.ErrorMessage = e.Message;
            }
            return response;
        }
    }
}
