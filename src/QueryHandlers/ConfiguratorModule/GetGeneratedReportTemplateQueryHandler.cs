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
    public class GetGeneratedReportTemplateQueryHandler : IQueryHandler<GetGeneratedReportTemplateQuery, EntityQueryResponse<GeneratedReportTemplateResponse>>
    {
        private readonly ILogger<GetGeneratedReportTemplateQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetGeneratedReportTemplateQueryHandler(
            ILogger<GetGeneratedReportTemplateQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<GeneratedReportTemplateResponse> Handle(GetGeneratedReportTemplateQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<GeneratedReportTemplateResponse>> HandleAsync(GetGeneratedReportTemplateQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetGeneratedReportTemplateQueryHandler), JsonConvert.SerializeObject(query));
            var response = new EntityQueryResponse<GeneratedReportTemplateResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetGeneratedReportTemplates(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in Handler: {HandlerName}. Message: {Message}, Details: {StackTrace}", 
                    nameof(GetGeneratedReportTemplateQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = $"Message: {ex.Message}. Details: {ex.StackTrace}";
            }
            return response;
        }
    }
}
