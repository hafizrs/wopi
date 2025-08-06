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
    public interface IPraxisClientService
    {
        Task<PraxisClient> GetPraxisClient(string itemId);
        void CreateDynamicRoles(string clientId);
        void AddRowLevelSecurity(string clientId);
        void AddFeatureRoleMapForAddingSupplierAndCategory(string clientId);
        void RemoveRowLevelSecurity(string clientId);
        Task<EntityQueryResponse<PraxisClient>> GetOwnPraxisClientList(string loggedInPraxisUserId, bool forProcessGuideForm);
        Task<bool> UserCreateCountDecrement(string clientId);
        Task<bool> UserCreateCountIncrement(string clientId);
        Task<bool> UpdateClientPaymentUserData(string clientId, string praxisUserId, string designation);
        bool AssignPaymentUserRole(string praxisUserId, string clientId);
        bool UnAssignPaymentUserRole(string praxisUserId, string clientId);
        bool DeletePraxisUserAdditionalInfo(string clientId, string infoTitleItemId);
        Task<bool> UpdateDepartmentUserCount(string departmentId, bool isUserAdded);
        Task<List<DepartmentWiseSuppliersResponse>> GetDepartmentWiseSuppliers(GetDepartmentWiseSuppliersQuery query);
        Task CloneDepartmentSuppliers(CloneDepartmentSuppliersCommand command, CommandResponse response);
        Task<List<DepartmentWiseUserAdditionalInfosResponse>> GetDepartmentWiseUserAdditionalInfos(GetDepartmentWiseUserAdditionalInfosQuery query);
        Task CloneDepartmentUserAdditionalInfos(CloneDepartmentUserAdditionalInfosCommand command, CommandResponse response);
        Task<bool> UpdateClientSubscriptionRelatedData(string clientId, double additionalStorage = 0, double additionalLanguagesToken = 0, double additionalManualToken = 0);
    }
}