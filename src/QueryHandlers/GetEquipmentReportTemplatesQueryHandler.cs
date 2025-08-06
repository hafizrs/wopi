using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetEquipmentReportTemplatesQueryHandler : IQueryHandler<GetEquipmentReportTemplatesQuery, EntityQueryResponse<EquipmentReportTemplatesResponse>>
    {
        private readonly ILogger<GetEquipmentReportTemplatesQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetEquipmentReportTemplatesQueryHandler(
            ILogger<GetEquipmentReportTemplatesQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<EquipmentReportTemplatesResponse> Handle(GetEquipmentReportTemplatesQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<EquipmentReportTemplatesResponse>> HandleAsync(GetEquipmentReportTemplatesQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetEquipmentReportTemplatesQueryHandler), query);
            var response = new EntityQueryResponse<EquipmentReportTemplatesResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetEquipmentReportTemplates(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in Handler: {HandlerName}.\nError Message: {Message}.\nDetails: {StackTrace}",
                    nameof(GetEquipmentReportTemplatesQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = ex.Message;
                response.StatusCode = 1;
            }
            return response;
        }
    }
}
