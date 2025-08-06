using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ConfiguratorModule
{
    public class GetReportTemplateSectionsQueryHandler : IQueryHandler<GetReportTemplateSectionsQuery, EntityQueryResponse<ReportTemplateSectionResponse>>
    {
        private readonly ILogger<GetReportTemplateSectionsQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetReportTemplateSectionsQueryHandler(
            ILogger<GetReportTemplateSectionsQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<ReportTemplateSectionResponse> Handle(GetReportTemplateSectionsQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<ReportTemplateSectionResponse>> HandleAsync(GetReportTemplateSectionsQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetReportTemplateSectionsQueryHandler), query);
            var response = new EntityQueryResponse<ReportTemplateSectionResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetReportTemplateSections(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in Handler: {HandlerName} Error Message: {Message}. Details: {StackTrace}",
                    nameof(GetReportTemplateSectionsQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = ex.Message;
                response.StatusCode = 1;
            }
            return response;
        }
    }
}
