using System;
using System.Collections.Generic;
using System.Linq;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingAnswerEvents;

public class PraxisTrainingQualificationPassedEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<PraxisTrainingQualificationPassedEventHandler> _logger;
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly PraxisOpenItemCompletionInfoCreatedEventHandler _praxisOpenItemCompletionInfoCreatedEventHandler;
    private readonly IPraxisOpenItemService _praxisOpenItemService;

    public PraxisTrainingQualificationPassedEventHandler(
        ILogger<PraxisTrainingQualificationPassedEventHandler> logger,
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        PraxisOpenItemCompletionInfoCreatedEventHandler praxisOpenItemCompletionInfoCreatedEventHandler,
        IPraxisOpenItemService praxisOpenItemService)
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _praxisOpenItemCompletionInfoCreatedEventHandler = praxisOpenItemCompletionInfoCreatedEventHandler;
        _praxisOpenItemService = praxisOpenItemService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(PraxisTrainingQualificationPassedEventHandler), JsonConvert.SerializeObject(@event));
        try
        {
            var trainingId = @event.JsonPayload ?? string.Empty;
            if (string.IsNullOrEmpty(trainingId))
            {
                _logger.LogInformation("{HandlerName}: Operation aborted as trainingId is empty.", nameof(PraxisTrainingQualificationPassedEventHandler));
                return false;
            }
            var securityContext = _securityContextProvider.GetSecurityContext();
            var loggedInUserId = securityContext.UserId;
            var praxisCurrentUser = await _repository.GetItemAsync<PraxisUser>(p => 
                !p.IsMarkedToDelete && p.UserId == loggedInUserId);
            var todos = await _praxisOpenItemService.GetProjectedOpenItemsWithSpecificTraining(trainingId);
            if (todos.Any())
            {
                var taskList = todos
                    .Select(async todo =>
                    {
                        var todoCompletionInfo = new PraxisOpenItemCompletionInfo
                        {
                            ItemId = Guid.NewGuid().ToString(),
                            ReportedByUserId = praxisCurrentUser.ItemId,
                            ReportedTime = DateTime.UtcNow.ToLocalTime(),
                            Comment = string.Empty,
                            Completion = new PraxisKeyValue
                            {
                                Key = "done",
                                Value = "Done",
                            },
                            DocumentInfo = new List<PraxisOpenItemDocument>(),
                            ActualBudget = 0,
                            PraxisOpenItemId = todo.ItemId,
                            Tags = new[] { "Is-Valid-PraxisOpenItemCompletionInfo" },
                            Language = "en-US",
                            IdsAllowedToRead = new[] { loggedInUserId },
                            IdsAllowedToUpdate = new[] { loggedInUserId },
                            IdsAllowedToDelete = new[] { loggedInUserId },
                            CreatedBy = loggedInUserId,
                            CreateDate = DateTime.Now.ToLocalTime(),
                            IsMarkedToDelete = false,
                            TenantId = securityContext.TenantId
                        };
                        await _repository.SaveAsync(todoCompletionInfo);

                        const string eventName = PraxisEventName.PraxisOpenItemCompletionInfoItemCreatedEventName;
                        var eventPayload = new GqlEvent<PraxisOpenItemCompletionInfo>
                        {
                            EntityData = todoCompletionInfo,
                            EventName = eventName,
                        };
                        return _praxisOpenItemCompletionInfoCreatedEventHandler.Handle(eventPayload);
                    })
                    .ToList();
                await Task.WhenAll(taskList);
            }

            return true;

        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(PraxisTrainingQualificationPassedEventHandler), JsonConvert.SerializeObject(@event), e.Message, e.StackTrace);
            return false;
        }
    }

}