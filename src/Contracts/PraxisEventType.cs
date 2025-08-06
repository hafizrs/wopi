namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    public class PraxisEventType
    {
        protected PraxisEventType() { }
        public const string CirsReportEvent = "CirsReportEvent";
        public const string CirsAdminAssignedEvent = "CirsAdminAssigned";
        public const string LibraryAdminAssignedEvent = "LibraryAdminAssigned";
        public const string LibraryFileSharedEvent = "LibraryFileShared";
        public const string LibraryFolderSharedEvent = "LibraryFolderShared";
        public const string LibraryFolderTreeSharedEvent = "LibraryFolderTreeShared";
        public const string OrganizationCreatedEvent = "PraxisOrganization.Created";
        public const string OrganizationUpdatedEvent = "PraxisOrganization.Updated";
        public const string LibraryFormUpdateEvent = "LibraryFormUpdateEvent";
        public const string LibraryRightsUpdatedEvent = "LibraryRightsUpdated";
        public const string LibraryFileApprovedEvent = "LibraryFileApprovedEvent";
        public const string LibraryFileRenamedEvent = "LibraryFileRenamedEvent";
        public const string LibraryFileMovedEvent = "LibraryFileMovedEvent";
        public const string LibraryFileDeletedEvent = "LibraryFileDeleted";
        public const string LibraryFileUploadedEvent = "LibraryFileUploadedEvent";
        public const string LibraryFolderCreatedEvent = "LibraryFolderCreatedEvent";
        public const string MaintenanceMailSendEvent = "MaintenanceMailSendEvent";
        public const string DmsArtifactReapprovalEvent = "DmsArtifactReapprovalEvent";
        public const string LibraryFileEditedByOthersEvent = "LibraryFileEditedByOthersEvent";
        public const string DmsArtifactUsageReferenceEvent = "DmsArtifactUsageReferenceEvent";
        public const string DmsArtifactUsageReferenceDeleteEvent = "DmsArtifactUsageReferenceDeleteEvent";
        public const string UpdateCirsAssignedAdminForCockpitEvent = "UpdateCirsAssignedAdminForCockpitEvent";
        public const string SubscriptionRenewEvent = "SubscriptionRenewEvent";
        public const string SubscriptionExpiredEvent = "SubscriptionExpiredEvent";
        public const string PraxisTrainingQualificationPassedEvent = "PraxisTrainingQualificationPassedEvent";
        public const string PraxisRiqsShiftPlanCreatedFromSchedulerEvent = "PraxisRiqsShiftPlanCreatedFromSchedulerEvent";
        public const string RemoveCockpitTaskForShiftPlanSchedulerEvent = "RemoveCockpitTaskForShiftPlanSchedulerEvent";
        public const string CockpitTaskRemoveEvent = "CockpitTaskRemoveEvent";
        public const string AppLogRecordedEvent = "AppLogRecordedEvent";
        public const string PraxisGeneratedReportTemplatePdf = "PraxisGeneratedReportTemplatePdf";
        public const string OpenItemDeactivateEvent = "OpenItemDeactivateEvent";
    }
}
