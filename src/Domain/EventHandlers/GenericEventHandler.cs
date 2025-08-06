using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.SubscriptionEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingAnswerEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class GenericEventHandler : IEventHandler<GenericEvent, bool>
    {
        public GenericEventHandler() {}

        [Invocable]
        public bool Handle(GenericEvent @event)
        {
            if (string.IsNullOrWhiteSpace(@event.JsonPayload))
            {
                return false;
            }

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return eventHandler.HandleAsync(@event).Result;
            }

            return false; 
        }

        [Invocable]
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            if (string.IsNullOrWhiteSpace(@event.JsonPayload))
            {
                return false;
            }

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return await eventHandler.HandleAsync(@event);
            }

            return false;
        }

        private IBaseEventHandlerAsync<GenericEvent>? EventHandler(string eventType)
        {
            return eventType switch
            {
                PraxisEventType.CirsReportEvent => ServiceLocator.GetService<CirsReportEventHandler>(),
                PraxisEventType.CirsAdminAssignedEvent => ServiceLocator.GetService<CirsAdminAssignedEventHandler>(),
                PraxisEventType.OrganizationCreatedEvent => ServiceLocator.GetService<PraxisOrganizationCreateUpdateEventHandler>(),
                PraxisEventType.OrganizationUpdatedEvent => ServiceLocator.GetService<PraxisOrganizationCreateUpdateEventHandler>(),
                PraxisEventType.LibraryFileSharedEvent => ServiceLocator.GetService<LibraryFileSharedEventHandler>(),
                PraxisEventType.LibraryFolderSharedEvent => ServiceLocator.GetService<LibraryFolderSharedEventHandler>(),
                PraxisEventType.LibraryFolderTreeSharedEvent => ServiceLocator.GetService<LibraryFolderTreeSharedEventHandler>(),
                PraxisEventType.LibraryFormUpdateEvent => ServiceLocator.GetService<LibraryFormUpdateEventHandler>(),
                PraxisEventType.LibraryRightsUpdatedEvent => ServiceLocator.GetService<LibraryRightsUpdatedEventHandler>(),
                PraxisEventType.LibraryFileApprovedEvent => ServiceLocator.GetService<LibraryFileApprovedEventHandler>(),
                PraxisEventType.LibraryFileRenamedEvent => ServiceLocator.GetService<LibraryFileRenamedEventHandler>(),
                PraxisEventType.LibraryFileMovedEvent => ServiceLocator.GetService<LibraryFileMovedEventHandler>(),
                PraxisEventType.LibraryFileDeletedEvent => ServiceLocator.GetService<LibraryFileDeletedEventHandler>(),
                PraxisEventType.MaintenanceMailSendEvent => ServiceLocator.GetService<MaintenanceMailSendEventEventHandler>(),
                PraxisEventType.DmsArtifactReapprovalEvent => ServiceLocator.GetService<DmsArtifactReapprovalEventHandler>(),
                PraxisEventType.LibraryFileEditedByOthersEvent => ServiceLocator.GetService<LibraryFileEditedByOthersEventHandler>(),
                PraxisEventType.DmsArtifactUsageReferenceEvent => ServiceLocator.GetService<DmsArtifactUsageReferenceEventHandler>(),
                PraxisEventType.DmsArtifactUsageReferenceDeleteEvent => ServiceLocator.GetService<DmsArtifactUsageReferenceDeletionEventHandler>(),
                PraxisEventType.UpdateCirsAssignedAdminForCockpitEvent => ServiceLocator.GetService<UpdateCirsAssignedAdminForCockpitEventHandler>(),
                PraxisEventType.SubscriptionRenewEvent => ServiceLocator.GetService<SubscriptionRenewEventHandler>(),
                PraxisEventType.SubscriptionExpiredEvent => ServiceLocator.GetService<SubscriptionExpiredEventHandler>(),
                PraxisEventType.PraxisTrainingQualificationPassedEvent => ServiceLocator.GetService<PraxisTrainingQualificationPassedEventHandler>(),
                PraxisEventType.PraxisRiqsShiftPlanCreatedFromSchedulerEvent => ServiceLocator.GetService<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler>(),
                PraxisEventType.RemoveCockpitTaskForShiftPlanSchedulerEvent => ServiceLocator.GetService<RemoveCockpitTaskForShiftPlanSchedulerEventHandler>(),
                PraxisEventType.CockpitTaskRemoveEvent => ServiceLocator.GetService<CockpitTaskRemoveEventHandler>(),
                PraxisEventType.LibraryFileUploadedEvent => ServiceLocator.GetService<DmsFileUploadedEventHandler>(),
                PraxisEventType.LibraryFolderCreatedEvent => ServiceLocator.GetService<DmsFolderCreatedEventHandler>(),
                PraxisEventType.AppLogRecordedEvent => ServiceLocator.GetService<AppLogRecordedEventHandler>(),
                PraxisEventType.PraxisGeneratedReportTemplatePdf => ServiceLocator.GetService<PraxisGeneratedReportTemplatePdfEventHandler>(),
                PraxisEventType.OpenItemDeactivateEvent => ServiceLocator.GetService<OpenItemDeactivateEventHandler>(),
                _ => null
            };
        }

    }
}
