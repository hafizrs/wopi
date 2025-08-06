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
    public class GetEquipmentReportPermissionsQueryHandler : IQueryHandler<GetEquipmentReportPermissionsQuery, EntityQueryResponse<EquipmentReportPermissionRecord>>
    {
        private readonly ILogger<GetEquipmentReportPermissionsQueryHandler> _logger;
        private readonly IReportTemplatePermissionService _reportTemplatePermissionService;

        public GetEquipmentReportPermissionsQueryHandler(
            ILogger<GetEquipmentReportPermissionsQueryHandler> logger,
            IReportTemplatePermissionService reportTemplatePermissionService)
        {
            _logger = logger;
            _reportTemplatePermissionService = reportTemplatePermissionService;
        }
        public EntityQueryResponse<EquipmentReportPermissionRecord> Handle(GetEquipmentReportPermissionsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<EquipmentReportPermissionRecord>> HandleAsync(GetEquipmentReportPermissionsQuery query)
        {
            _logger.LogInformation("Entered {HandlerName} with query: {Query}", nameof(GetEquipmentReportPermissionsQueryHandler), query);
            var response = new EntityQueryResponse<EquipmentReportPermissionRecord>();
            try
            {
                var result = await _reportTemplatePermissionService.EquipmentReportPermissions(query.ClientId, query.OrganizationId, query.EquipmentId);
                response.Results = new List<EquipmentReportPermissionRecord> { result };
                response.TotalRecordCount = 1;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occurred in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetEquipmentReportPermissionsQueryHandler), e.Message, e.StackTrace);
                response.ErrorMessage = e.Message;
            }
            return response;
        }
    }
}
