namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public enum ObjectArtifactMetaDataKeyEnum
    {
        DEPARTMENT_ID,
        VERSION,
        FILE_TYPE,
        KEYWORDS,
        STATUS,
        APPROVAL_STATUS,
        ASSIGNED_ON,
        IS_DRAFT,
        FORM_TYPE,
        IS_ESIGN_REQUIRED,
        IS_2FA_ENABLED,
        IS_A_ORIGINAL_ARTIFACT,
        ORIGINAL_ARTIFACT_ID,
        FORM_FILL_STATUS,
        REAPPROVE_INTERVAL,
        NEXT_REAPPROVE_DATE,
        REAPPROVE_PROCESS_START,
        REAPPROVE_PROCESS_START_DATE,
        IS_SECRET_ARTIFACT,
        IS_UPLOADED_FROM_WEB,
        IS_USED_IN_ANOTHER_ENTITY,
        ARTIFACT_USAGE_REFERENCE_COUNTER,
        DEPARTMENT_ID_FOR_SUBSCRIPTION,
        IS_STANDARD_FILE,
        IS_CHILD_STANDARD_FILE,
        INTERFACE_MIGRATION_SUMMARY_ID,
        MANUAL_FILE_UPLOAD_STATUS,
        IS_NOTIFIED_TO_COCKPIT,
        APPROVED_DATE,
        DOCUMENT_EDITED_DATE,
        IS_ORG_LEVEL,
        PROCESSED_ORIGINAL_HTML_FILE_ID
        

    }

    public enum ObjectArtifactMetaDataKeyTypeEnum
    {
        STRING,
        DATETIME
    }

    public enum LibraryFileStatusEnum
    {
        INACTIVE = 0,
        ACTIVE = 1
    }

    public enum LibraryFileApprovalStatusEnum
    {
        PENDING = 1,
        APPROVED = 2,
        PARTIALLY_APPROVED = 3,
        REAPPROVE = 4
    }

    public enum LibraryFileTypeEnum
    {
        DOCUMENT = 1,
        FORM = 2,
        IMAGE = 3,
        VIDEO = 4,
        EXCELS = 5,
        PPT = 6,
        PDF = 7,
        OTHER = 8,
        EQUIPMENT_MANUAL = 9
    }

    public enum LibraryFormTypeEnum
    {
        NORMAL = 1,
        CONFIDENTIAL = 2,
        GENERAL = 3
    }

    public enum LibraryBooleanEnum
    {
        FALSE = 0,
        TRUE = 1
    }

    public enum LibraryViewModeEnum
    {
        ALL,
        APPROVAL_VIEW,
        APPROVED,
        DOCUMENT,
        FORM,
        MANUAL
    }

    public enum ObjectArtifactPermissions
    {
        VIEW,
        DOWNLOAD,
        RENAME,
        CHANGE_COLOR,
        EDIT,
        ACTIVE_INACTIVE_TOGGLE,
        APPROVE,
        EDIT_DOC,
        FILL_FORM,
        VERSION_HISTORY,
        SHARE,
        MOVE,
        VIEW_ONLY_ACCESS_CONTROL,
        EDIT_ACCESS_CONTROL,
        ADD_CHILDREN,
        REMOVE
    }

    public enum LibraryGroupType
    {
        MAIN_GROUP,
        SUB_GROUP,
        SUB_SUB_GROUP
    }

    public enum ObjectArtifactEvent
    {
        FILE_UPLOADED,
        FILE_APPROVED,
        FOLDER_UPLOADED,
        FORM_RESPONSE_DRAFTED,
        FORM_RESPONSE_SAVED,
        DOCUMENT_DRAFTED,
        DRAFTED_DOCUMENT_SAVED
    }

    public enum FormFillStatus
    {
        DRAFT = 1,
        COMPLETE = 2,
        PENDING_SIGNATURE = 3
    }

    public enum ArtifactActivityName
    {
        FORM_RESPONSE_COMPLETED,
        APPROVAL
    }

    public enum LibraryAssignedMemberType
    {
        ASSIGNED_TO = 1,
        FORM_FILLED_BY = 2,
        FORM_FILL_PENDING_BY = 3
    }
}
