using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.DynamicRolePrefix;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents
{
    public class PraxisOpenItemCompletionInfoCreatedEventHandler :
        IBaseEventHandler<GqlEvent<PraxisOpenItemCompletionInfo>>
    {
        private readonly ILogger<PraxisOpenItemCompletionInfoCreatedEventHandler> _logger;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly IPraxisOpenItemService _praxisOpenItemService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisOpenItemCompletionInfoCreatedEventHandler(
            ILogger<PraxisOpenItemCompletionInfoCreatedEventHandler> logger,
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            IPraxisOpenItemService praxisOpenItemService,
            ICockpitSummaryCommandService cockpitSummaryCommandService)
        {
            _logger = logger;
            _mongoSecurityService = mongoSecurityService;
            _repository = repository;
            _praxisOpenItemService = praxisOpenItemService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }

        public bool Handle(GqlEvent<PraxisOpenItemCompletionInfo> eventPayload)
        {
            _logger.LogInformation(
                $"Enter {nameof(PraxisOpenItemCompletionInfoCreatedEventHandler)}" +
                $"with event payload: {JsonConvert.SerializeObject(eventPayload)}."
            );
            try
            {
                var itemId = eventPayload.EntityData.ItemId;
                var praxisOpenItem = _repository.GetItem<PraxisOpenItem>(openItem =>
                    openItem.ItemId.Equals(eventPayload.EntityData.PraxisOpenItemId) && !openItem.IsMarkedToDelete
                );
                if (praxisOpenItem != null && itemId != null)
                {
                    var praxisOpenItemCompletionInfo =
                        _repository.GetItem<PraxisOpenItemCompletionInfo>(o =>
                            o.ItemId == itemId && !o.IsMarkedToDelete);
                    if (praxisOpenItemCompletionInfo != null)
                    {
                        AddPraxisOpenItemCompletionInfoRowLevelSecurity(praxisOpenItemCompletionInfo,
                            praxisOpenItem.ClientId);
                        if (!string.IsNullOrEmpty(praxisOpenItem.TaskReferenceId))
                        {
                            _praxisOpenItemService.GetOpenItemCompletionDetails(new GetCompletionListQuery
                            { TaskReferenceId = praxisOpenItem.TaskReferenceId });
                        }

                        if (praxisOpenItemCompletionInfo.Completion != null)
                        {
                            _praxisOpenItemService.ProcessEmailForAssignedUsersCompletion(praxisOpenItem, praxisOpenItemCompletionInfo);

                            _praxisOpenItemService.UpdatePraxisOpenItemCompletionStatus(
                                praxisOpenItem, praxisOpenItemCompletionInfo, false
                            );
                            _cockpitSummaryCommandService.SyncSubmittedAnswer(itemId, nameof(PraxisOpenItemCompletionInfo))
                                .GetAwaiter()
                                .GetResult();
                        }
                    }
                }
                else
                {
                    _logger.LogInformation(
                        $"No {nameof(PraxisOpenItem)} data found by ItemId: {eventPayload.EntityData.PraxisOpenItemId}."
                    );
                }

                _logger.LogInformation(
                    $"Handled by {nameof(PraxisOpenItemCompletionInfoCreatedEventHandler)} " +
                    $"with event payload: {JsonConvert.SerializeObject(eventPayload)}."
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during update dynamic role in: {nameof(PraxisOpenItemCompletionInfo)} " +
                    $"with ItemId: {eventPayload.EntityData.ItemId}.   " +
                    $"Exception Message: {ex.Message}. Exception Details: {ex.StackTrace}."
                );
                return false;
            }
        }

        private void AddPraxisOpenItemCompletionInfoRowLevelSecurity(
            PraxisOpenItemCompletionInfo completionInfo, string clientId
        )
        {
            var rolesAllowToRead = new List<string>();
            var rolesAllowToUpdate = new List<string>();

            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(PraxisClientRead, clientId);
            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(PraxisClientManager, clientId);

            rolesAllowToRead.Add(clientAdminAccessRole);
            rolesAllowToRead.Add(clientReadAccessRole);
            rolesAllowToRead.Add(clientManagerAccessRole);

            rolesAllowToUpdate.Add(clientAdminAccessRole);
            rolesAllowToUpdate.Add(clientManagerAccessRole);

            completionInfo.RolesAllowedToRead = GetRoles(completionInfo.RolesAllowedToRead, rolesAllowToRead);
            completionInfo.RolesAllowedToUpdate = GetRoles(completionInfo.RolesAllowedToUpdate, rolesAllowToUpdate);

            var update = new Dictionary<string, object>
            {
                {nameof(PraxisOpenItemCompletionInfo.RolesAllowedToRead), completionInfo.RolesAllowedToRead},
                {nameof(PraxisOpenItemCompletionInfo.RolesAllowedToUpdate), completionInfo.RolesAllowedToUpdate}
            };

            _repository.UpdateAsync<PraxisOpenItemCompletionInfo>(p => p.ItemId == completionInfo.ItemId, update)
                .Wait();
            _logger.LogInformation(
                $"{nameof(PraxisOpenItemCompletionInfo)} Entity data has been Updated" +
                $"with ItemId: {completionInfo.ItemId}."
            );
        }

        private string[] GetRoles(string[] roles, List<string> existingRoles)
        {
            if (roles == null || roles?.Length == 0)
            {
                return existingRoles.ToArray();
            }

            var previousRoles = roles.ToList();
            previousRoles.AddRange(existingRoles);
            return previousRoles.ToArray();
        }
    }
}