using EventHandlers.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace EventHandlers.Services
{
   public class PdfGeneratorService: IPdfGeneratorService
    {
        private readonly ILogger<PdfGeneratorService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly AccessTokenProvider _accessTokenProvider;
        private readonly IServiceClient _serviceClient;
        private readonly string _pdfServiceBaseUrl;
        private readonly string _pdfServiceVersion;
        private readonly string _pdfServicePath;
        private readonly IPraxisReportService _praxisReportService;

        public PdfGeneratorService(
            ILogger<PdfGeneratorService> logger,
            ISecurityContextProvider securityContextProvider,
            AccessTokenProvider accessTokenProvider,
            IConfiguration configuration,
            IServiceClient serviceClient,
            IPraxisReportService praxisReportService
            )
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _accessTokenProvider = accessTokenProvider;
            _serviceClient = serviceClient;
            _pdfServiceBaseUrl = configuration["PdfGeneratorBaseUrl"];
            _pdfServiceVersion = configuration["PdfGeneratorVersion"];
            _pdfServicePath = configuration["PdfGeneratorPath"];
            _praxisReportService = praxisReportService;
        }
        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = Guid.NewGuid().ToString(),
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "lalu vulu",
                UserName = "laluvulu@yopmail.com",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { "admin", "system_admin", "appuser" }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
        public async Task GeneratePdf(PdfGeneratorPayload pdfGeneratorPayload)
        {
            try
            {
                var token = await GetAdminToken();
                var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _pdfServiceBaseUrl,
                    _pdfServiceVersion,
                    _pdfServicePath,
                    pdfGeneratorPayload,
                    token
                );
                _logger.LogInformation(JsonConvert.SerializeObject(response));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Invoice Template generation -> {ex.Message}");
                throw;
            }
        }

        public async Task GeneratePdfReport(IDictionary<string, string> eventReferenceData)
        {
            try
            {
                var pdfGeneratorPayload =
                    JsonConvert.DeserializeObject<PdfGeneratorPayload>(eventReferenceData["PdfGenerationPayload"]);
                if (!pdfGeneratorPayload.EventReferenceData.ContainsKey("PraxisReport"))
                {
                    pdfGeneratorPayload.EventReferenceData.Add("PraxisReport", "true");
                }

                await GeneratePdf(pdfGeneratorPayload);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while generating Pdf report with file id " + eventReferenceData["PraxisReportFileId"]);
                _logger.LogError($"Exception Message: {ex.Message} Exception Details: {ex.StackTrace}");
                await _praxisReportService.UpdatePraxisReportStatus(
                    eventReferenceData["PraxisReportFileId"],
                    PraxisReportProgress.Failed
                );
            }
        }

        public Task GeneratePdfUsingV1(GeneratePdfUsingTemplateEnginePayload pdfGeneratorPayload)
        {
            throw new NotImplementedException();
        }
    }
}
