using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using System.Reflection;
using System.Runtime.Serialization;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard
{



    public class CirsReportEventHandlerService : ICirsReportEventHandlerService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly ILogger<CirsReportEventHandlerService> _logger;
        private readonly ISequenceNumberService _sequenceNumberService;
        private readonly ICirsPermissionService _cirsPermissionService;
        private readonly IBlocksMongoDbDataContextProvider _dbDataContextProvider;
        private readonly IExternalUserCreateService _externalUserCreateService;
        private readonly IEmailDataBuilder _emailDataBuilder;
        private readonly IEmailNotifierService _emailNotifierService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly IExportReportService _exportReportService;
        public CirsReportEventHandlerService(
            ILogger<CirsReportEventHandlerService> logger,
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            ISequenceNumberService sequenceNumberService,
            ICirsPermissionService cirsPermissionService,
            IBlocksMongoDbDataContextProvider dbDataContextProvider,
            IExternalUserCreateService externalUserCreateService,
            IEmailDataBuilder emailDataBuilder,
            IEmailNotifierService emailNotifierService,
            IGenericEventPublishService genericEventPublishService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IExportReportService exportReportService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _sequenceNumberService = sequenceNumberService;
            _cirsPermissionService = cirsPermissionService;
            _dbDataContextProvider = dbDataContextProvider;
            _externalUserCreateService = externalUserCreateService;
            _emailDataBuilder = emailDataBuilder;
            _emailNotifierService = emailNotifierService;
            _genericEventPublishService = genericEventPublishService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _exportReportService = exportReportService;
            _logger = logger;
        }
        private static string GetCirsDashboardName(CirsDashboardName enumValue)
        {
            return typeof(CirsDashboardName)
                .GetMember(enumValue.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? enumValue.ToString();
        }

        public static int GetSwissTimeZoneOffset(DateTime utcTime)
        {
            var zurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var offset = zurichTimeZone.GetUtcOffset(utcTime);
            return (int)offset.TotalMinutes;
        }



        private async Task<string[]> GetCirsExternalOfficerAttachmentsFileIds(CirsGenericReport report, string clientId)
        {

            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string fileName = $"Report_{GetCirsDashboardName(report.CirsDashboardName)}_{currentDate}";
            List<string> attachmentList = new List<string>();
            var externalCirsReport = new ExportReportCommand
            {
                CirsDashboardName = GetCirsDashboardName(report.CirsDashboardName),
                CirsReportId = report.ItemId,
                ClientId = clientId,
                CreateDateFilter = null,
                FileName = fileName,
                FileNameWithExtension = $"{fileName}.xlsx",
                IsActive = true,
                LanguageKey = "en",
                OrganizationId = report.OrganizationId,
                ReportFileId = Guid.NewGuid().ToString(),
                RequestedOn = DateTime.Now,
                TextSearchKey = "",
                TimezoneOffsetInMinutes = GetSwissTimeZoneOffset(DateTime.UtcNow),
            };

            bool success = await _exportReportService.ExportCirsReport(externalCirsReport);

            if (success)
            {
                attachmentList.Add(externalCirsReport.ReportFileId);

                if (report.AttachmentIds != null)
                {
                    attachmentList.AddRange(report.AttachmentIds);
                }

                return attachmentList.ToArray();
            }

            return Array.Empty<string>();
        }
        private async Task<CirsGenericReport> GetCirsReportByIdAsync(string cirsReportId)
        {

            return await _repository.GetItemAsync<CirsGenericReport>(i =>
                i.ItemId == cirsReportId &&
                !i.IsMarkedToDelete);
        }

        private async Task<PraxisClient> GetPraxisCientAsnyc(string clientId)
        {

            return await _repository.GetItemAsync<PraxisClient>(i =>
                i.ItemId == clientId &&
                !i.IsMarkedToDelete);
        }

        public async Task ProcessEmailForCirsExternalUsers(CirsReportEvent cirsReportEvent)
        {
            try
            {
                if (cirsReportEvent?.IsColumnChanged != true) return;
                var report = await GetCirsReportByIdAsync(cirsReportEvent?.ReportId);

                if (report == null)
                {
                    _logger.LogError("Exception occured during ProcessEmailForCirsExternalUsers event handle: Cirs report not found");
                    return;
                }
                if (report.CirsDashboardName != CirsDashboardName.Incident) return;

                var statusList = report.CirsDashboardName.GetCirsReportStatusEnumValues();
                if (report.Status != statusList[1]) return;

                var clientId = report?.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? report.ClientId;
                var client = await GetPraxisCientAsnyc(clientId);

                string[] attachments = await GetCirsExternalOfficerAttachmentsFileIds(report, clientId);

                var emailTasks = new List<Task<bool>>();
                var purposeByOffice = report.CirsDashboardName == CirsDashboardName.Hint ? EmailTemplateName.HintReported.ToString() : EmailTemplateName.CIRSReported.ToString();
                if (report?.ExternalReporters?.Count > 0)
                {
                    foreach (var externalInfo in report.ExternalReporters)
                    {
                        if (!string.IsNullOrEmpty(externalInfo?.SupplierInfo?.SupplierEmail))
                        {
                            var person = new Person()
                            {
                                DisplayName = externalInfo.SupplierInfo.SupplierName,
                                Email = externalInfo.SupplierInfo.SupplierEmail
                            };
                            var emailData = _emailDataBuilder.BuildCirsReportEmailData(report, person, client.ClientName, externalInfo?.SupplierInfo);
                            var emailStatus = _emailNotifierService.SendEmail(
                                                                        person.Email,
                                                                        purposeByOffice,
                                                                        emailData,
                                                                        attachments,
                                                                        true
                                                                    );
                            emailTasks.Add(emailStatus);
                        }
                    }
                }


                if (emailTasks.Count > 0) await Task.WhenAll(emailTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ProcessEmailForCirsExternalUsers: {ex.Message} -> {ex.StackTrace}"); ;

            }

        }
    }
}
