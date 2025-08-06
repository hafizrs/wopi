using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using SeliseBlocks.Entities.PrimaryEntities.EnterpriseCRM;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport
{
    public class CirsReportGenerationService : ICirsReportGenerationService
    {
        private readonly ILogger<CirsReportGenerationService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly ICirsPermissionService _cirsPermissionService;
        private readonly IRepository _repository;
        private readonly ISecurityHelperService _securityHelperService;

        private Dictionary<string, string> _translatedStringsAsDictionary = new();

        public CirsReportGenerationService(
            ILogger<CirsReportGenerationService> logger,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProviderprovider,
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisReportService praxisReportService,
            ICirsPermissionService cirsPermissionService,
            IRepository repository,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _ecapMongoDbDataContextProvider= ecapMongoDbDataContextProviderprovider;
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisReportService = praxisReportService;
            _cirsPermissionService = cirsPermissionService;
            _repository = repository;
            _securityHelperService = securityHelperService;
        }

        public async Task<bool> GenerateReport(ExcelPackage excel, ExportReportCommand command)
        {
            try
            {
                _translatedStringsAsDictionary = GetTranslationFromUILM(command.LanguageKey, command.DashboardNameEnum);
                var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                var worksheet = excel.Workbook.Worksheets.Add(command.FileName);

                var cirsGenericReports = await GetCirsGenericReportsAsync(command);

                var dashboardName = command.DashboardNameEnum.Value;
                var configName = GetConfigurationName(command.ClientId, dashboardName);
                WriteHeader(worksheet, command.FileName, reportDateString, dashboardName);
                WriteCirsReport(worksheet, dashboardName, cirsGenericReports, configName);
                SetColumnSpecificStyle(worksheet);
                AddBorderToTable(worksheet, cirsGenericReports.Count + 1);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during report generation");
                _logger.LogError("Exception message: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private string GetConfigurationName(string clientId, CirsDashboardName dashboardName)
        {
            var client = _repository.GetItem<PraxisClient>(pc => pc.ItemId == clientId);
            return client?.CirsReportConfig.GetAssignmentLevel(dashboardName).ToString() ?? string.Empty;
        }

        private async Task<List<CirsGenericReport>> GetCirsGenericReportsAsync(ExportReportCommand command)
        {
            var collection =
                _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<CirsGenericReport>("CirsGenericReports");
            
            var praxisClient = (await GetPraxisClientsAsync(command.ClientId)).FirstOrDefault();
            var dashboardPermission = command.DashboardNameEnum != null 
                ? await _cirsPermissionService.GetCirsDashboardPermissionAsync(command.ClientId, command.DashboardNameEnum.Value, true) 
                : null;
            var isACirsAdmin = command.DashboardNameEnum != null && await _cirsPermissionService.IsACirsAdminAsync(
                command.ClientId,
                (CirsDashboardName)command.DashboardNameEnum,
                praxisClient,
                dashboardPermission);

            var filter = GetCirsGenericReportQueryFilter(command, isACirsAdmin, dashboardPermission);

            var projection = Builders<CirsGenericReport>.Projection
                .Include(r => r.ItemId)
                .Include(r => r.Tags)
                .Include(r => r.SequenceNumber)
                .Include(r => r.CreateDate)
                .Include(r => r.Title)
                .Include(r => r.KeyWords)
                .Include(r => r.Description)
                .Include(r => r.Status)
                .Include(r => r.Remarks)
                .Include(r => r.StatusChangeLog)
                .Include(r => r.IsActive)
                .Include(r => r.MetaData)
                .Include(r => r.CirsDashboardName)
                .Include(r => r.OpenItemAttachments)
                .Include(r => r.ProcessGuideAttachments)
                .Include(r => r.CreatedBy)
                .Include(r => r.LastUpdatedBy)
                .Include(r => r.CirsEditHistory);

            var cirsGenericReportDocuments = await collection
                .Find(filter)
                .Sort("{LastUpdateDate: -1}")
                .Project(projection)
                .ToListAsync();

            var cirsGenericReports = cirsGenericReportDocuments
                .Select(i => BsonSerializer.Deserialize<CirsGenericReport>(i))
                .ToList();

            FormatIncidentSequencEnumber(cirsGenericReports);

            return cirsGenericReports;
        }

        private string[] GetAdminRoles()
        {
            var roles = new string[] { RoleNames.Admin, RoleNames.TaskController };
            return roles;
        }


        private void FormatIncidentSequencEnumber(List<CirsGenericReport> cirsGenericReports)
        {
            cirsGenericReports.ForEach(report =>
            {
                report.SequenceNumber = report.Tags[0] == PraxisTag.IsValidCirsReport ? $"#IN{report.SequenceNumber}" : $"#IN{report.SequenceNumber}(A)";
            });
        }

        private void WriteHeader(
            ExcelWorksheet worksheet, 
            string reportName,
            string dateString,
            CirsDashboardName dashboardName)
        {
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation(CirsReport.CommonKeys["ReportName"]);
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;

                worksheet.Cells[2, 1].Value = GetTranslation(CirsReport.CommonKeys["Date"]);
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;

                var columnIndex = 1;
                const int headerRowIndex = CirsReport.HeaderRowIndex;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                foreach (var column in CirsReport.ColumnKeys(dashboardName))
                {
                    worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation(column.Value);
                }

                //AddHeaderLogo(worksheet);
            }
            catch (Exception e)
            {
                                   _logger.LogError(e,"Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = CirsReport.ColumnCount;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, CirsReport.LogoSize, logoStartColumn, rqLatestLogo);
        }

        private void WriteCirsReport(ExcelWorksheet worksheet, CirsDashboardName dashboardName, List<CirsGenericReport> cirsGenericReports, string configName)
        {
            try
            {
                const int startRow = CirsReport.HeaderRowIndex + 1;
                int rowIndex = startRow - 1;
                for (var index = 0; index < cirsGenericReports.Count; index++)
                {
                    var cirsGenericReport = cirsGenericReports[index];
                    ++rowIndex;
                    var columnIndex = 1;
                    // Card Number
                    worksheet.Cells[rowIndex, columnIndex++].Value = cirsGenericReport.SequenceNumber;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Dashboard Name
                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedDashboardName(cirsGenericReport.CirsDashboardName.ToString());
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Create Date
                    worksheet.Cells[rowIndex, columnIndex++].Value = cirsGenericReport.CreateDate.ToString("dd.MM.yyyy");
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Created By
                    var isSenttAnonymously = cirsGenericReport?.MetaData?.TryGetValue($"{CommonCirsMetaKey.IsSentAnonymously}", out object senddAnonymous) == true
                              && senddAnonymous?.ToString() == ((int)CirsBooleanEnum.True).ToString();
                    worksheet.Cells[rowIndex, columnIndex++].Value = isSenttAnonymously ? GetTranslatedAnonymous() : GetPraxisUserNameById(cirsGenericReport.CreatedBy);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Last Updated By
                    worksheet.Cells[rowIndex, columnIndex++].Value = isSenttAnonymously && (cirsGenericReport.CreatedBy == cirsGenericReport.LastUpdatedBy) 
                        ? GetTranslatedAnonymous() : GetPraxisUserNameById(cirsGenericReport.LastUpdatedBy);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Title
                    worksheet.Cells[rowIndex, columnIndex++].Value = cirsGenericReport.Title;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Keywords
                    worksheet.Cells[rowIndex, columnIndex++].Value =
                        cirsGenericReport.KeyWords.ToList().Count == 1
                        ? cirsGenericReport.KeyWords.ToList()[0]
                        : string.Join(", ", cirsGenericReport.KeyWords.ToList());
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Description
                    worksheet.Cells[rowIndex, columnIndex++].Value = cirsGenericReport.Description;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Status
                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedStatus(cirsGenericReport.Status);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Remarks
                    worksheet.Cells[rowIndex, columnIndex++].Value = GetRemarksEditHistory(cirsGenericReport?.CirsEditHistory, cirsGenericReport.Remarks);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Assignment Level
                    worksheet.Cells[rowIndex, columnIndex++].Value = configName;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Reported by
                    var isSentAnonymously = cirsGenericReport?.MetaData?.TryGetValue($"{CommonCirsMetaKey.IsSentAnonymously}", out object sendAnonymous) == true
                                    && sendAnonymous?.ToString() == ((int)CirsBooleanEnum.True).ToString();
                    worksheet.Cells[rowIndex, columnIndex++].Value = isSentAnonymously ? GetTranslatedAnonymous() : GetPraxisUserNameByPraxisId(cirsGenericReport?.ReportedBy?.PraxisUserId);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    // Board Status: [New Complaint, In Progress, Complete]
                    foreach (var status in cirsGenericReport.CirsDashboardName.StatusEnumValues())
                    {
                        worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedStatusChangeLogValue(cirsGenericReport.StatusChangeLog, status);
                        worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    // Inactive
                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedStatusChangeLogValue(cirsGenericReport.StatusChangeLog, CirsCommonEnum.INACTIVE.ToString());
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    foreach (var key in CirsReport.GetDashboardSpecificFields(dashboardName).Keys)
                    {
                        if(string.Equals(key, "ImplementationProposal", StringComparison.InvariantCultureIgnoreCase))
                        {
                            worksheet.Cells[rowIndex, columnIndex++].Value = GetImplementationProposalEditHistory(cirsGenericReport.CirsEditHistory);
                        }
                        else
                        {
                            worksheet.Cells[rowIndex, columnIndex++].Value = string.Equals(key, "Topic", StringComparison.InvariantCultureIgnoreCase)
                            ? GetTranslatedTopic(cirsGenericReport.MetaData.GetValueOrDefault(key)?.ToString())
                            : cirsGenericReport.MetaData.GetValueOrDefault(key)?.ToString();
                        }
                        
                        worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    var hasAnyAttachment = cirsGenericReport.OpenItemAttachments?.Any() == true ||
                                           cirsGenericReport.ProcessGuideAttachments?.Any() == true;
                    // Open Item Attachments
                    var openItemRow = rowIndex - (hasAnyAttachment ? 1 : 0);
                    var guideRow = rowIndex - (hasAnyAttachment ? 1 : 0);
                    foreach (var attachment in cirsGenericReport.OpenItemAttachments ?? new List<OpenItemAttachment>())
                    {
                        var openItem = _repository.GetItem<PraxisOpenItem>(oi => oi.ItemId == attachment.OpenItemId);
                        var properties = new List<object>
                        {
                            attachment.OpenItemName,
                            openItem.CreateDate.ToString("dd.MM.yyyy"),
                            GetPraxisUserNames(openItem),
                            attachment.CompletionStatus,
                            openItem.IsCompleted ? openItem.LastUpdateDate.ToString("dd.MM.yyyy") : "",
                            GetCompletedByUsersOfOpenItem(openItem)
                        };
                        int i = 0, j = columnIndex;
                        ++openItemRow;
                        foreach (var key in CirsReport.GetAttachmentSpecificFields(nameof(PraxisOpenItem)).Keys)
                        {
                            worksheet.Cells[openItemRow, j++].Value = properties[i++];
                            worksheet.Cells[openItemRow, j - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            worksheet.Cells[openItemRow, j - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        worksheet.Row(openItemRow).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    columnIndex += CirsReport.GetAttachmentSpecificFields(nameof(PraxisOpenItem)).Count;
                    // Process Guide Attachments
                    foreach (var attachment in cirsGenericReport.ProcessGuideAttachments ?? new List<ProcessGuideAttachment>())
                    {
                        var guide = _repository.GetItem<PraxisProcessGuide>(oi => oi.ItemId == attachment.ProcessGuideId);
                        var properties = new List<object>
                        {
                            attachment.ProcessGuideTitle,
                            guide.CreateDate.ToString("dd.MM.yyyy"),
                            GetPraxisUserNames(guide),
                            attachment.CompletionStatus,
                            guide.CompletionStatus == 100 ? guide.CompletionDate?.ToString("dd.MM.yyyy") : "",
                            GetCompletedByUsersOfProcessGuide(guide)
                        };
                        int i = 0, j = columnIndex;
                        ++guideRow;
                        foreach (var key in CirsReport.GetAttachmentSpecificFields(nameof(PraxisOpenItem)).Keys)
                        {
                            worksheet.Cells[guideRow, j++].Value = properties[i++];
                            worksheet.Cells[guideRow, j - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            worksheet.Cells[guideRow, j - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }
                        worksheet.Row(guideRow).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    rowIndex = Math.Max(openItemRow, guideRow);
                    worksheet.Row(rowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in WriteUserList");
            }
        }

        private string GetTranslatedTopic(string value)
        {
            return !string.IsNullOrWhiteSpace(value) ? GetTranslation(CirsReport.TopicKeys[value]) : "";
        }

        private string GetTranslatedStatus(string value)
        {
            return !string.IsNullOrWhiteSpace(value) ? GetTranslation(CirsReport.StageKeys[value]) : "";
        }

        private string GetTranslatedStatusChangeLogValue(IEnumerable<StatusChangeEvent> statusChangeLog, string key)
        {
            if (statusChangeLog == null) return "";
            var currentStatusLog = statusChangeLog?.Where(status => status?.CurrentStatus == key && status?.ChangedOn != null)?.Select(status => status?.ChangedOn?.ToString("dd.MM.yyyy"));
            return string.Join(",\n", currentStatusLog);
        }

        private Dictionary<string, string> GetTranslationFromUILM(string languageKey, CirsDashboardName? dashboardName)
        {
            var keyList = new List<string>();
            keyList.AddRange(CirsReport.CommonKeys.Values);
            if (dashboardName != null) keyList.AddRange(CirsReport.ColumnKeys(dashboardName.Value).Values);
            keyList.AddRange(CirsReport.TopicKeys.Values);
            keyList.AddRange(GetDashboardNameKeys());
            return _uilmResourceKeyService
                    .GetResourceValueByKeyName(keyList, languageKey);
        }

        private string GetTranslation(string key)
        {
            try
            {
                return _translatedStringsAsDictionary[key];
            }
            catch (Exception)
            {
                return key;
            }
        }

        private void SetColumnSpecificStyle(ExcelWorksheet worksheet)
        {
            const int tableColumns = CirsReport.ColumnCount;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[CirsReport.HeaderRowIndex, i];
                column.AutoFit(20, 50);
                column.Style.WrapText= true;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(CirsReport.HeaderBackground);
                if (i == tableColumns)
                    column.Width = Math.Max(column.Width, 20);
            }
        }

        private void AddBorderToTable(ExcelWorksheet worksheet, int tableRows)
        {
            const int tableColumns = CirsReport.ColumnCount;
            const int headerRowIndex = CirsReport.HeaderRowIndex;

            for (var i = 1; i <= 2; i++)
                for (var j = 1; j <= 2; j++)
                    worksheet.Cells[i, j].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            for (var i = 1; i <= tableColumns; i++)
            {
                worksheet.Cells[headerRowIndex, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            for (var i = 1; i <= tableColumns; i++)
            {
                worksheet.Cells[tableRows + headerRowIndex - 1, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            for (var i = 1; i <= tableRows; i++)
            {
                worksheet.Cells[i + headerRowIndex - 1, tableColumns].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            for (var i = 1; i <= tableRows; i++)
            {
                worksheet.Cells[i, 1].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            }
        }

        private FilterDefinition<CirsGenericReport> GetCirsGenericReportQueryFilter(ExportReportCommand command, bool isACirsAdmin, CirsDashboardPermission dashboardPermission)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var builder = Builders<CirsGenericReport>.Filter;
            var filter = builder.Eq(r => r.IsMarkedToDelete, false) &
                         builder.Eq(r => r.CirsDashboardName, command.DashboardNameEnum);

            var haveOfficerPermission = _cirsPermissionService.HaveAllUnitViewPermissions(securityContext.UserId, command.DashboardNameEnum).GetAwaiter().GetResult();

            var loggedInUserPermission = _cirsPermissionService.GetPermissionsByDashBoardName(command.DashboardNameEnum.Value, dashboardPermission);
            var praxisClient = GetPraxisClientsAsync(command.ClientId).GetAwaiter().GetResult().FirstOrDefault();

            var filterModel = new GetPermissionFilterModel
            {
                LoggedInUserPermission = loggedInUserPermission,
                DashboardName = command.DashboardNameEnum.Value,
                ClientId = command.ClientId,
                ClientOrganizationId = command.OrganizationId,
                IsActive = command.IsActive ?? false,
                CirsReportId = command.CirsReportId,
                HaveOfficerPermission = haveOfficerPermission,
                PraxisClient = praxisClient,
                IsACirsAdmin = isACirsAdmin,
                DashboardPermission = dashboardPermission
            };

            var permissionFilter = _cirsPermissionService.GetPermissionFilter(filterModel)
                .GetAwaiter().GetResult();
            filter &= permissionFilter;

            filter &= builder.Where(r => 
                r.AffectedInvolvedParties != null && 
                r.AffectedInvolvedParties.Any(party => 
                    party.PraxisClientId == command.ClientId));

            if (!string.IsNullOrWhiteSpace(command.TextSearchKey))
            {
                filter &= builder.Regex(nameof(CirsGenericReport.Title), $"/{command.TextSearchKey}/i");
            }
            if (command.CreateDateFilter?.From != null)
            {
                filter &= builder.Gte(r => r.CreateDate, command.CreateDateFilter.From);
            }

            if (command.CreateDateFilter?.To != null)
            {
                filter &= builder.Lt(r => r.CreateDate, command.CreateDateFilter.To);
            }

            return filter;
        }
        private async Task<List<PraxisClient>> GetPraxisClientsAsync(params string[] praxisClientIds)
        {
            var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisClient>("PraxisClients");
            var filter = Builders<PraxisClient>.Filter.In(client => client.ItemId, praxisClientIds);
            var projection = Builders<PraxisClient>.Projection
                .Include(client => client.ItemId)
                .Include(client => client.ClientName)
                .Include(client => client.ParentOrganizationId);

            return await collection.Find(filter).Project<PraxisClient>(projection).ToListAsync();
        }

        private string GetPraxisUserNameById(string userId)
        {
            return _repository.GetItem<PraxisUser>(pu => pu.UserId == userId)?.DisplayName ?? "-";
        }
        private string GetPraxisUserNameByPraxisId(string userId)
        {
            return _repository.GetItem<PraxisUser>(pu => pu.ItemId == userId)?.DisplayName ?? "-";
        }

        private string GetPraxisUserNames(EntityBase entity)
        {
            switch (entity)
            {
                case PraxisOpenItem openItem:
                    var assignedUsers = openItem.ControlledMembers?.ToList() ?? new List<string>();
                    if (openItem.ControlledGroups?.Any() == true)
                    {
                        var users = GetPraxisUserByRoles(openItem.ClientId, openItem.ControlledGroups.ToList());
                        assignedUsers.AddRange(users.Select(u => u.ItemId));
                    }
                    else if (assignedUsers.Any() == false)
                    {
                        var praxisUsers = GetPraxisUserByClientId(openItem.ClientId);
                        assignedUsers = praxisUsers?.Select(pu => pu.ItemId).ToList() ?? new List<string>();
                    }
                    return string.Join(", ", assignedUsers.Select(GetPraxisUserNameByPraxisId));
                case PraxisProcessGuide processGuide:
                    assignedUsers = processGuide.ControlledMembers?.ToList() ?? new List<string>();
                    if (assignedUsers.Any() == false)
                    {
                        var praxisUsers = GetPraxisUserByClientId(processGuide.ClientId);
                        assignedUsers = praxisUsers?.Select(pu => pu.ItemId).ToList() ?? new List<string>();
                    }
                    return string.Join(", ", assignedUsers.Select(GetPraxisUserNameByPraxisId));
            }

            return string.Empty;
        }

        private List<PraxisUser> GetPraxisUserByClientId(string clientId)
        {
            var collection = _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");
            var builder = Builders<PraxisUser>.Filter;
            var filter = builder.Ne(user => user.IsMarkedToDelete, true) &
                         builder.Not(builder.AnyEq("Roles", RoleNames.GroupAdmin)) &
                         builder.Exists(f => f.ClientList) &
                         builder.ElemMatch(user => user.ClientList, client => client.ClientId == clientId);
            return collection.Find(filter).ToList();
        }

        private List<PraxisUser> GetPraxisUserByRoles(string clientId, List<string> roles)
        {
            var users = _repository.GetItems<PraxisUser>(pu =>
                    pu.IsMarkedToDelete == false &&
                    pu.ClientList != null &&
                    pu.ClientList.Any(c => c.ClientId == clientId) &&
                    !(pu.Roles != null && pu.Roles.Contains(RoleNames.GroupAdmin)) &&
                    pu.Roles != null &&
                    pu.Roles.Any(r => roles.Contains(r)))
                .ToList() ?? new List<PraxisUser>();
            return users;
        }

        private string GetCompletedByUsersOfOpenItem(PraxisOpenItem openItem)
        {
            var usersCompleted = _repository.GetItems<PraxisOpenItemCompletionInfo>(c =>
                    c.PraxisOpenItemId == openItem.ItemId &&
                    !c.IsMarkedToDelete &&
                    c.Completion != null &&
                    c.Completion.Key == "done")?
                .Select(u => u.ReportedByUserId)
                .Distinct()
                .ToList() ?? new List<string>();
            return string.Join(", ", usersCompleted.Select(GetPraxisUserNameByPraxisId));
        }

        private string GetCompletedByUsersOfProcessGuide(PraxisProcessGuide processGuide)
        {
            var usersSubmitted = _repository.GetItems<PraxisProcessGuideAnswer>(c =>
                    !c.IsMarkedToDelete &&
                    c.ProcessGuideId == processGuide.ItemId &&
                    c.Answers != null &&
                    c.Answers.Any())?
                .Select(u => u.SubmittedBy)
                .Distinct()
                .ToList() ?? new List<string>();
            return string.Join(", ", usersSubmitted.Select(GetPraxisUserNameByPraxisId));
        }

        private string GetImplementationProposalEditHistory(List<CirsEditHistory> cirsEditHistory)
        {
            if(cirsEditHistory == null) return string.Empty;
            var remarksProperty = cirsEditHistory.FirstOrDefault(p => p.PropertyName == "ImplementationProposal");
            if (remarksProperty != null && remarksProperty?.CirsActivityPerformerModel != null)
            {
                var formattedRemarks = string.Join(",\n", remarksProperty.CirsActivityPerformerModel.Where(r => r.CurrentResponse != null).Select(ap => $"{GetPraxisUserNameByPraxisId(ap.PerformedBy)}: {ap.CurrentResponse}"));
                return formattedRemarks;
            }

            return string.Empty;
        }

        private string GetRemarksEditHistory(List<CirsEditHistory> cirsEditHistory, string remarks)
        {
            if (cirsEditHistory == null) return remarks;
            var remarksProperty = cirsEditHistory.FirstOrDefault(p => p.PropertyName == "Remarks");
            if (remarksProperty != null && remarksProperty?.CirsActivityPerformerModel != null)
            {
                var formattedRemarks = string.Join(",\n", remarksProperty.CirsActivityPerformerModel.Where(r => r.CurrentResponse != null).Select(ap => $"{GetPraxisUserNameByPraxisId(ap.PerformedBy)}: {ap.CurrentResponse}"));
                return formattedRemarks;
            }

            return string.Empty;
        }

        private string GetTranslatedDashboardName(string dashboardName)
        {
            return !string.IsNullOrWhiteSpace(dashboardName) ? GetTranslation(dashboardName) : "";
        }

        private string GetTranslatedAnonymous()
        {
            string value = "APP_CIRS_REPORT.ANONYMOUS";
            return !string.IsNullOrWhiteSpace(value) ? GetTranslation(value) : "";
        }
    }
}