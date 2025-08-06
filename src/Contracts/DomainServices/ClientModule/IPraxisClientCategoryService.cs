using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisClientCategoryService
    {
        Task<PraxisClientCategory> GetClientCategory(string itemId);
        Task<List<PraxisClientCategory>> GetClientCategories(string clientId);
        Task AddOrUpdateControlMembers(string itemId, List<PraxisUser> controlledUsers, List<PraxisUser> controllingUsers);
        void AddRowLevelSecurity(string itemId, string clientId);
        void RemoveRowLevelSecurity(string clientId);
        Task<EntityQueryResponse<PraxisClientCategory>> GetCategoryReportData(string filter, string sort);
        Task<List<DepartmentWiseCategoriesResponse>> GetDepartmentWiseCategories(GetDepartmentWiseCategoriesQuery query);
        Task CloneDepartmentCategories(CloneDepartmentCategoriesCommand command, CommandResponse response);
    }
}
