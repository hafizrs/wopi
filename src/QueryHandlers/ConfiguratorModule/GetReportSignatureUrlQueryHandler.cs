using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
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
    public class GetReportSignatureUrlQueryHandler : IQueryHandler<GetReportSignatureUrlQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetReportSignatureUrlQueryHandler> _logger;
        private readonly IReportTemplateSignatureService _reportTemplateSignatureService;

        public GetReportSignatureUrlQueryHandler(
            ILogger<GetReportSignatureUrlQueryHandler> logger,
            IReportTemplateSignatureService reportTemplateSignatureService)
        {
            _logger = logger;
            _reportTemplateSignatureService = reportTemplateSignatureService;
        }
        public QueryHandlerResponse Handle(GetReportSignatureUrlQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetReportSignatureUrlQuery query)
        {
            _logger.LogInformation("Entered into {HandlerName} with query: {@Query}", nameof(GetReportSignatureUrlQueryHandler), JsonConvert.SerializeObject(query, Formatting.Indented));
            var response = new QueryHandlerResponse();
            try
            {
                var signatureMapping = await _reportTemplateSignatureService.GetSignatureUrlAsync(query.ReportId);
                response.Data = signatureMapping;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {HandlerName}: {ErrorMessage}", nameof(GetReportSignatureUrlQueryHandler), ex.Message);
                response.ErrorMessage = ex.Message;
            }
            return response;
        }
    }
}
