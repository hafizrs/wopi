namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public enum CockpitDocumentActivityEnum
    {
        DOCUMENTS_ASSIGNED,
        DOCUMENTS_EDITED_BY_OTHERS,
        PENDING_FORMS_TO_SIGN,
        DOCUMENTS_TO_APPROVE,
        DOCUMENTS_TO_REAPPROVE
    }

    public enum CockpitResponseActivityEnum
    {
        PENDING_READ_CONFIRMATIONS = 1,
        NEW_FILES_PENDING_APPROVALS = 2,
        PENDING_RE_APPROVALS = 3,
    }
}
