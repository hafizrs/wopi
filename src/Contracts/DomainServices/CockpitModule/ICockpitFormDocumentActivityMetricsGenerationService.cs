using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

public interface ICockpitFormDocumentActivityMetricsGenerationService
{
    Task OnCreateFormTodoGenerateActivityMetrics(PraxisOpenItem openItem);
    Task OnCreatePraxisProcessGuideFormGenerateActivityMetrics(PraxisProcessGuide guide);
    Task OnCreateEquipmentMaintenanceFormGenerateActivityMetrics(PraxisEquipmentMaintenance maintenance);
    Task OnFormFillGenerateActivityMetrics(string[] objectArtifactIds, string organizationId, string clientId, EntityBase entity);
    Task OnDeleteTaskRemoveSummaryFromActivityMetrics(List<string> taskItemIds, string entityName);
}