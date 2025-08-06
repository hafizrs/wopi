using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.PraxisOpenItem;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisOpenItemService
    {
        PraxisOpenItemConfig GetPraxisOpenItemConfig(string itemId);
        PraxisOpenItem GetPraxisPraxisOpenItem(string itemId);
        void UpdatePraxisOpenItemConfig(string itemId);
        List<PraxisOpenItemConfig> GetAllPraxisOpenItemConfig();
        List<PraxisOpenItem> GetAllPraxisOpenItem();
        Task<List<PraxisOpenItemResponseRecord>> GetPraxisOpenItems(string filter, string sort, int pageNumber, int pageSize);
        void AddPraxisOpenItemRowLevelSecurity(string itemId, string clientId);
        void AddPraxisOpenItemConfigRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        List<string> GetNotCompletedMembers(string ItemId,List<string> controlledMembers);
        Task<QueryCompletionResponse> GetOpenItemCompletionDetails(GetCompletionListQuery getCompletion);
        Task<EntityQueryResponse<PraxisOpenItem>> GetPraxisOpenReportData(string filter, string sort);
        void UpdatePraxisOpenItemCompletionStatus(PraxisOpenItem praxisOpenItem, PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo, bool isUpdate);
        public Task PopulateOverAllCompletionStatus(string filterString = "{}");
        Task UpdateOpenItemLibraryFormResponse(ObjectArtifact artifact);
        void ProcessEmailForAssignedUsersCompletion(PraxisOpenItem praxisOpenItem, PraxisOpenItemCompletionInfo praxisOpenItemCompletionInfo);
        IEnumerable<string> GetAggregatedControlledMembers(PraxisOpenItem openItem);
        Task<long> GetOpenItemDocumentCount(string filter);
        Task<List<PraxisOpenItem>> GetProjectedOpenItemsWithSpecificTraining(string trainingId);
        Task UpdateTaskForToDo(dynamic payload);
    }
}
