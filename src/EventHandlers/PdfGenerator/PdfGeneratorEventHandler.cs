using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHandlers.Models;
using EventHandlers.Services.EmailService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;

namespace EventHandlers.PdfGenerator
{
    public class PdfGeneratorEventHandler : IEventHandler<PdfsFromHtmlCreatedEvent, bool>
    {
        private readonly IEmailDataBuilders _emailDataBuilders;
        private readonly ILogger<PdfGeneratorEventHandler> _logger;
        private readonly IInvoiceEmailNotifierService _emailNotifierService;
        private readonly IRepository _repository;
        private readonly IPraxisReportService _praxisReportService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly INotificationService _notificationService;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;
        private readonly IReportTemplateSignatureService _reportTemplateSignatureService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityContextProvider _securityContextProvider;

        public PdfGeneratorEventHandler(
            IEmailDataBuilders emailDataBuilders,
            ILogger<PdfGeneratorEventHandler> logger,
            IInvoiceEmailNotifierService emailNotifierService,
            IRepository repositoryService,
            IPraxisReportService praxisReportService,
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            INotificationService notificationService,
            IPraxisReportTemplateService praxisReportTemplateService,
            IReportTemplateSignatureService reportTemplateSignatureService,
            ITokenService tokenService,
            ISecurityContextProvider securityContextProvider)
        {
            _emailDataBuilders = emailDataBuilders;
            _logger = logger;
            _emailNotifierService = emailNotifierService;
            _repository = repositoryService;
            _praxisReportService = praxisReportService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _notificationService = notificationService;
            _praxisReportTemplateService = praxisReportTemplateService;
            _reportTemplateSignatureService = reportTemplateSignatureService;
            _tokenService = tokenService;
            _securityContextProvider = securityContextProvider;
        }
        public bool Handle(PdfsFromHtmlCreatedEvent data)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> HandleAsync(PdfsFromHtmlCreatedEvent @event)
        {
            _logger.LogInformation("Enter into the event handler {HandlerName} with event -> {Event}", nameof(PdfGeneratorEventHandler), JsonConvert.SerializeObject(@event));
            if (@event.EventReferenceData == null) return true;
            
            if (@event.EventReferenceData.TryGetValue("InvoicePdfFileId", out var invoicePdfFileId) && !string.IsNullOrEmpty(invoicePdfFileId))
            {
                var subscriptionData = GetSubscriptionData(@event.MessageCoRelationId);
                if (subscriptionData != null)
                {
                    await _praxisClientSubscriptionService.UpdateSubscriptionInvoicePdfFileId(@event.MessageCoRelationId, invoicePdfFileId);
                    if (@event.EventReferenceData.TryGetValue("NotifySubscriptionId", out var notifySubscriptionId) && !string.IsNullOrEmpty(notifySubscriptionId))
                    {
                        var denormalizePayload = JsonConvert.SerializeObject(new
                        {
                            InvoicePdfFileId = invoicePdfFileId
                        });
                        await _notificationService.GetCommonSubscriptionNotification(true, notifySubscriptionId, "generate-invoice", "generate-invoice", denormalizePayload);
                    }
                    var organizationData = GetOrganization(subscriptionData.OrganizationId);
                    if (organizationData != null && @event.EventReferenceData.TryGetValue("SendNotification", out var type) && type.Equals("YES"))
                    {
                        var emails = GetPraxisUserEmails(new string[] { organizationData.AdminUserId, organizationData.DeputyAdminUserId });
                        var emailData = _emailDataBuilders.BuildPaymentCompletedEmailData();
                        IEnumerable<string> invoiceId = new string[] { @event.OutputFileId };
                        List<string> emailList = new List<string> { Constants.SystemEmail };
                        if (emails.Count > 0)
                        {
                            emailList.AddRange(emails);
                        }

                        if (!string.IsNullOrWhiteSpace(subscriptionData.ResponsiblePerson?.Email))
                        {
                            emailList.Add(subscriptionData.ResponsiblePerson?.Email);
                        }
                        await _emailNotifierService.SendPaymentCompleteEmail(emailList.Distinct().ToList(), emailData, invoiceId);
                    }
                }
            }
            else if (@event.EventReferenceData.ContainsKey("PraxisReport"))
            {
                await _praxisReportService.HandlePdfGenerationEvent(@event);
            }
            else if (@event.EventReferenceData.TryGetValue("ValidationReport", out var validationReportId))
            {
                _logger.LogInformation("Received template generation success event for ValidationReport with reportFileId: {ReportFileId}", @event.EventReferenceData["ValidationReportTemplate"]);
                var subscriptionData = @event.EventReferenceData["ValidationReportTemplate"];
                var outputFileId = @event.OutputFileId;
                var reportItemId = @event.EventReferenceData["GeneralReportItemId"];
                var isSignaturePending = @event.EventReferenceData.TryGetValue("IsSignaturePending", out var value) && bool.TryParse(value, out var result) && result;

                var loggedInUserId = @event.EventReferenceData.TryGetValue("LoggedInUserId", out var userId) ? userId : null;

                if (!string.IsNullOrEmpty(loggedInUserId))
                {
                    await CreateToken(loggedInUserId);
                }

                if (!string.IsNullOrEmpty(reportItemId))
                {
                    await _praxisReportTemplateService.UpdateReportTemplateConfigWithPdfFileId(reportItemId, outputFileId);
                }
                if (isSignaturePending)
                {
                    await _reportTemplateSignatureService.CreateSignatureRequestAsync(null, reportItemId);
                }
                await _notificationService.EquipmentValidationReportPdfGenerationNotification(@event.Success, subscriptionData, outputFileId);
            }
            return true;
        }

        private List<string> GetPraxisUserEmails(string[] ids)
        {
            return _repository.GetItems<PraxisUser>(pu => ids.Contains(pu.ItemId))
                .Select(pu => pu.Email)
                .ToList();
        }

        private PraxisClientSubscription GetSubscriptionData(string subscripotionId)
        {
            return _repository.GetItem<PraxisClientSubscription>(pcs => pcs.ItemId == subscripotionId);
        }


        private PraxisOrganization GetOrganization(string orgId)
        {
            return _repository.GetItem<PraxisOrganization>(o => o.ItemId == orgId);
        }

        private async Task CreateToken(string userId)
        {
            var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.ItemId == userId);
            if (praxisUser == null) return;
            _tokenService.CreateImpersonateContext(
                _securityContextProvider.GetSecurityContext(),
                praxisUser.Email,
                praxisUser.ItemId,
                praxisUser.Roles.ToList());
        }
    }
}
