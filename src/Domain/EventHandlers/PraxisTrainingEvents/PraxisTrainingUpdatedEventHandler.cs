using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ChangeEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System.Linq;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingEvents
{
    public class PraxisTrainingUpdatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTraining>>
    {
        private readonly IChangeLogService changeLogService;
        private readonly IRepository repository;
        private readonly ILogger<PraxisTrainingCreatedEventHandler> _logger;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IPraxisUserService praxisUserService;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IUilmResourceKeyService uilmResourceKeyService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisTrainingUpdatedEventHandler(
            IChangeLogService changeLogService,
            ILogger<PraxisTrainingCreatedEventHandler> logger,
            IEmailNotifierService emailNotifierService,
            IPraxisUserService praxisUserService,
            IEmailDataBuilder emailDataBuilder,
            IRepository repository,
            IUilmResourceKeyService uilmResourceKeyService,
            ICockpitSummaryCommandService cockpitSummaryCommandService
            )
        {
            this.changeLogService = changeLogService;
            this.emailNotifierService = emailNotifierService;
            this.praxisUserService = praxisUserService;
            this.emailDataBuilder = emailDataBuilder;
            this.repository = repository;
            this._logger = logger;
            this.uilmResourceKeyService = uilmResourceKeyService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }
        public bool Handle(GqlEvent<PraxisTraining> eventPayload)
        {
            try
            {
                if (eventPayload.EntityData != null && eventPayload.Filter != null)
                {
                    var trainingData = eventPayload.EntityData;
                    trainingData.ItemId = eventPayload.Filter;
                    _cockpitSummaryCommandService
                        .CreateSummary(trainingData.ItemId, nameof(CockpitTypeNameEnum.PraxisTraining), true)
                        .GetAwaiter();
                }
                
                if (eventPayload.EventData != null)
                {
                    PraxisTrainingChangeEvent trainingChanges =
                        JsonConvert.DeserializeObject<PraxisTrainingChangeEvent>(eventPayload.EventData);
                    var praxisTraining = eventPayload.EntityData;
                    if (praxisTraining.IsActive)
                    {
                        ProcessEmailForAssignedUsers(praxisTraining);
                    }

                    if (trainingChanges != null)
                    {
                        return ProcessRiskChanges(trainingChanges).GetAwaiter().GetResult();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception -> {ErrorMessage}", e.Message);
                return false;
            }
        }

        private async Task<bool> ProcessRiskChanges(PraxisTrainingChangeEvent changes)
        {
            var trainingNameUpdateStatuses = new List<Task<bool>>();

            if (!string.IsNullOrEmpty(changes.Title) && !string.IsNullOrEmpty(changes.ItemId))
            {
                var updates = new Dictionary<string, object>
                {
                    {"TaskReferenceTitle", changes.Title }
                };

                var builders = Builders<BsonDocument>.Filter;
                var dataFilters = builders.Eq("TaskReferenceId", changes.ItemId);

                var praxisOpenItemUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisOpenItem, dataFilters, updates);
                trainingNameUpdateStatuses.Add(praxisOpenItemUpdateStatus);

                var openItemConfigupdates = new Dictionary<string, object>
                {
                    {"TaskReferenceTitle", changes.Title }
                };

                var praxisOpenItemConfigUpdateStatus = changeLogService.UpdateChange(EntityName.PraxisOpenItemConfig, dataFilters, openItemConfigupdates);
                trainingNameUpdateStatuses.Add(praxisOpenItemConfigUpdateStatus);
            }

            await Task.WhenAll(trainingNameUpdateStatuses);

            return true;
        }

        private void ProcessEmailForAssignedUsers(PraxisTraining praxisTraining)
        {
            praxisTraining.TopicValue = uilmResourceKeyService.GetResourceValueByKeyName(praxisTraining.TopicValue, string.Empty);
            var praxisUsers = praxisUserService.GetControlledUsersForSendingMail(praxisTraining);
            if (praxisUsers.Any())
            {
                SendEmailToPraxisUsers(praxisUsers, praxisTraining);
            }
        }
  
        private void SendEmailToPraxisUsers(List<PraxisUser> praxisUsers, PraxisTraining praxisTraining)
        {
            if (praxisUsers != null && praxisUsers.Count > 0)
            {
                foreach (var praxisUser in praxisUsers)
                {
                    var person = repository.GetItem<Person>(p => p.ItemId.Equals(praxisUser.ItemId) && !p.IsMarkedToDelete);
                    if (!string.IsNullOrWhiteSpace(person?.Email))
                    {
                        var emailData = emailDataBuilder.BuildTraingEmailData(praxisTraining, person);
                        emailNotifierService.SendTrainingAssignedEmail(person, emailData);
                    }
                }
            }
        }
    }
}
