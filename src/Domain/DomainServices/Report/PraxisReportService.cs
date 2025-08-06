using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml.Style;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.Entities.PrimaryEntities.GermanRailway;
using SeliseBlocks.MailService.Services.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class PraxisReportService : IPraxisReportService
    {
        private readonly ILogger<PraxisReportService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly INotificationService _notificationService;
        private readonly IRepository _repository;
        private readonly ICirsPermissionService _cirsPermissionService;
        private readonly ISecurityHelperService _securityHelperService;

        public PraxisReportService(
            ILogger<PraxisReportService> logger,
            IRepository repository,
            INotificationService notificationService,
            ISecurityContextProvider securityContextProvider,
            ICirsPermissionService cirsPermissionService,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _repository = repository;
            _notificationService = notificationService;
            _securityContextProvider = securityContextProvider;
            _cirsPermissionService = cirsPermissionService;
            _securityHelperService = securityHelperService;
        }

        public void AddLogoInExcelReport(
            ExcelWorksheet workSheetTable,
            int logoSize,
            int logoPosition,
            string logoLocation,
            int columnOffsetPixel = 0
        )
        {
            try
            {
                // Add Logo
                var appDirectoryFolderPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

                if (appDirectoryFolderPath == null)
                {
                    _logger.LogError("App directory folder path is null, logo is not found.");
                    return;
                }

                var logoFilePath = @"" + appDirectoryFolderPath + rqLatestLogo;
                //Pass the filepath and filename to the StreamWriter Constructor
                if (!File.Exists(logoFilePath))
                {
                    _logger.LogError("Logo is not found. File '{LogoFilePath}' doesn't exist.", logoFilePath);
                    return;
                }

                var imageBytes = File.ReadAllBytes(logoFilePath);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var logoImage = Image.FromStream(ms, true, true);
                    var excelPicture = workSheetTable.Drawings.AddPicture("Logo", logoImage);
                    excelPicture.SetPosition(0, 0, logoPosition - 1, columnOffsetPixel);
                    excelPicture.SetSize(logoSize);
                }
                _logger.LogInformation("Logo added to excel successfully");

            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding logo to Excel. Error: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        public void DrawBorder(ExcelWorksheet workSheetTable, int startRow, int startColumn, int endRow, int endColumn)
        {
            var range = workSheetTable.Cells[startRow, startColumn, endRow, endColumn];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        }

        private PraxisReport SetDynamicRolesForPraxisReport(PraxisReport praxisReport, IEnumerable<string> clientIds)
        {
            var necessaryRoles = new List<string> { RoleNames.Admin, RoleNames.TaskController };
            foreach (var clientId in clientIds)
            {
                necessaryRoles.AddRange(
                    new List<string>
                    {
                        $"{RoleNames.PowerUser_Dynamic}_{clientId}",
                        $"{RoleNames.Leitung_Dynamic}_{clientId}"
                    }
                );
            }
            praxisReport.RolesAllowedToDelete = necessaryRoles.ToArray();
            praxisReport.RolesAllowedToRead = necessaryRoles.ToArray();
            praxisReport.RolesAllowedToUpdate = necessaryRoles.ToArray();
            return praxisReport;
        }

        public List<string> GetDynamicRolesForPraxisReportFromCLietIds(List<string> clientIds)
        {
            var necessaryRoles = new List<string> { RoleNames.Admin, RoleNames.TaskController };
            foreach (var clientId in clientIds)
            {
                necessaryRoles.AddRange(
                    new List<string>
                    {
                        $"{RoleNames.PowerUser_Dynamic}_{clientId}",
                        $"{RoleNames.Leitung_Dynamic}_{clientId}"
                    }
                );
            }
            return necessaryRoles;
        }

        public async Task CreatePraxisReport(ExportReportCommand reportCommand, string moduleName)
        {
            var praxisReport = await GenerateNewPraxisReportAsync(
                moduleName,
                reportCommand.FileNameWithExtension,
                reportCommand.ReportFileId,
                reportCommand.ReportRemarks,
                reportCommand.RequestedOn ?? DateTime.Now,
                reportCommand.OrganizationId,
                reportCommand.ClientId,
                dashboardName: reportCommand.DashboardNameEnum);

            await InsertOrUpdatePraxisReport(praxisReport, reportCommand.ReportFileId);
        }

        public async Task CreatePraxisReport(GeneratePdfUsingTemplateEngineCommand reportCommand)
        {
            var praxisReport = await GenerateNewPraxisReportAsync(
                reportCommand.ModuleName,
                reportCommand.FileNameWithExtension,
                reportCommand.ReportFileId,
                reportCommand.ReportRemarks,
                reportCommand.RequestedOn ?? DateTime.Now,
                null,
                reportCommand.ClientId);

            await InsertOrUpdatePraxisReport(praxisReport, reportCommand.ReportFileId);
        }

        public Task<PraxisReport> CreatePraxisReportWithExportReportCommand(ExportReportCommand command, string moduleName)
        {
            var praxisReport = GeneratePraxisReportWithExportReportCommand(command, moduleName);

            return Task.FromResult(praxisReport);
        }


        public async Task InsertOrUpdatePraxisReport(PraxisReport praxisReport, string reportFileId)
        {
            var existingReport = await _repository.GetItemAsync<PraxisReport>(
                report => report.ReportFileId.Equals(reportFileId)
            );
            if (existingReport == null)
            {
                _logger.LogInformation("Saving praxis report for {ModuleName} with file Id: {ReportFileId}", praxisReport.ModuleName, praxisReport.ReportFileId);
                await _repository.SaveAsync(praxisReport);
            }
            else
            {
                _logger.LogInformation("Updating praxis report for {ModuleName} with file Id: {ReportFileId}", praxisReport.ModuleName, praxisReport.ReportFileId);
                praxisReport.ItemId = existingReport.ItemId;
                await _repository.UpdateAsync(report => report.ReportFileId.Equals(reportFileId), praxisReport);
            }
        }

        public async Task UpdatePraxisReportStatus(string reportFileId, string status)
        {
            try
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var praxisReport = await _repository.GetItemAsync<PraxisReport>(
                    report => report.ReportFileId == reportFileId
                );
                if (praxisReport != null)
                {
                    praxisReport.LastUpdateDate = new DateTime();
                    praxisReport.LastUpdatedBy = userId;
                    praxisReport.Status = status;
                    await _repository.UpdateAsync(report => report.ReportFileId == reportFileId, praxisReport);
                    switch (status)
                    {
                        case PraxisReportProgress.Complete:
                            await _notificationService.PraxisReportStatusNotifyToClient(true, reportFileId);
                            break;
                        case PraxisReportProgress.Failed:
                            await _notificationService.PraxisReportStatusNotifyToClient(false, reportFileId);
                            break;
                    }
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while updating PraxisReport with ReportFileId: {ReportFileId}", reportFileId);
                _logger.LogInformation("Attempted status update: {Status}", status);
                _logger.LogError("Error message: {Message}. Error stacktrace: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        public async Task HandlePdfGenerationEvent(PdfsFromHtmlCreatedEvent pdfFromHtmlCreatedEvent)
        {
            await UpdatePraxisReportStatus(
                pdfFromHtmlCreatedEvent.MessageCoRelationId,
                pdfFromHtmlCreatedEvent.Success ? PraxisReportProgress.Complete : PraxisReportProgress.Failed
            );
        }

        private async Task<PraxisReport> GenerateNewPraxisReportAsync(
            string moduleName,
            string reportName,
            string reportFileId,
            string reportRemarks,
            DateTime createDate,
            string organizationId = null,
            string clientId = null,
            string reportType = null,
            CirsDashboardName? dashboardName = null)
        {
            var emptyStringArray = Array.Empty<string>();
            var securityContext = _securityContextProvider.GetSecurityContext();
            var creatorUserId = securityContext.UserId;
            var necessaryRoles = new List<string> { RoleNames.Admin, RoleNames.TaskController };
            if (!string.IsNullOrEmpty(clientId))
            {
                necessaryRoles.AddRange(
                    new List<string>
                    {
                        $"{RoleNames.PowerUser_Dynamic}_{clientId}",
                        $"{RoleNames.Leitung_Dynamic}_{clientId}"
                    }
                );
            }

            if (securityContext.Roles.Contains(RoleNames.AdminB))
            {
                var orgIds = _securityHelperService.ExtractOrganizationIdsFromOrgLevelUser();
                foreach (var orgId in orgIds)
                {
                    necessaryRoles.Add($"{RoleNames.AdminB_Dynamic}_{orgId}");
                }
            }

            return new PraxisReport
            {
                ItemId = Guid.NewGuid().ToString(),
                ModuleName = moduleName,
                ReportName = reportName,
                ReportFileId = reportFileId,
                ReportRemarks = reportRemarks,
                ReportType = reportType ?? reportName.Split('.').Last(),
                Status = PraxisReportProgress.Pending,
                OrganizationId = organizationId,
                ClientIds = string.IsNullOrEmpty(clientId) ? emptyStringArray : new[] { clientId },
                CreateDate = createDate,
                CreatedBy = creatorUserId,
                LastUpdatedBy = creatorUserId,
                IdsAllowedToDelete = emptyStringArray,
                IdsAllowedToRead = moduleName == "CIRS"
                    ? await GetCirsReportIdsAllowedToReadAsync(clientId, dashboardName)
                    : emptyStringArray,
                IdsAllowedToUpdate = emptyStringArray,
                IdsAllowedToWrite = emptyStringArray,
                Language = "en-US",
                LastUpdateDate = default,
                RolesAllowedToDelete = necessaryRoles.ToArray(),
                RolesAllowedToRead = moduleName == "CIRS" 
                    ? GetCirsReportRolesAllowedToRead(organizationId, clientId)
                    : necessaryRoles.ToArray(),
                RolesAllowedToUpdate = necessaryRoles.ToArray(),
                RolesAllowedToWrite = emptyStringArray,
                Tags = emptyStringArray,
                TenantId = PraxisConstants.PraxisTenant,
                IsMarkedToDelete = false
            };
        }

        private PraxisReport GeneratePraxisReportWithExportReportCommand(ExportReportCommand command, string moduleName, string reportType = null)
        {
            var emptyStringArray = Array.Empty<string>();
            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;

            return new PraxisReport
            {
                ItemId = Guid.NewGuid().ToString(),
                ModuleName = moduleName,
                ReportName = command.FileNameWithExtension,
                ReportFileId = command.ReportFileId,
                ReportRemarks = command.ReportRemarks,
                ReportType = reportType ?? command.FileNameWithExtension.Split('.').Last(),
                Status = PraxisReportProgress.Pending,
                OrganizationId = command.OrganizationId,
                ClientIds = string.IsNullOrEmpty(command.ClientId) ? emptyStringArray : new[] { command.ClientId },
                CreateDate = command.RequestedOn ?? DateTime.Now,
                CreatedBy = userId,
                LastUpdatedBy = userId,
                Language = "en-US",
                LastUpdateDate = default,
                Tags = emptyStringArray,
                TenantId = PraxisConstants.PraxisTenant,
                IsMarkedToDelete = false
            };
        }

        private static string[] GetCirsReportRolesAllowedToRead(string organizationId, string clientId)
        {
            var roles = new List<string>
            {
                RoleNames.Admin,
                RoleNames.TaskController,
                $"{RoleNames.AdminB_Dynamic}_{organizationId}",
                $"{RoleNames.PowerUser_Dynamic}_{clientId}",
                $"{RoleNames.Leitung_Dynamic}_{clientId}",
                $"{RoleNames.MpaGroup_Dynamic}_{clientId}"
            };

            return roles.ToArray();
        }

        private async Task<string[]> GetCirsReportIdsAllowedToReadAsync(string praxisClientId, CirsDashboardName? dashboardName)
        {
            var creatorUserId = _securityContextProvider.GetSecurityContext().UserId;
            var creatorIdAllowedToRead = new string[] { creatorUserId };
            if (dashboardName == null) return creatorIdAllowedToRead;

            var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(praxisClientId, (CirsDashboardName)dashboardName);
            if (permission == null) return creatorIdAllowedToRead;

            var permissionAdminsIds = permission?.AdminIds?.Select(ad => ad.UserId) ?? Array.Empty<string>();
            return creatorIdAllowedToRead.Concat(permissionAdminsIds).ToArray();
        }

        public async Task<bool> DeletePraxisReport(string itemId)
        {
            try
            {
                await _repository.DeleteAsync<PraxisReport>(report => report.ItemId.Equals(itemId));
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {EntityName} with ItemId {ItemId}", nameof(PraxisReport), itemId);
                _logger.LogError("Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
                return false;
            }
        }

        private PraxisReport UpdatePermissionForRiskManagementPraxisReport(PraxisReport praxisReport, IEnumerable<string> clientIds)
        {
            try
            {
                if (praxisReport.ModuleName.Equals("RISK_MANAGEMENT") && praxisReport.ReportRemarks.Equals("RISK_ALL_CLIENT_EXCEL_REPORT"))
                {
                    var user = _securityContextProvider.GetSecurityContext();
                    if (!user.Roles.Contains(RoleNames.Admin) && !user.Roles.Contains(RoleNames.TaskController))
                    {
                        praxisReport = SetDynamicRolesForPraxisReport(praxisReport, clientIds);
                    }
                }
                return praxisReport;
            }
            catch (Exception)
            {
                return praxisReport;
            }
        }

        public async Task AddClientIdsToPraxisReport(string reportFileId, IEnumerable<string> clientIds)
        {
            _logger.LogInformation("Going to add clientIds to PraxisReport with file id: {ReportFileId}", reportFileId);
            try
            {

                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var praxisReport = await _repository.GetItemAsync<PraxisReport>(
                    report => report.ReportFileId == reportFileId
                );
                praxisReport.LastUpdateDate = new DateTime();
                praxisReport.LastUpdatedBy = userId;

                praxisReport = UpdatePermissionForRiskManagementPraxisReport(praxisReport, clientIds);

                praxisReport.ClientIds = clientIds;
                await _repository.UpdateAsync(report => report.ReportFileId.Equals(reportFileId), praxisReport);

                _logger.LogInformation("Successfully added clientIds to PraxisReport with file id: {ReportFileId}", reportFileId);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while adding clientIds to PraxisReport with file id: {ReportFileId}", reportFileId);
                _logger.LogError("Error message: {Message}. Full StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        public async Task InsertOrUpdateRowLevelSecurity(IRowLevelSecurity roles, string reportFileId)
        {
            var report = await _repository.GetItemAsync<PraxisReport>(pr => pr.ReportFileId.Equals(reportFileId));
            if (report == null)
            {
                _logger.LogError("PraxisReport with file id: {ReportFileId} not found", reportFileId);
                return;
            }

            report.RolesAllowedToRead = UpdateRolesOrIds( report.RolesAllowedToRead, roles.RolesAllowedToRead);
            report.RolesAllowedToUpdate = UpdateRolesOrIds(report.RolesAllowedToUpdate, roles.RolesAllowedToUpdate);
            report.RolesAllowedToDelete = UpdateRolesOrIds(report.RolesAllowedToDelete, roles.RolesAllowedToDelete);
            report.RolesAllowedToWrite = UpdateRolesOrIds(report.RolesAllowedToWrite, roles.RolesAllowedToWrite);

            report.IdsAllowedToRead = UpdateRolesOrIds(report.IdsAllowedToRead, roles.IdsAllowedToRead);
            report.IdsAllowedToUpdate = UpdateRolesOrIds(report.IdsAllowedToUpdate, roles.IdsAllowedToUpdate);
            report.IdsAllowedToDelete = UpdateRolesOrIds(report.IdsAllowedToDelete, roles.IdsAllowedToDelete);
            report.IdsAllowedToWrite = UpdateRolesOrIds(report.IdsAllowedToWrite, roles.IdsAllowedToWrite);
            
            await _repository.UpdateAsync<PraxisReport>(p => p.ItemId.Equals(report.ItemId), report);
        }

        private static string[] UpdateRolesOrIds(string[] current, string[] newItems)
        {
            current ??= Array.Empty<string>();
            if (newItems?.Length > 0)
            {
                current = current.Union(newItems).Distinct().ToArray();
            }

            return current;
        }

    }
}