using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport
{
    public class GeneratePraxisUserListReportForAllData : IPraxisUserListReportGenerate
    {
        private readonly ILogger<GeneratePraxisUserListReportForAllData> _logger;

        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisUserService _praxisUserService;
        private readonly IPraxisReportService _praxisReportService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IChangeLogService _changeLogService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();
        public GeneratePraxisUserListReportForAllData(
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisUserService praxisUserService,
            IPraxisReportService praxisReportService,
            ILogger<GeneratePraxisUserListReportForAllData> logger,
            ISecurityContextProvider securityContextProvider,
            IChangeLogService changeLogService
        )
        {
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisUserService = praxisUserService;
            _praxisReportService = praxisReportService;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _changeLogService = changeLogService;
        }

        public async Task<bool> GenerateReport(ExcelPackage excel, ExportPraxisUserListReportCommand command)
        {
            try
            {

                var user = _securityContextProvider.GetSecurityContext();
                var organizationId = string.Empty;
                bool isAdminBUser = false;
                if (!string.IsNullOrEmpty(user.UserId) && user.Roles.Contains(RoleNames.AdminB)
                  && !user.Roles.Contains(RoleNames.Admin) && !user.Roles.Contains(RoleNames.TaskController))
                {
                    var adminBUser = _praxisUserService.GetPraxisUserByUserId(user.UserId);
                    if (adminBUser != null && adminBUser.ClientList != null)
                    {
                        var client = adminBUser.ClientList.FirstOrDefault(c => !string.IsNullOrEmpty(c.ParentOrganizationId) && c.ParentOrganizationId == command.OrganizationId);
                        if (client != null)
                        {
                            organizationId = client.ParentOrganizationId;
                            isAdminBUser = true;
                        }
                    }
                }
                var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                _translatedStringsAsDictionary = _uilmResourceKeyService
                    .GetResourceValueByKeyName(PraxisUserListReport.TranslationKeys, command.LanguageKey);
                var worksheet = excel.Workbook.Worksheets.Add(command.ReportName);
                WriteHeader(worksheet, command.ReportName, reportDateString);

                var praxisUserList = (await _praxisUserService.GetPraxisUserListReportData(command.FilterString)).Results;
                var praxisUserListForReport = new List<PraxisUserForReport>();

                var clientIds = new List<string>();
                foreach (var praxisUser in praxisUserList)
                {
                    if (!isAdminBUser || (isAdminBUser && praxisUser.ClientList.FirstOrDefault(c => c.ParentOrganizationId == organizationId) != null))
                    {
                        if (IsAdminBUser(praxisUser.Roles.ToList()))
                        {
                            praxisUserListForReport.Add(
                            new PraxisUserForReport(
                                    praxisUser.DisplayName,
                                    "",
                                    null,
                                    GetRoleName(new List<string>() { RoleNames.AdminB }),
                                    praxisUser.DateOfJoining,
                                    null,
                                    true,
                                    praxisUser.Email,
                                    praxisUser.Phone,
                                    praxisUser.Active,
                                    praxisUser.Gender,
                                    praxisUser.DateOfBirth,
                                    praxisUser.Nationality,
                                    praxisUser.MotherTongue,
                                    praxisUser.OtherLanguage,
                                    praxisUser.AcademicTitle,
                                    praxisUser.WorkLoad,
                                    praxisUser.NumberOfChildren,
                                    praxisUser.Telephone,
                                    praxisUser.GlnNumber,
                                    praxisUser.ZsrNumber,
                                    praxisUser.KNumber));
                        }
                        foreach (var client in praxisUser.ClientList.OrderBy(c => c.ClientName))
                        {
                            var roles = GetRoleName(client.Roles.ToList());
                            clientIds.Add(client.ClientId);
                            praxisUserListForReport.Add(
                                new PraxisUserForReport(
                                    praxisUser.DisplayName,
                                    client.ClientName,
                                    client.Designation,
                                    roles,
                                    client.DateOfJoining,
                                    client.PhoneExtensionNumber,
                                    client.IsCreateProcessGuideEnabled,
                                    praxisUser.Email,
                                    praxisUser.Phone,
                                    praxisUser.Active,
                                    praxisUser.Gender,
                                    praxisUser.DateOfBirth,
                                    praxisUser.Nationality,
                                    praxisUser.MotherTongue,
                                    praxisUser.OtherLanguage,
                                    praxisUser.AcademicTitle,
                                    praxisUser.WorkLoad,
                                    praxisUser.NumberOfChildren,
                                    praxisUser.Telephone,
                                    praxisUser.GlnNumber,
                                    praxisUser.ZsrNumber,
                                    praxisUser.KNumber));
                        }
                    }
                }
                clientIds = clientIds.Distinct().ToList();
                await _praxisReportService.AddClientIdsToPraxisReport(command.ReportFileId, clientIds);
                if (isAdminBUser) await UpdatePermissionForUserPraxisReport(command.ReportFileId, clientIds);

                WriteUserList(worksheet, praxisUserListForReport);
                SetColumnSpecificStyle(worksheet);
                MergeSameUserRows(worksheet, praxisUserListForReport.Count);
                AddBorderToUserListTable(worksheet, praxisUserListForReport.Count + 1);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError( "Exception message: {Message}. Exception Details: {StackTrace}", e.Message, e.StackTrace);
            }

            return false;
        }

        private async Task UpdatePermissionForUserPraxisReport(string reportFileId, List<string> clientIds)
        {
            try
            {
                var rolesAllowedToRead = _praxisReportService.GetDynamicRolesForPraxisReportFromCLietIds(clientIds);
                var updateData = new Dictionary<string, object>
                {
                    { "RolesAllowedToRead", rolesAllowedToRead.ToArray() }
                };


                var filterBuilder = Builders<BsonDocument>.Filter;
                var updateFilters = filterBuilder.Eq("ReportFileId", reportFileId);

                await _changeLogService.UpdateChange("PraxisReport", updateFilters, updateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred. Exception message: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private static bool IsAdminBUser(List<string> roles)
        {
            return roles.Contains(RoleNames.AdminB);
        }

        private List<string> GetRoleName(List<string> roles)
        {
            var userRoles = new List<string>();
            foreach (var role in roles)
            {
                switch (role)
                {
                    case RoleNames.PowerUser:
                        userRoles.Add("Power User");
                        continue;
                    case RoleNames.Leitung:
                        userRoles.Add("Management");
                        continue;
                    case RoleNames.MpaGroup1:
                        userRoles.Add("EE Group 1");
                        continue;
                    case RoleNames.MpaGroup2:
                        userRoles.Add("EE Group 2");
                        continue;
                    case RoleNames.Admin:
                        userRoles.Add("Admin");
                        continue;
                    case RoleNames.AdminB:
                        userRoles.Add("Admin B");
                        continue;
                    case RoleNames.SystemAdmin:
                        userRoles.Add("System Admin");
                        continue;
                    default:
                        return new List<string>();
                }
            }
            return userRoles;
        }

        private void WriteHeader(ExcelWorksheet worksheet, string reportName, string dateString)
        {
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;

                worksheet.Cells[2, 1].Value = GetTranslation("DATE");
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;

                var columnIndex = 1;
                const int headerRowIndex = PraxisUserListReport.HeaderRowIndexForAllDataReport;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NAME");

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ORGANIZATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DESIGNATION");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ROLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DATE_OF_JOINING");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("INTERNAL_NUMBER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CAN_CREATE_PROCESS_GUIDE");

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("STATUS");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("CONTACT_INFO");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("EMAIL");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("GENDER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("DATE_OF_BIRTH");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NATIONALITY");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NATIVE_LANGUAGE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("OTHER_LANGUAGE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ACADEMIC_TITLE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("WORKLOAD");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NUMBER_OF_CHILDREN");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("TELEPHONE");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("GLN_NUMBER");
                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("ZSR_NUMBER");
                worksheet.Cells[headerRowIndex, columnIndex].Value = GetTranslation("K-NUMBER");

                //AddHeaderLogo(worksheet);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception message: {Message}. Exception Details: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        private void AddHeaderLogo(ExcelWorksheet worksheet)
        {
            const int logoStartColumn = PraxisUserListReport.ColumnsForAllDataReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, PraxisUserListReport.LogoSize, logoStartColumn, rqLatestLogo);
        }

        private void WriteUserList(ExcelWorksheet worksheet, List<PraxisUserForReport> praxisUsers)
        {
            try
            {
                const int startRow = PraxisUserListReport.HeaderRowIndexForAllDataReport + 1;
                for (var index = 0; index < praxisUsers.Count; index++)
                {
                    var praxisUser = praxisUsers[index];
                    var rowIndex = startRow + index;
                    var columnIndex = 1;

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.DisplayName;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.ClientName;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Designation;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Roles != null ? GetListToStringByNewLine(praxisUser.Roles) : "";
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.DateOfJoining.ToString("dd.MM.yyyy");
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.PhoneExtensionNumber;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedIsCreateProcessGuideEnabledValue(praxisUser.IsCreateProcessGuideEnabled);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedActiveValue(praxisUser.Active);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Phone;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Email ?? "";
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = GetTranslatedGender(praxisUser.Gender);
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.DateOfBirth.ToString("dd.MM.yyyy");
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Nationality;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.MotherTongue;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;    // trans
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.OtherLanguage != null ? GetListToStringByNewLine(praxisUser.OtherLanguage.ToList()) : "";
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;  // trans
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.AcademicTitle;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.WorkLoad;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.NumberOfChildren;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Telephone;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.GlnNumber;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.ZsrNumber;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex - 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[rowIndex, columnIndex].Value = praxisUser.KNumber;
                    worksheet.Cells[rowIndex, columnIndex].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[rowIndex, columnIndex].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Row(rowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Row(rowIndex).Height = praxisUser.Roles != null && praxisUser.Roles.Count > 0 ?
                        praxisUser.Roles.Count * PraxisUserListReport.RowHeight :
                        PraxisUserListReport.RowHeight;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in WriteUserList");
                _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private string GetListToStringByNewLine(List<string> values)
        {
            if (values != null)
            {
                return values.Count == 1 ? values[0] : string.Join('\n', values);
            }
            return "";
        }

        private void SetColumnSpecificStyle(ExcelWorksheet worksheet)
        {
            const int tableColumns = PraxisUserListReport.ColumnsForAllDataReport;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[PraxisUserListReport.HeaderRowIndexForAllDataReport, i];
                column.AutoFit();
                column.Style.WrapText = true;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(PraxisUserListReport.HeaderBackground);
                if (i == tableColumns)
                    column.Width = Math.Max(column.Width, 20);
            }
        }

        private void MergeSameUserRows(ExcelWorksheet worksheet, int praxisUserCount)
        {
            try
            {
                const int startRow = PraxisUserListReport.HeaderRowIndexForAllDataReport + 1;

                for (var rowIndex = startRow; rowIndex < startRow + praxisUserCount; rowIndex++)
                {
                    var firstRowForUser = rowIndex;

                    while (worksheet.Cells[rowIndex, 10].Value.Equals(worksheet.Cells[rowIndex + 1, 10].Value))
                        rowIndex++;

                    if (firstRowForUser == rowIndex) continue;

                    var userRange = worksheet.SelectedRange[firstRowForUser, 1, rowIndex, 1];
                    userRange.Merge = true;

                    for (int i = 8; i <= PraxisUserListReport.ColumnsForAllDataReport; i++)
                    {
                        worksheet.SelectedRange[firstRowForUser, i, rowIndex, i].Merge = true;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception message: {Message}. Exception Details: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        private void AddBorderToUserListTable(ExcelWorksheet worksheet, int tableRows)
        {
            const int tableColumns = PraxisUserListReport.ColumnsForAllDataReport;
            const int headerRowIndex = PraxisUserListReport.HeaderRowIndexForAllDataReport;

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

        private string GetTranslation(string key)
        {
            if (_translatedStringsAsDictionary.TryGetValue($"APP_USER_MANAGEMENT.{key}", out var translation))
            {
                return translation;
            }
            return key;
        }

        private string GetTranslatedGender(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
            { 
                return ""; 
            }
            string gender = GetTranslation(value.ToUpper());
            return gender == value.ToUpper() ? value : gender;
        }

        private string GetTranslatedIsCreateProcessGuideEnabledValue(bool value)
        {
            return value ? _translatedStringsAsDictionary["YES"] : _translatedStringsAsDictionary["NO"];
        }

        private string GetTranslatedActiveValue(bool value)
        {
            return value ? GetTranslation("ACTIVE") : GetTranslation("INACTIVE");
        }
    }
}