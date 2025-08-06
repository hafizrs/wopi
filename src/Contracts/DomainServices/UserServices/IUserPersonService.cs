using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IUserPersonService
    {
        void AddRowLevelSecurity(string itemId, string[] clientIds);
        void RoleAssignToUser(Person person, IEnumerable<PraxisClientInfo> clientList, bool isTechnicalClient = false);
        void RoleReassignToUser(Person person, IEnumerable<PraxisClientInfo> clientList, bool isTechnicalClient = false);
        Person GetByUserId(string userId);
        Person GetById(string personId);
        PersonQueryResponse GetListByIds(List<string> personIds, string searchText = null, int pageNumber = 0, bool useImpersonation = false);
        PersonQueryResponse GetListByUserIds(List<string> userIds, string searchText = null, int pageNumber = 0, bool useImpersonation = false);
        PersonQueryResponse GetList(int pageNumber = 0, bool useImpersonation = false);
        PersonQueryResponse GetAdminList(int pageNumber = 0, bool useImpersonation = false);
        PersonQueryResponse GetListByClientId(string clientId, int pageNumber = 0, bool useImpersonation = false);
        PersonQueryResponse GetListByFilter(FilterDefinition<Person> filter, List<string> fieldsToReturn = null,
            int pageNumber = 0, int itemsPerPage = 100, string orderBy = "CreateDate", bool descending = true,
            bool useImpersonation = true);
        List<Person> GetListByOrgId(string orgId);
        List<Person> GetListByRoles(List<string> Roles);
        void SendEmailToUserForLatestClient( PraxisUser praxisUserInfo);
        PraxisUserDto MapPraxisUserDto(PraxisUser praxisUser, string itemId = "");
        Task PrepareDataForFeatureRoleMap(string roleName);
    }
}
