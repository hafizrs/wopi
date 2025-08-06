using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Users
{
    public class UserActivatedEventHandler : IBaseEventHandler<User>
    {
        private readonly ILogger<UserActivatedEventHandler> _logger;
        private readonly IPraxisUserService praxisUserService;
        private readonly IUserCountMaintainService userCountMaintainService;
        private readonly IRepository repository;

        public UserActivatedEventHandler(ILogger<UserActivatedEventHandler> logger,
            IPraxisUserService praxisUserService,
            IUserCountMaintainService userCountMaintainService,
            IRepository repository)
        {
            _logger = logger;
            this.praxisUserService = praxisUserService;
            this.userCountMaintainService = userCountMaintainService;
            this.repository = repository;
        }
        public bool Handle(User user)
        {
            if (string.IsNullOrEmpty(user?.ItemId)) return false;

            bool activated = praxisUserService.UpdateUserActivationStatus(user.ItemId, user.Active, user.EmailVarified);

            _logger.LogInformation("PraxisUser ({UserId}) Activation Success: {Activated}", user.ItemId, activated);

            var primaryDept = repository.GetItem<PraxisUser>(pu => pu.UserId == user.ItemId && !user.IsMarkedToDelete)?.ClientList?.FirstOrDefault(c => c.IsPrimaryDepartment);
            if (primaryDept != null) userCountMaintainService.InitiateUserCountUpdateProcessOnUserCreate(primaryDept.ClientId, primaryDept.ParentOrganizationId).GetAwaiter().GetResult();

            return true;
        }
    }
}
