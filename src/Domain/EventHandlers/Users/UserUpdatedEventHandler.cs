using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Users
{
    public class UserUpdatedEventHandler : IBaseEventHandler<User>
    {
        private readonly ILogger<UserUpdatedEventHandler> _logger;
        private readonly IRepository repository;
        private readonly IUserPersonService userService;
        private readonly IPraxisUserService praxisUserService;

        public UserUpdatedEventHandler(ILogger<UserUpdatedEventHandler> logger, IUserPersonService userService, IRepository repository, IPraxisUserService praxisUserService)
        {
            _logger = logger;
            this.repository = repository;
            this.userService = userService;
            this.praxisUserService = praxisUserService;
        }

        public bool Handle(User user)
        {
            if (string.IsNullOrEmpty(user?.ItemId)) return false;

            var userId = user.ItemId;
            var person = userService.GetByUserId(userId);
            var praxisUser = repository.GetItem<PraxisUser>(p => p.ItemId == person.ItemId && !p.IsMarkedToDelete);
            if (praxisUser != null && (praxisUser.ClientList != null || praxisUser.ClientList?.Count() > 0) && !string.IsNullOrEmpty(person.CreatedBy))
            {
                _logger.LogInformation("Client List for {EntityName} entity with ItemId: {ItemId} and Client List: {ClientList}.",
                    nameof(PraxisUser), person.ItemId, JsonConvert.SerializeObject(praxisUser.ClientList));

                var latestOrganizationId = praxisUser.ClientList.Where(x => x.IsLatest).Select(y => y.ClientId).FirstOrDefault();
                if (!string.IsNullOrEmpty(latestOrganizationId))
                {
                    userService.SendEmailToUserForLatestClient(praxisUser);
                }
                var clientIds = praxisUser.ClientList.Select(p => p.ClientId).ToList();
                if (clientIds.Count == 1)
                {
                    var praxisClient = repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(clientIds[0]) && !pc.IsMarkedToDelete);

                    if (praxisClient?.CompanyTypes != null && praxisClient.CompanyTypes.ToList().Exists(c => c.Equals(RoleNames.TechnicalClient)))
                    {
                        _logger.LogInformation("User.Updated, Praxis User");
                        userService.RoleReassignToUser(person, praxisUser.ClientList, true);
                        userService.AddRowLevelSecurity(person.ItemId, clientIds.ToArray());
                    }
                    else
                    {
                        _logger.LogInformation("User.Updated, Non Praxis User");
                        userService.RoleReassignToUser(person, praxisUser.ClientList);
                        userService.AddRowLevelSecurity(person.ItemId, clientIds.ToArray());
                    }
                }
                else if(clientIds.Count>1)
                {
                    _logger.LogInformation("User.Updated, Non Praxis User");
                    userService.RoleReassignToUser(person, praxisUser.ClientList);
                    userService.AddRowLevelSecurity(person.ItemId, clientIds.ToArray());
                }
                praxisUserService.UpdatePraxisUserRoles(person.ItemId);
                praxisUserService.AddRowLevelSecurity(praxisUser.ItemId, clientIds.ToArray());
            }
            else
            {
                _logger.LogInformation("Organization ID or Person Created ID missing: For UserID {UserId}", user.ItemId);
            }

            if (praxisUser != null && praxisUser.Active != user.Active)
            {
                bool status = praxisUserService.UpdateUserActivationStatus(user.ItemId, user.Active);
                _logger.LogInformation("PraxisUser ({UserId}) {Action} Success: {Status}", user.ItemId, user.Active ? "Activation" : "Deactivation", status);
            }

            return true;
        }
    }
}
