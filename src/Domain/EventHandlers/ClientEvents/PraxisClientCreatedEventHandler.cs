using System;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsAdmins;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents
{
    public class PraxisClientCreatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisClient>>
    {
        private readonly ILogger<PraxisClientCreatedEventHandler> _logger;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IRiqsAdminsCreateUpdateService _riqsAdminsCreateUpdateService;
        private readonly IUserCountMaintainService _userCountMaintainService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly INotificationService _notificationService;
        private readonly UserUpdateService _userUpdateService;

        public PraxisClientCreatedEventHandler(
            ILogger<PraxisClientCreatedEventHandler> logger,
            IPraxisClientService praxisClientService,
            IRiqsAdminsCreateUpdateService riqsAdminsCreateUpdateService,
            IUserCountMaintainService userCountMaintainService, 
            IPraxisClientSubscriptionService praxisClientSubscriptionService,
            INotificationService notificationService,
            UserUpdateService userUpdateService
        )
        {
            _logger = logger;
            _praxisClientService = praxisClientService;
            _riqsAdminsCreateUpdateService = riqsAdminsCreateUpdateService;
            _userCountMaintainService = userCountMaintainService;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
            _notificationService = notificationService;
            _userUpdateService = userUpdateService;
        }

        public async Task<bool> HandleAsync(GqlEvent<PraxisClient> eventPayload)
        {
            try
            {
                var entityData = eventPayload.EntityData;
                _praxisClientService.CreateDynamicRoles(entityData.ItemId);
                _praxisClientService.AddRowLevelSecurity(entityData.ItemId);
                _praxisClientService.AddFeatureRoleMapForAddingSupplierAndCategory(entityData.ItemId);
                await _userCountMaintainService.UpdateOrganizationLevelUserCount(entityData.ItemId);
                if (entityData.ParentOrganizationId != null)
                {
                    await _praxisClientSubscriptionService.SaveClientSubscriptionOnClientCreateUpdate(entityData.ItemId);
                    await _riqsAdminsCreateUpdateService.InitiateAdminBUpdateOnNewDepartmentAdd(entityData.ParentOrganizationId);
                    await InitiateGroupAdmin(entityData);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during in {nameof(PraxisClientCreatedEventHandler)}");
                _logger.LogError($"Exception Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }

            return false;
        }

        private async Task InitiateGroupAdmin(PraxisClient client)
        {
            var praxisUsers = await _riqsAdminsCreateUpdateService.InitiateGroupAdminUpdateOnNewDepartmentAdd(client.ItemId, client.ParentOrganizationId);
            if (praxisUsers?.Count > 0)
            {
                foreach (var pu in praxisUsers)
                {
                    var userId = pu.UserId;
                    pu.ItemId = userId;
                    var success = await _userUpdateService.ProcessData(pu, null);

                    var result = new
                    {
                        NotifiySubscriptionId = userId,
                        Success = success
                    };
                    await _notificationService.UserLogOutNotification(success, userId, result, "UserUpdate", "RolesUpdated");
                }
            }
        } 
    }
}
