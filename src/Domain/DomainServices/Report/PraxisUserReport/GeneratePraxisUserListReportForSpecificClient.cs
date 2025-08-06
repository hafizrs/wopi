using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport
{
    public class GeneratePraxisUserListReportForSpecificClient : IPraxisUserListReportGenerate
    {
        private readonly ILogger<GeneratePraxisUserListReportForSpecificClient> _logger;

        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private readonly IPraxisUserService _praxisUserService;
        private readonly IPraxisReportService _praxisReportService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new Dictionary<string, string>();

        public GeneratePraxisUserListReportForSpecificClient(
            IUilmResourceKeyService uilmResourceKeyService,
            IPraxisUserService praxisUserService,
            IPraxisReportService praxisReportService,
            ILogger<GeneratePraxisUserListReportForSpecificClient> logger
        )
        {
            _uilmResourceKeyService = uilmResourceKeyService;
            _praxisUserService = praxisUserService;
            _praxisReportService = praxisReportService;
            _logger = logger;
        }

        public async Task<bool> GenerateReport(ExcelPackage excel, ExportPraxisUserListReportCommand command)
        {
            try
            {
                var reportDateString = DateTime.Today.ToString("dd.MM.yyyy");
                _translatedStringsAsDictionary = _uilmResourceKeyService
                    .GetResourceValueByKeyName(PraxisUserListReport.TranslationKeys, command.LanguageKey);
                var worksheet = excel.Workbook.Worksheets.Add(command.ReportName);

                WriteHeader(worksheet, command.ReportName, reportDateString, command.ClientName);
                var praxisUserList = (await _praxisUserService.GetPraxisUserListReportData(command.FilterString))
                    .Results;
                var praxisUserListForReport = new List<PraxisUserForReport>();
                foreach (var praxisUser in praxisUserList)
                {
                    var client = praxisUser.ClientList.
                        FirstOrDefault(clientInfo => clientInfo.ClientId == command.ClientId);
                    if (client == null) continue;
                    praxisUserListForReport.Add(
                        new PraxisUserForReport(
                            praxisUser.DisplayName,
                            client.ClientName,
                            client.Designation,
                            GetRoleName(client.Roles.ToList()),
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

                WriteUserList(worksheet, praxisUserListForReport);

                SetColumnSpecificStyle(worksheet);
                AddBorderToUserListTable(worksheet, praxisUserListForReport.Count + 1);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception message: {Message}. Exception Details: {StackTrace}", e.Message, e.StackTrace);
            }

            return false;
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
                    default:
                        return new List<string>();
                }
            }
            return userRoles;
        }

        private void WriteHeader(ExcelWorksheet worksheet, string reportName, string dateString, string clientName)
        {
            try
            {
                worksheet.Cells[1, 1].Value = GetTranslation("REPORT_NAME");
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Value = reportName;

                worksheet.Cells[2, 1].Value = GetTranslation("DATE");
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = dateString;

                worksheet.Cells[3, 1].Value = GetTranslation("ORGANIZATION");
                worksheet.Cells[3, 1].Style.Font.Bold = true;
                worksheet.Cells[3, 2].Value = clientName;

                var columnIndex = 1;
                const int headerRowIndex = PraxisUserListReport.HeaderRowIndexForSpecificClientReport;
                worksheet.Row(headerRowIndex).Style.Font.Bold = true;
                worksheet.Row(headerRowIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Row(headerRowIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                worksheet.Cells[headerRowIndex, columnIndex++].Value = GetTranslation("NAME");

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
            const int logoStartColumn = PraxisUserListReport.ColumnsForSpecificClientReport;
            // [firstRow, firstColumn, lastRow, LastColumn]
            worksheet.Cells[1, logoStartColumn, 2, logoStartColumn].Merge = true;
            _praxisReportService.AddLogoInExcelReport(worksheet, PraxisUserListReport.LogoSize, logoStartColumn, rqLatestLogo);
        }

        private void WriteUserList(ExcelWorksheet worksheet, List<PraxisUserForReport> praxisUsers)
        {
            try
            {
                const int startRow = PraxisUserListReport.HeaderRowIndexForSpecificClientReport + 1;
                for (var index = 0; index < praxisUsers.Count; index++)
                {
                    var praxisUser = praxisUsers[index];
                    var rowIndex = startRow + index;
                    var columnIndex = 1;
                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.DisplayName;
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

                    worksheet.Cells[rowIndex, columnIndex++].Value = praxisUser.Email;
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
                _logger.LogError(ex, "Exception in WriteUserList. Exception Message: {Message}. Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
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
            const int tableColumns = PraxisUserListReport.ColumnsForSpecificClientReport;
            for (var i = 1; i <= tableColumns; i++)
            {
                var column = worksheet.Column(i);
                var headerCell = worksheet.Cells[PraxisUserListReport.HeaderRowIndexForSpecificClientReport, i];
                column.AutoFit();
                column.Style.WrapText = true;
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(PraxisUserListReport.HeaderBackground);
                if (i == tableColumns)
                    column.Width = Math.Max(column.Width, 20);
            }
        }

        private void AddBorderToUserListTable(ExcelWorksheet worksheet, int tableRows)
        {
            const int tableColumns = PraxisUserListReport.ColumnsForSpecificClientReport;
            const int headerRowIndex = PraxisUserListReport.HeaderRowIndexForSpecificClientReport;

            for (var i = 1; i <= 3; i++)
            {
                for (var j = 1; j <= 2; j++)
                {
                    worksheet.Cells[i, j].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }


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
            return _translatedStringsAsDictionary[$"APP_USER_MANAGEMENT.{key}"];
        }

        private string GetTranslatedGender(string value)
        {
            return !string.IsNullOrWhiteSpace(value) ? GetTranslation(value.ToUpper()) : "";
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