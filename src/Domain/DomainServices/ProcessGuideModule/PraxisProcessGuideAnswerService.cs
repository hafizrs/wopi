using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;



namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisProcessGuideAnswerService : IPraxisProcessGuideAnswerService
    {
        private readonly ILogger<PraxisProcessGuideAnswerService> _logger;
        private readonly IRepository _repository;
        private readonly IPraxisProcessGuideService _praxisProcessGuideService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        public PraxisProcessGuideAnswerService(
            ILogger<PraxisProcessGuideAnswerService> logger,
            IRepository repository,
            IPraxisProcessGuideService praxisProcessGuideService,
            IObjectArtifactUtilityService objectArtifactUtilityService
        )
        {
            _logger = logger;
            _repository = repository;
            _praxisProcessGuideService = praxisProcessGuideService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }

        public async Task UpdateProcessGuideLibraryFormResponse(ObjectArtifact artifact)
        {
            try
            {
                if (artifact == null) return;
                if (!string.IsNullOrEmpty(artifact.OwnerId))
                {

                    var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.UserId == artifact.OwnerId);

                    if (praxisUser != null && artifact.MetaData != null)
                    {
                        var metaData = artifact.MetaData;
                        var praxisUserId = praxisUser.ItemId;
                        var entityName = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "EntityName");
                        var entityId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "EntityId");
                        var taskId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "TaskId");
                        var clientId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, "ClientId");
                        var isComplete = _objectArtifactUtilityService.IsACompletedFormResponse(metaData);

                        var originalFormId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                                                $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"]);

                        if (entityName == EntityName.PraxisProcessGuide && !string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(taskId) && !string.IsNullOrEmpty(clientId))
                        {
                            var processGuideAnswer = _repository.GetItem<PraxisProcessGuideAnswer>
                                            (p => p.ProcessGuideId == entityId && p.ClientId == clientId &&  p.SubmittedBy == praxisUserId && p.LibraryFormResponses != null &&
                                    p.LibraryFormResponses.Any(l => l.ProcessGuideTaskId == taskId && l.OriginalFormId == originalFormId && !l.IsComplete));

                            if (processGuideAnswer != null)
                            {
                                var libraryFormResponse = processGuideAnswer.LibraryFormResponses
                                            .FirstOrDefault(l => l.ProcessGuideTaskId == taskId && l.OriginalFormId == originalFormId);
                                if (libraryFormResponse != null)
                                {
                                    libraryFormResponse.LibraryFormId = artifact.ItemId;
                                    libraryFormResponse.CompletedBy = praxisUserId;
                                    if (isComplete)
                                    {
                                        libraryFormResponse.IsComplete = isComplete;
                                        libraryFormResponse.CompletedOn = DateTime.UtcNow;
                                        if (processGuideAnswer.Answers == null) processGuideAnswer.Answers = new List<PraxisProcessGuideSingleAnswer>();
                                        
                                        var answer = processGuideAnswer.Answers.FirstOrDefault(a => a.QuestionId == taskId);
                                        if (answer != null)
                                        {
                                            answer.SubmittedOn = DateTime.UtcNow;
                                        }
                                        else
                                        {
                                            answer = new PraxisProcessGuideSingleAnswer()
                                            {
                                                QuestionId = taskId,
                                                SubmittedOn = DateTime.UtcNow,
                                                Remarks = "",
                                                FileIds = new List<string>(),
                                                Files = new List<PraxisDocument>()
                                            };
                                            var answers = processGuideAnswer.Answers.ToList();
                                            answers.Add(answer);
                                            processGuideAnswer.Answers = answers;
                                        }
                                    }

                                    await _repository.UpdateAsync(
                                        p => p.ItemId == processGuideAnswer.ItemId,
                                        PraxisConstants.PraxisTenant,
                                        processGuideAnswer);
                                    if (isComplete)
                                    {
                                       // _ = await _praxisProcessGuideService.UpdateProcessGuideCompletionStatus(new List<string> { entityId });
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in UpdateProcessGuideLibraryFormResponse: {ErrorMessage}", ex.Message);
            }
        }
    }
}