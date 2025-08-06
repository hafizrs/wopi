using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideAnswerEvents;

public class PraxisProcessGuideAnswerUpdatedEventHandler : IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuideAnswer>>
{
    private readonly ILogger<PraxisProcessGuideAnswerUpdatedEventHandler> _logger;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public PraxisProcessGuideAnswerUpdatedEventHandler(
        ILogger<PraxisProcessGuideAnswerUpdatedEventHandler> logger,
        ICockpitSummaryCommandService cockpitSummaryCommandService)
    {
        _logger = logger;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }
    public async Task<bool> HandleAsync(GqlEvent<PraxisProcessGuideAnswer> @eventPayload)
    {
        _logger.LogInformation("Enter {HandlerName} with event payload: {EventPayload}.",
            nameof(PraxisProcessGuideAnswerUpdatedEventHandler), JsonConvert.SerializeObject(@eventPayload));
        try
        {
            // var answerId = @eventPayload.Filter;
            // await _cockpitSummaryCommandService.SyncSubmittedAnswer(answerId, nameof(PraxisProcessGuideAnswer));
            return true;

        }
        catch (Exception e)
        {
            _logger.LogError("Error in Handler: {HandlerName}. Error Message: {Message} Error Details: {StackTrace}",
                nameof(PraxisProcessGuideAnswerUpdatedEventHandler), e.Message, e.StackTrace);
            return false;
        }
    }
}