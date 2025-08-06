using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideAnswerEvents;

public class PraxisProcessGuideAnswerCreatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuideAnswer>>
{
    private readonly ILogger<PraxisProcessGuideAnswerCreatedEventHandler> _logger;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public PraxisProcessGuideAnswerCreatedEventHandler(
        ILogger<PraxisProcessGuideAnswerCreatedEventHandler> logger,
        ICockpitSummaryCommandService cockpitSummaryCommandService)
    {
        _logger = logger;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }
    public async Task<bool> HandleAsync(GqlEvent<PraxisProcessGuideAnswer> @eventPayload)
    {
        _logger.LogInformation("Enter {HandlerName} with event payload: {EventPayload}.",
            nameof(PraxisProcessGuideAnswerCreatedEventHandler), JsonConvert.SerializeObject(@eventPayload));
        try
        {
            // var answerId = @eventPayload.EntityData.ItemId;
            // await _cockpitSummaryCommandService.SyncSubmittedAnswer(answerId, nameof(PraxisProcessGuideAnswer));
            return true;

        }
        catch (Exception e)
        {
            _logger.LogError("Error in Handler: {HandlerName}. Error Message: {Message} Error Details: {StackTrace}",
                nameof(PraxisProcessGuideAnswerCreatedEventHandler), e.Message, e.StackTrace);
            return false;
        }
    }
}