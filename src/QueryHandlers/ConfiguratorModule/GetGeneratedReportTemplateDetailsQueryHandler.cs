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
    public class GetGeneratedReportTemplateDetailsQueryHandler : IQueryHandler<GetGeneratedReportTemplateDetailsQuery, EntityQueryResponse<GeneratedReportTemplateDetailsResponse>>
    {
        private readonly ILogger<GetGeneratedReportTemplateDetailsQueryHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        public GetGeneratedReportTemplateDetailsQueryHandler(
            ILogger<GetGeneratedReportTemplateDetailsQueryHandler> logger,
            IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }
        public EntityQueryResponse<GeneratedReportTemplateDetailsResponse> Handle(GetGeneratedReportTemplateDetailsQuery query)
        {
            throw new NotImplementedException();
        }
        public async Task<EntityQueryResponse<GeneratedReportTemplateDetailsResponse>> HandleAsync(GetGeneratedReportTemplateDetailsQuery query)
        {
            _logger.LogInformation("Entered into Handler: {HandlerName} with Query: {Query}", nameof(GetGeneratedReportTemplateDetailsQueryHandler), JsonConvert.SerializeObject(query));
            var response = new EntityQueryResponse<GeneratedReportTemplateDetailsResponse>();
            try
            {
                response = await _praxisReportTemplateService.GetGeneratedReportTemplateDetails(query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in Handler: {HandlerName}. Message: {Message}, Details: {StackTrace}",
                    nameof(GetGeneratedReportTemplateDetailsQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = $"Message: {ex.Message}. Details: {ex.StackTrace}";
            }
            return response;
        }
    }
}
