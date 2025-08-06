using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.ConfiguratorModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ConfiguratorModule
{
    public class GetGeneratedReportTemplateSectionsQueryHandler : IQueryHandler<GetGeneratedReportTemplateSectionsQuery, EntityQueryResponse<GeneratedReportTemplateSectionResponse>>
    {
        private readonly ILogger<GetGeneratedReportTemplateSectionsQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetGeneratedReportTemplateSectionsQueryHandler(
            ILogger<GetGeneratedReportTemplateSectionsQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<GeneratedReportTemplateSectionResponse> Handle(GetGeneratedReportTemplateSectionsQuery query)
        {
            throw new NotImplementedException();
        }
        public async Task<EntityQueryResponse<GeneratedReportTemplateSectionResponse>> HandleAsync(GetGeneratedReportTemplateSectionsQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetGeneratedReportTemplateSectionsQueryHandler), JsonConvert.SerializeObject(query));
            var response = new EntityQueryResponse<GeneratedReportTemplateSectionResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetGeneratedReportTemplateSections(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in Handler: {HandlerName}. Message: {Message}, Details: {StackTrace}",
                    nameof(GetGeneratedReportTemplateSectionsQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = $"Message: {ex.Message}. Details: {ex.StackTrace}";
            }
            return response;
        }
    }
}
