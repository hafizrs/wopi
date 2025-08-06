using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ConfiguratorModule
{
    public class GetReportTemplatesQueryHandler : IQueryHandler<GetReportTemplatesQuery, EntityQueryResponse<ReportTemplatesResponse>>
    {
        private readonly ILogger<GetReportTemplatesQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetReportTemplatesQueryHandler(
            ILogger<GetReportTemplatesQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<ReportTemplatesResponse> Handle(GetReportTemplatesQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<ReportTemplatesResponse>> HandleAsync(GetReportTemplatesQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetReportTemplatesQueryHandler), query);
            var response = new EntityQueryResponse<ReportTemplatesResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetReportTemplates(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in Handler: {HandlerName} Error Message: {Message}. Details: {StackTrace}",
                    nameof(GetReportTemplatesQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = ex.Message;
                response.StatusCode = 1;
            }
            return response;
        }
    }
}
