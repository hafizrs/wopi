using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisUserEvents
{
    public class PraxisUserCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisUser>>
    {
        private readonly ILogger<PraxisUserCreatedEventHandler> _logger;
        private readonly IPraxisUserService praxisUserService;
        private readonly IRepository repository;
        public PraxisUserCreatedEventHandler(
            ILogger<PraxisUserCreatedEventHandler> logger,
            IPraxisUserService praxisUserService,
            IRepository repository)
        {
            this._logger = logger;
            this.praxisUserService = praxisUserService;
            this.repository = repository;
        }
        public bool Handle(GqlEvent<PraxisUser> eventPayload)
        {
            _logger.LogInformation("Enter {EventHandlerName} with praxisUserId: {PraxisUserId}",
                nameof(PraxisUserCreatedEventHandler), eventPayload.EntityData.ItemId);
            try
            {
                string praxisUserId = eventPayload.EntityData.ItemId;

                if (string.IsNullOrEmpty(praxisUserId)) return false;

                if (eventPayload.EntityData.ClientList != null || eventPayload.EntityData.ClientList.Any())
                {
                    var clientIds = eventPayload.EntityData.ClientList.Select(p => p.ClientId).ToList();
                    if (clientIds.Count == 1)
                    {
                        var praxisClient = repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(clientIds[0]) && !pc.IsMarkedToDelete);

                        if (praxisClient?.CompanyTypes != null && praxisClient.CompanyTypes.ToList().Exists(c => c.Equals(RoleNames.TechnicalClient)))
                        {
                            _logger.LogInformation("PraxisUser.Created, Praxis User");
                            praxisUserService.RoleAssignToPraxisUser(praxisUserId, eventPayload.EntityData.ClientList, true);
                        }
                        else
                        {
                            _logger.LogInformation("PraxisUser.Created, Non Praxis User");
                            praxisUserService.RoleAssignToPraxisUser(praxisUserId, eventPayload.EntityData.ClientList);
                        }

                        praxisUserService.AddRowLevelSecurity(praxisUserId, new[] { praxisClient?.ItemId });
                    }
                    else if (clientIds.Count > 1)
                    {
                        _logger.LogInformation("PraxisUser.Created, Non Praxis User");
                        praxisUserService.RoleAssignToPraxisUser(praxisUserId, eventPayload.EntityData.ClientList);
                        praxisUserService.AddRowLevelSecurity(praxisUserId, clientIds.ToArray());
                    }
                }
                else
                {
                    _logger.LogInformation("ClientId missing: For PraxisUserId {PraxisUserId}", praxisUserId);
                }
                _logger.LogInformation("Handled By {EventHandlerName} with praxisUserId: {PraxisUserId}",
                    nameof(PraxisUserCreatedEventHandler), eventPayload.EntityData.ItemId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during insert dynamic role while creating new {EntityName} with ItemId: {ItemId}. Exception message: {ExceptionMessage}. Exception Details: {ExceptionDetails}.",
                    nameof(PraxisUser), eventPayload.EntityData.ItemId, ex.Message, ex.StackTrace);
            }

            return false;
        }
    }
}
