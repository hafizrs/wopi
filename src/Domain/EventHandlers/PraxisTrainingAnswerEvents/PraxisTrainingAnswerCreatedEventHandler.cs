using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingAnswerEvents
{
    public class PraxisTrainingAnswerCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTrainingAnswer>>
    {
        private readonly ILogger<PraxisTrainingAnswerCreatedEventHandler> _logger;
        private readonly IPraxisTrainingAnswerService _praxisTrainingAnswerService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly PraxisTrainingQualificationPassedEventHandler _praxisTrainingQualificationPassedEventHandler;
        public PraxisTrainingAnswerCreatedEventHandler(IPraxisTrainingAnswerService praxisTrainingAnswerService, 
            ILogger<PraxisTrainingAnswerCreatedEventHandler> log,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            IGenericEventPublishService genericEventPublishService,
            PraxisTrainingQualificationPassedEventHandler praxisTrainingQualificationPassedEventHandler
        )
        {
            _logger = log;
            _praxisTrainingAnswerService = praxisTrainingAnswerService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _praxisTrainingQualificationPassedEventHandler = praxisTrainingQualificationPassedEventHandler;
        }
        public bool Handle(GqlEvent<PraxisTrainingAnswer> eventPayload)
        {
            try
            {
               _praxisTrainingAnswerService.AddRowLevelSecurity(eventPayload.EntityData.ItemId, eventPayload.EntityData.ClientId);
               _praxisTrainingAnswerService.UpdatePraxisAnswerSummary(eventPayload.EntityData);
               _cockpitSummaryCommandService
                   .CreateSummary(eventPayload.EntityData.TrainingId, nameof(CockpitTypeNameEnum.PraxisTraining), true)
                   .GetAwaiter()
                   .GetResult();
               var score = eventPayload.EntityData.Score;
               var qualification = eventPayload.EntityData.Qualification;
               if (score >= qualification)
               {
                   var genericEvent = new GenericEvent()
                   {
                       JsonPayload = eventPayload.EntityData.TrainingId
                   };
                   _praxisTrainingQualificationPassedEventHandler.HandleAsync(genericEvent).GetAwaiter().GetResult();
               }
               return true;
            }
            catch (Exception e)
            {
               
                _logger.LogError("PraxisTrainingAnswerCreatedEventHandler -> {ErrorMessage}", e.Message);
                _logger.LogError("Full StackTrace -> {StackTrace}", e.StackTrace);
            }

            return false;
        }
    }
}
