using EventHandlers.Services;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.TemplateEngine.Events;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.GermanRailway;

namespace EventHandlers.TemplateGenerator
{
    public class TemplateGeneratorEventHandler : IEventHandler<CreateFileWithFilteredSQLQueryEvent, bool>
    {
        private readonly ILogger<TemplateGeneratorEventHandler> _logger;
        private readonly IPdfGeneratorService _pdfGeneratorService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IServiceClient _serviceClient;

        public TemplateGeneratorEventHandler(
            ILogger<TemplateGeneratorEventHandler> logger,
            IPdfGeneratorService pdfGeneratorService,
            IPraxisReportService praxisReportService,
            IServiceClient serviceClient
        )
        {
            _logger = logger;
            _pdfGeneratorService = pdfGeneratorService;
            _praxisReportService = praxisReportService;
            _serviceClient = serviceClient;
        }
        public bool Handle(CreateFileWithFilteredSQLQueryEvent @event)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(CreateFileWithFilteredSQLQueryEvent @event)
        {
            _logger.LogInformation("Enter into the event handler TemplateGeneratorEventHandler with event -> {event}", JsonConvert.SerializeObject(@event));
            try
            {
                _logger.LogInformation("EventReferenceData -> {eventReferenceData}", JsonConvert.SerializeObject(@event.EventReferenceData));
                if (@event.EventReferenceData != null)
                {
                    
                    if (@event.EventReferenceData.TryGetValue("EventReference", out var eventRef) && eventRef.Equals("PaymentSuccessful"))
                    {
                        _logger.LogInformation("Pdf Generate with template I -> {fileId}", @event.FileId);
                        PdfGeneratorPayload pdfGeneratorPayload = PreparePdfGeneratorPayloadV2(@event);
                        await _pdfGeneratorService.GeneratePdf(pdfGeneratorPayload);
                    }
                    else if (@event.EventReferenceData.TryGetValue("PraxisReportFileId", out var reportFileId))
                    {
                        _logger.LogInformation("Received template generation success event for PraxisReport with reportFileId: {reportFileId}", reportFileId);
                        @event.EventReferenceData.TryGetValue("ReportType", out var reportType);
                        switch (reportType)
                        {
                            case "pdf":
                                await _pdfGeneratorService.GeneratePdfReport(@event.EventReferenceData);
                                break;
                            case "docx":
                            {
                                _serviceClient.SendToQueue<CommandResponse>(
                                    PraxisConstants.GetReportQueueName(),
                                    new GenerateDocumentFromHtmlCommand
                                    {
                                        HtmlFileId = @event.FileId,
                                        PraxisReportFileId = @event.EventReferenceData["PraxisReportFileId"],
                                        FileNameWithExtension = @event.EventReferenceData["FileNameWithExtension"]
                                    }
                                );
                                break;
                            }
                            default:
                                await _praxisReportService.UpdatePraxisReportStatus(
                                    @event.EventReferenceData["PraxisReportFileId"],
                                    PraxisReportProgress.Failed
                                );
                                _logger.LogError("No report type found for PraxisReport with reportFileId: {reportFileId}", reportFileId);
                                break;
                        }
                    }
                    else if (@event.EventReferenceData.TryGetValue("ValidationReportTemplate", out var validationReportId))
                    {
                        _logger.LogInformation("Received template generation success event for ValidationReport with reportFileId: {reportFileId}", validationReportId);
                        @event.EventReferenceData.TryGetValue("ReportType", out var reportType);
                        switch (reportType)
                        {
                            case "pdf":
                                try
                                {
                                    var pdfGeneratorPayload = JsonConvert.DeserializeObject<PdfGeneratorPayload>(@event.EventReferenceData["PdfGeneratorPayload"]);
                                    if (!pdfGeneratorPayload.EventReferenceData.ContainsKey("ValidationReport"))
                                    {
                                        pdfGeneratorPayload.EventReferenceData.Add("ValidationReport", "true");
                                    }
                                    await _pdfGeneratorService.GeneratePdf(pdfGeneratorPayload);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError("Error occurred while generating Pdf report with file id " + validationReportId);
                                    throw;
                                }
                                break;
                            default:
                                _logger.LogError("No report type found for ValidationReport with reportFileId: {reportFileId}", validationReportId);
                                break;
                        }
                    }
                    else
                    {
                        _logger.LogError("No EventReference found in EventReferenceData");
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error in TemplateGeneratorEventHandler ->  {ex.Message}");
            }
            return true;
        }
        private PdfGeneratorPayload PreparePdfGeneratorPayloadV2(CreateFileWithFilteredSQLQueryEvent eventData)
        {
            PdfGeneratorConfiguration pdfGeneratorConfig = new PdfGeneratorConfiguration()
            {
                HtmlFileId = eventData.FileId,
                FooterHtmlFileId = string.Empty,
                HeaderHtmlFileId = string.Empty,
                DirectoryId = string.Empty,
                OutputPdfFileId = Guid.NewGuid().ToString(),
                OutputPdfFileName = $"invoice_{DateTime.UtcNow.ToString("yyyy-MM-dd")}.pdf",
                HeaderHeight = 0,
                FooterHeight = 0,
                IsPageNumberEnabled = false,
                IsTotalPageCountEnabled = false,
                UseFormatting = false,
                Engine = 3,
                Profile = "4c93a871-6617-47f6-85ed-acbf5a02c406",
                HasHeader = false,
                HasFooter = false,
                OpenInBrowser = false
            };
            var pdfGeneratorConfigList = new List<PdfGeneratorConfiguration>() { pdfGeneratorConfig };
            var notifySubscriptionId = string.Empty;
            if (eventData.EventReferenceData.TryGetValue("NotifySubscriptionId", out var nottifySubscriptionId) && !string.IsNullOrEmpty(nottifySubscriptionId))
            {
                notifySubscriptionId = nottifySubscriptionId;
            }
            return new PdfGeneratorPayload()
            {
                EventReferenceData = new Dictionary<string, string> {
                    {"InvoicePdfFileId", pdfGeneratorConfig.OutputPdfFileId },
                    {"NotifySubscriptionId", notifySubscriptionId }
                },
                MessageCoRelationId = eventData.SubscriptionFilterId,
                CreateFromHtmlCommands = pdfGeneratorConfigList.ToArray()
            };
        }
    }
}
