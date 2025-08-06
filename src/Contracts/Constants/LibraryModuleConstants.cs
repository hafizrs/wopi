using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class LibraryModuleConstants
    {
        public static Dictionary<string, string> ObjectArtifactMetaDataKeys { get; set; } = new Dictionary<string, string>
        {
            { $"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID}", "DepartmentId" },
            { $"{ObjectArtifactMetaDataKeyEnum.VERSION}", "Version" },
            { $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}", "FileType" },
            { $"{ObjectArtifactMetaDataKeyEnum.KEYWORDS}", "Keywords" },
            { $"{ObjectArtifactMetaDataKeyEnum.STATUS}", "Status" },
            { $"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}", "ApprovalStatus" },
            { $"{ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON}", "AssignedOn" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_DRAFT}", "IsDraft" },
            { $"{ObjectArtifactMetaDataKeyEnum.FORM_TYPE}", "FormType" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_ESIGN_REQUIRED}", "IsEsignRequired" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_2FA_ENABLED}", "Is2FaEnabled" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT}", "IsAOriginalArtifact" },
            { $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}", "OriginalArtifactId" },
            { $"{ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS}", "FormFillStatus" },
            { $"{ObjectArtifactMetaDataKeyEnum.REAPPROVE_INTERVAL}", "ReapproveInterval" },
            { $"{ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE}", "NextReapproveDate" },
            { $"{ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START}", "ReapproveProcessStart" },
            { $"{ObjectArtifactMetaDataKeyEnum.REAPPROVE_PROCESS_START_DATE}", "ReapproveProcessStartDate" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_SECRET_ARTIFACT}", "IsSecretArtifact" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB}", "IsUploadedFromWeb" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_USED_IN_ANOTHER_ENTITY}", "IsUsedInAnotherEntity" },
            { $"{ObjectArtifactMetaDataKeyEnum.ARTIFACT_USAGE_REFERENCE_COUNTER}", "ArtifactUsageReferenceCounter" },
            { $"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID_FOR_SUBSCRIPTION}", "DepartmentIdForSubscription" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE}", "IsStandardFile" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE}", "IsChildStandardFile" },
            { $"{ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID}", "InterfaceMigrationSummeryId" },
            { $"{ObjectArtifactMetaDataKeyEnum.MANUAL_FILE_UPLOAD_STATUS}", "ManualFileUploadStatus" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_NOTIFIED_TO_COCKPIT}", "IsNotifiedToCockpit" },
            { $"{ObjectArtifactMetaDataKeyEnum.APPROVED_DATE}", "ApprovedDate" },
            { $"{ObjectArtifactMetaDataKeyEnum.DOCUMENT_EDITED_DATE}", "DocumentEditedDate" },
            { $"{ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL}", "IsOrgLevel" },
            { $"{ObjectArtifactMetaDataKeyEnum.PROCESSED_ORIGINAL_HTML_FILE_ID}", "ProcessOriginalHtmlFileId" }
        };

        public static Dictionary<string, string> ObjectArtifactMetaDataKeyTypes { get; set; } = new Dictionary<string, string>
        {
            { $"{ObjectArtifactMetaDataKeyTypeEnum.STRING}", "string" },
            { $"{ObjectArtifactMetaDataKeyTypeEnum.DATETIME}", "datetime" }
        };

        public static List<KeyValueModel> LibraryViewModeList { get; set; } = new List<KeyValueModel>()
        {
            new KeyValueModel()
            {
                Key = $"{LibraryViewModeEnum.ALL}",
                Value = "all"
            },
            new KeyValueModel()
            {
                Key = $"{LibraryViewModeEnum.APPROVAL_VIEW}",
                Value = "approval-view"
            },
            new KeyValueModel()
            {
                Key = $"{LibraryViewModeEnum.DOCUMENT}",
                Value = "document"
            },
            new KeyValueModel()
            {
                Key = $"{LibraryViewModeEnum.FORM}",
                Value = "form"
            },
            new KeyValueModel()
            {
                Key = $"{LibraryViewModeEnum.MANUAL}",
                Value = "manual"
            },
        };

        public static Dictionary<string, string> ObjectArtifactStatusValueLanguageMap { get; set; } = new Dictionary<string, string>
        {
            { ((int)LibraryFileStatusEnum.INACTIVE).ToString(), $"{LibraryFileStatusEnum.INACTIVE}" },
            { ((int)LibraryFileStatusEnum.ACTIVE).ToString(), $"{LibraryFileStatusEnum.ACTIVE}" }
        };

        public static Dictionary<string, string> ObjectArtifactApprovalStatusValueLanguageMap { get; set; } = new Dictionary<string, string>
        {
            { ((int)LibraryFileApprovalStatusEnum.PENDING).ToString(), $"{LibraryFileApprovalStatusEnum.PENDING}" },
            { ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString(), $"{LibraryFileApprovalStatusEnum.APPROVED}" },
            { ((int)LibraryFileApprovalStatusEnum.PARTIALLY_APPROVED).ToString(), $"{LibraryFileApprovalStatusEnum.PARTIALLY_APPROVED}" }
        };

        public static Dictionary<string, string> LibraryViewModeFilterPropertyMap { get; set; } = new Dictionary<string, string>
        {
            { $"{LibraryViewModeEnum.APPROVED}", "MetaData.ApprovalStatus.Value" },
            { $"{LibraryViewModeEnum.APPROVAL_VIEW}", "MetaData.ApprovalStatus.Value" },
            { $"{LibraryViewModeEnum.DOCUMENT}", "MetaData.FileType.Value" },
            { $"{LibraryViewModeEnum.FORM}", "MetaData.FileType.Value" }
        };

        public static Dictionary<string, string> LibraryViewModeFilterValueMap { get; set; } = new Dictionary<string, string>
        {
            { $"{LibraryViewModeEnum.APPROVED}", ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString() },
            { $"{LibraryViewModeEnum.DOCUMENT}", ((int)LibraryFileTypeEnum.DOCUMENT).ToString() },
            { $"{LibraryViewModeEnum.FORM}", ((int)LibraryFileTypeEnum.FORM).ToString() }
        };

        public static Dictionary<string, string> LibraryMetaDataFilterPropertyMap { get; set; } = new Dictionary<string, string>
        {
            { $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}", "MetaData.FileType.Value" }
        };

        public static List<string> LibraryViewCommonResponseFields = new List<string>
        {
            nameof(RiqsObjectArtifact.ItemId),
            nameof(RiqsObjectArtifact.Tags),
            nameof(RiqsObjectArtifact.FileStorageId),
            nameof(RiqsObjectArtifact.WorkSpaceId),
            nameof(RiqsObjectArtifact.WorkSpaceName),
            nameof(RiqsObjectArtifact.StorageAreaId),
            nameof(RiqsObjectArtifact.OrganizationId),
            nameof(RiqsObjectArtifact.Name),
            nameof(RiqsObjectArtifact.ParentId),
            nameof(RiqsObjectArtifact.ParentName),
            nameof(RiqsObjectArtifact.OwnerId),
            nameof(RiqsObjectArtifact.OwnerName),
            nameof(RiqsObjectArtifact.ArtifactType),
            nameof(RiqsObjectArtifact.Extension),
            nameof(RiqsObjectArtifact.Color),
            nameof(RiqsObjectArtifact.FileSizeInByte),
            nameof(RiqsObjectArtifact.Description),
            nameof(RiqsObjectArtifact.MetaData),
            nameof(RiqsObjectArtifact.SharedOrganizationList)
        };

        public static List<string> OtherNodulePdfFormResponseFields = new List<string>
        {
            nameof(RiqsObjectArtifact.ItemId),
            nameof(RiqsObjectArtifact.FileStorageId),
            nameof(RiqsObjectArtifact.OrganizationId),
            nameof(RiqsObjectArtifact.Name),
            nameof(RiqsObjectArtifact.Extension),
            nameof(RiqsObjectArtifact.FileSizeInByte)
        };

        public static List<string> FolderUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.RENAME}",
            $"{ObjectArtifactPermissions.MOVE}",
            $"{ObjectArtifactPermissions.CHANGE_COLOR}",
            $"{ObjectArtifactPermissions.SHARE}",
            $"{ObjectArtifactPermissions.REMOVE}"
        };

        public static List<string> FileUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}",
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.RENAME}",
            $"{ObjectArtifactPermissions.EDIT}",
            $"{ObjectArtifactPermissions.MOVE}",
            $"{ObjectArtifactPermissions.EDIT_DOC}",
            $"{ObjectArtifactPermissions.FILL_FORM}",
            $"{ObjectArtifactPermissions.VERSION_HISTORY}",
            $"{ObjectArtifactPermissions.SHARE}",
            $"{ObjectArtifactPermissions.REMOVE}"
        };

        public static List<string> ApprovedFileUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}",
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.RENAME}",
            $"{ObjectArtifactPermissions.EDIT}",
            $"{ObjectArtifactPermissions.MOVE}",
            $"{ObjectArtifactPermissions.EDIT_DOC}",
            $"{ObjectArtifactPermissions.FILL_FORM}",
            $"{ObjectArtifactPermissions.VERSION_HISTORY}",
            $"{ObjectArtifactPermissions.SHARE}",
            $"{ObjectArtifactPermissions.REMOVE}"
        };

        public static List<string> PendingFileUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.APPROVE}",
            $"{ObjectArtifactPermissions.REMOVE}",
            $"{ObjectArtifactPermissions.EDIT}",
        };


        public static List<string> DocumentUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}",
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.RENAME}",
            $"{ObjectArtifactPermissions.EDIT}",
            $"{ObjectArtifactPermissions.MOVE}",
            $"{ObjectArtifactPermissions.EDIT_DOC}",
            $"{ObjectArtifactPermissions.VERSION_HISTORY}",
            $"{ObjectArtifactPermissions.SHARE}",
            $"{ObjectArtifactPermissions.REMOVE}"
        };

        public static List<string> FormUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}",
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.RENAME}",
            $"{ObjectArtifactPermissions.EDIT}",
            $"{ObjectArtifactPermissions.MOVE}",
            $"{ObjectArtifactPermissions.FILL_FORM}",
            $"{ObjectArtifactPermissions.SHARE}",
            $"{ObjectArtifactPermissions.REMOVE}",
            $"{ObjectArtifactPermissions.VERSION_HISTORY}",
        };

        public static List<string> FormResponseUserPermissions = new List<string>
        {
            $"{ObjectArtifactPermissions.VIEW}",
            $"{ObjectArtifactPermissions.DOWNLOAD}",
            $"{ObjectArtifactPermissions.FILL_FORM}",
            $"{ObjectArtifactPermissions.REMOVE}"
        };

        public static Dictionary<string, string> StaticRoleDynamicRolePrefixMap { get; set; } = new Dictionary<string, string>
        {
            { RoleNames.Organization_Read_Dynamic, RoleNames.Organization_Read_Dynamic },
            { RoleNames.PowerUser, RoleNames.PowerUser_Dynamic },
            { RoleNames.Leitung, RoleNames.Leitung_Dynamic },
            { RoleNames.MpaGroup1, RoleNames.MpaGroup_Dynamic},
            { RoleNames.MpaGroup2, RoleNames.MpaGroup_Dynamic}
        };

        public static int LibraryAssigneeSummaryLimit = 3;
        public static int LibraryPageLimit = 100;
    }
}
