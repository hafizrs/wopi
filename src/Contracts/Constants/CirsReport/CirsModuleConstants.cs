using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport
{
    public static class CirsModuleConstants
    {
        public static Dictionary<string, string> PermissionKeys { get; set; } = new Dictionary<string, string>
        {
            { $"{CirsPermissionEnum.IS_ASSIGNED_ADMIN}", CirsPermissionValue.IsAssignedAdmin },
            { $"{CirsPermissionEnum.CAN_ASSIGN_ADMIN}", CirsPermissionValue.CanAssignAdmin },
            { $"{CirsPermissionEnum.CAN_CREATE_REPORT}", CirsPermissionValue.CanCreateReport },
            { $"{CirsPermissionEnum.CAN_EDIT_REPORT}", CirsPermissionValue.CanEditReport },
            { $"{CirsPermissionEnum.CAN_INPUT_EXTERNAL_OFFICE_EMAIL}", "CanInputExternalOfficeEmail" },
            { $"{CirsPermissionEnum.CAN_MOVE_REPORT_FOR_PUBLISHING}", "CanMoveReportForPublishing" },
            { $"{CirsPermissionEnum.CAN_SEE_ACTIVE_CARDS}", CirsPermissionValue.CanSeeActiveCards },
            { $"{CirsPermissionEnum.CAN_SEE_INACTIVE_CARDS}", CirsPermissionValue.CanSeeInactiveCards },
            { $"{CirsPermissionEnum.CAN_EDIT_PUBLISHED_REPORT}", "CanEditPublishedReport" },
            { $"{CirsPermissionEnum.CAN_GENERATE_EXCEL_REPORT}", CirsPermissionValue.CanGenerateExcelReport },
            { $"{CirsPermissionEnum.CAN_DELETE_REPORT}", CirsPermissionValue.CanDeleteReport },
            { $"{CirsPermissionEnum.CAN_VIEW_REPORT}", CirsPermissionValue.CanViewReport },
            { $"{CirsPermissionEnum.CAN_INACTIVE_REPORT}", CirsPermissionValue.CanInactiveReport },
            { $"{CirsPermissionEnum.CAN_REQUALIFY_REPORT}", "CanRequalifyReport" },
            { $"{CirsPermissionEnum.HIDE_TO_BE_APPROVED_COLUMN}", CirsPermissionValue.HideToBeApprovedColumn }
        };

        public static List<string> CirsReportDataPermissionKeys { get; set; } = new List<string>
        {
            CirsPermissionValue.CanCloneReport,
            CirsPermissionValue.CanViewReport,
            CirsPermissionValue.CanEditReport,
            CirsPermissionValue.CanInactiveReport,
            CirsPermissionValue.CanMoveReport,
            CirsPermissionValue.CanCreateToDo,
            CirsPermissionValue.CanCreateProcessGuide
        };
    }

    public static class CirsPermissionValue
    {

        public const string IsAssignedAdmin = "IsAssignedAdmin";
        public const string CanAssignAdmin = "CanAssignAdmin";
        public const string CanCreateReport = "CanCreateReport";
        public const string CanEditReport = "CanEditReport";
        public const string CanSeeActiveCards = "CanSeeActiveCards";
        public const string CanSeeInactiveCards = "CanSeeInactiveCards";
        public const string CanGenerateExcelReport = "CanGenerateExcelReport";
        public const string CanDeleteReport = "CanDeleteReport";
        public const string CanViewReport = "CanViewReport";
        public const string CanInactiveReport = "CanInactiveReport";
        public const string CanCloneReport = "CanCloneReport";
        public const string CanMoveReport = "CanMoveReport";
        public const string CanCreateToDo = "CanCreateToDo";
        public const string CanCreateProcessGuide = "CanCreateProcessGuide";
        public const string HideToBeApprovedColumn = "HideToBeApprovedColumn";
    }
}
