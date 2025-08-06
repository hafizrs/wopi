using System.Collections.Generic;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisUserService
    {
        Task<PraxisUser> GetPraxisUser(string userId);
        List<PraxisUser> GetControlledUsers(string praxisClientId);
        List<PraxisUser> GetControllingUsers(string praxisClientId);
        List<PraxisUser> GetControlledAndControllingUsers(string praxisClientId);
        Task<List<PraxisUser>> GetAllPraxisUsers();
        Task<Person> GetPersonByUserId(string userId);
        Task UpdatePraxisUserImage(string userId, PraxisImage image);

        void RoleAssignToPraxisUser(string praxisUserId, IEnumerable<PraxisClientInfo> clientList,
            bool isTechnicalClient = false);

        void RoleReassignToPraxis(string praxisUserId, IEnumerable<PraxisClientInfo> clientList,
            bool isTechnicalClient = false);

        void AddRowLevelSecurity(string itemId, string[] clientIds);
        bool UpdatePraxisUserRoles(string itemId);
        bool UpdatePraxisUserLatestClientProperty(PraxisUser praxisUserInfo);
        PraxisUser GetPraxisUserByUserId(string userId);
        bool UpdateUserActivationStatus(string userId, bool active = true, bool isEmailVerified = false);
        List<PraxisUser> GetControlledUsersForSendingMail(PraxisTraining existingTraining);

        Task<EntityQueryResponse<PraxisUser>> GetPraxisUserListReportData(string filter, string sort = "{DisplayName: 1}");

        Task<bool> ProcessPraxisUserDtos(List<string> praxisUserIds);
        Task<bool> ProcessPraxisUserDtosForAllPraxisUsers();
        Task<bool> UpdatePraxisUserDto(List<PraxisClientInfo> clientList, string praxisUserId);
    }
}