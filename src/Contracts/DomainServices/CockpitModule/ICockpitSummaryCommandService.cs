using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule
{
    public interface ICockpitSummaryCommandService
    {
        Task CreateSummary(string itemId, string entityName, bool onUpdate = false);
        Task DeleteSummaryAsync(List<string> relatedEntityIds, CockpitTypeNameEnum relatedEntityName);
        Task UpdateCockpitSummary(string[] taskScheduleIds, string relatedEntityName);
        void DeleteCockpitSummaryByTaskSummaryId(string[] taskSummaryIds, string relatedEntityName);
        Task DiscardCockpitSummary(string[] relatedEntityIds);
        Task UpdateCirsAssignedAdmins(string dashboardPermissionId);
        Task<bool> DeleteCockpitDataByContext(DeleteCockpitDataContext context, List<string> itemIds);
        Task SyncSubmittedAnswer(string answerId, string entityName);
        Task UpdateSummeryForClonedProcessGuide(string itemId, string entityName);
        Task DeleteDataForClient(string clientId, string orgId = null);
    }
}