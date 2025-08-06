using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTrainingEvents
{
    public class PraxisTrainingCreatedEventHandler : IBaseEventHandler<GqlEvent<PraxisTraining>>
    {
        private readonly IPraxisTrainingService praxisTrainingService;
        private readonly IRepository repository;
        private readonly ILogger<PraxisTrainingCreatedEventHandler> _logger;
        private readonly IEmailNotifierService emailNotifierService;
        private readonly IPraxisUserService praxisUserService;
        private readonly IEmailDataBuilder emailDataBuilder;
        private readonly IPraxisFormService praxisFormService;
        private readonly IUilmResourceKeyService uilmResourceKeyService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        

        public PraxisTrainingCreatedEventHandler(
            IPraxisTrainingService praxisTrainingService,
            ILogger<PraxisTrainingCreatedEventHandler> logger,
            IEmailNotifierService emailNotifierService,
            IPraxisUserService praxisUserService,
            IEmailDataBuilder emailDataBuilder,
            IRepository repository,
            IPraxisFormService praxisFormService,
            IUilmResourceKeyService uilmResourceKeyService,
            ICockpitSummaryCommandService cockpitSummaryCommandService
           
        )
        {
            this.praxisTrainingService = praxisTrainingService;
            this.emailNotifierService = emailNotifierService;
            this.praxisUserService = praxisUserService;
            this.emailDataBuilder = emailDataBuilder;
            this.repository = repository;
            _logger = logger;
            this.praxisFormService = praxisFormService;
            this.uilmResourceKeyService = uilmResourceKeyService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
           
        }
        public bool Handle(GqlEvent<PraxisTraining> eventPayload)
        {
            try
            {
                var praxisTraining = eventPayload.EntityData;
                
                var training = GetPraxisTrainingById(praxisTraining.ItemId).GetAwaiter().GetResult();
                _cockpitSummaryCommandService.CreateSummary(training.ItemId, nameof(CockpitTypeNameEnum.PraxisTraining)).GetAwaiter();
                
                praxisTrainingService.AddRowLevelSecurity(praxisTraining.ItemId, praxisTraining.ClientId);
                if (praxisTraining.IsActive)
                {
                    ProcessEmailForAssignedUsers(praxisTraining);
                }

                praxisFormService.UpdatePraxisForm(
                    "PraxisTraining",
                    praxisTraining.ItemId, new string[] { praxisTraining.FormId },
                    praxisTraining.ClientId);
                
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("PraxisTrainingCreatedEventHandler -> {ErrorMessage}", e.Message);
            }

            return false;
        }

        private void ProcessEmailForAssignedUsers(PraxisTraining praxisTraining)
        {
            praxisTraining.TopicValue = uilmResourceKeyService.GetResourceValueByKeyName(praxisTraining.TopicValue, string.Empty);

            if (!string.IsNullOrEmpty(praxisTraining.ItemId))
            {
                var praxisUsers = praxisUserService.GetControlledUsersForSendingMail(praxisTraining);
                if (praxisUsers.Any())
                {
                    SendEmailToPraxisUsers(praxisUsers, praxisTraining);
                }
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

        private async Task<PraxisTraining> GetPraxisTrainingById(string trainingId)
        {
            return await repository.GetItemAsync<PraxisTraining>(t => t.ItemId.Equals(trainingId));
        }
    }
}
