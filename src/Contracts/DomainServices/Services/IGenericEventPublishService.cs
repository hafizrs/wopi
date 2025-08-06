using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

public interface IGenericEventPublishService
{
    void PublishDmsArtifactUsageReferenceEvent(EntityBase entity);
    void PublishDmsArtifactUsageReferenceDeleteEvent(EntityBase entity);
    void SendDmsArtifactUsageReferenceCreateEventToQueue(DmsArtifactUsageReferenceEventModel model);
}