using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisFormService
    {     
    
        void UpdatePraxisForm(string EntityName,string Id, IEnumerable<string> formIds, string ClientId);
        List<PraxisForm> GetAllPraxisForm(string formId);
        void AddRowLevelSecurity(string itemId, string clientId);
        void AddRowLevelSecurity(string itemId, List<string> clientIds, List<string> orgIds);
        PraxisForm AddRowLevelSecurityForPraxisFormUpdate(PraxisForm praxisForm, string itemId, string clientId);
        Task<EntityQueryResponse<PraxisForm>> GetDeveloperReportData(string filter, string sort);
        void UpdatePraxisFormRelatedData(PraxisForm praxisForm, string itemId);
        Task DeletePraxisFormFilesAsync(PraxisForm form);
        Task MarkedToDeletePraxisFormIncludingDependantEntites(string formId);
        PraxisForm GetPraxisFromById(string formId);

        Task<EntityQueryResponse<PraxisFormWithPermission>> GetPraxisFormDetailWithPermission(string formId);
        Task<EntityQueryResponse<PraxisFormWithPermission>> GetPraxisFormForDepartment(GetPraxisFormForDepartmentQuery query);

    }
}
