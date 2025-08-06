using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisTrainingAnswerService : IPraxisTrainingAnswerService, IDeleteDataForClientInCollections
    {
        private readonly ILogger<PraxisTrainingAnswerService> _logger;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

        public PraxisTrainingAnswerService(
            IMongoSecurityService mongoSecurityService,
            ILogger<PraxisTrainingAnswerService> logger,
            IRepository repository,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            ISecurityContextProvider securityContextProvider,
            ICockpitSummaryCommandService cockpitSummaryCommandService
        )
        {
            _mongoSecurityService = mongoSecurityService;
            _logger = logger;
            _repository = repository;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _securityContextProvider = securityContextProvider;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
        }

        public void UpdatePraxisAnswerSummary(PraxisTrainingAnswer praxisTrainingAnswer)
        {
            try
            {
                var praxisTraining =
                    _repository.GetItem<PraxisTraining>(pt =>
                        pt.ItemId.Equals(praxisTrainingAnswer.TrainingId) && !pt.IsMarkedToDelete);

                if (praxisTraining != null)
                {
                    var answerSummary =
                        praxisTraining.AnswerSummary.FirstOrDefault(summary =>
                            !string.IsNullOrWhiteSpace(summary.PersonId) &&
                            summary.PersonId.Equals(praxisTrainingAnswer.PersonId)
                        );

                    if (answerSummary == null)
                    {
                        var newAnswerSummary = new PraxisAnswerSummary
                        {
                            PersonId = praxisTrainingAnswer.PersonId,
                            BestScore = praxisTrainingAnswer.Score,
                            BestScoreSubmissionDate = DateTime.Now,
                            TotalTries = 1
                        };

                        if (newAnswerSummary.BestScore > praxisTraining.Qualification)
                        {
                            List<string> completedByList;
                            if (praxisTraining.CompletedBy == null)
                            {
                                completedByList = new[] { praxisTrainingAnswer.PersonId }.ToList();
                            }
                            else
                            {
                                completedByList = praxisTraining.CompletedBy.ToList();
                                if (!completedByList.Contains(praxisTrainingAnswer.PersonId))
                                    completedByList.Add(praxisTrainingAnswer.PersonId);
                            }

                            praxisTraining.CompletedBy = completedByList;
                        }

                        IEnumerable<PraxisAnswerSummary> answerSummaryList = new[] { newAnswerSummary };
                        praxisTraining.AnswerSummary = praxisTraining.AnswerSummary.Concat(answerSummaryList);
                        praxisTraining.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                        _repository.Update(training => training.ItemId.Equals(praxisTraining.ItemId), praxisTraining);
                    }
                    else
                    {
                        answerSummary.BestScoreSubmissionDate = praxisTrainingAnswer.Score > answerSummary.BestScore
                            ? DateTime.Now
                            : answerSummary.BestScoreSubmissionDate;
                        answerSummary.BestScore = Math.Max(answerSummary.BestScore, praxisTrainingAnswer.Score);
                        answerSummary.TotalTries++;

                        if (answerSummary.BestScore > praxisTraining.Qualification)
                        {
                            List<string> CompletedByList = null;
                            if (praxisTraining.CompletedBy == null)
                            {
                                CompletedByList = new[] { praxisTrainingAnswer.PersonId }.ToList();
                            }
                            else
                            {
                                CompletedByList = praxisTraining.CompletedBy.ToList();
                                if (!CompletedByList.Contains(praxisTrainingAnswer.PersonId))
                                    CompletedByList.Add(praxisTrainingAnswer.PersonId);
                            }

                            praxisTraining.CompletedBy = CompletedByList;
                            praxisTraining.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                        }

                        _repository.Update(training => training.ItemId.Equals(praxisTraining.ItemId), praxisTraining);
                    }
                    _logger.LogInformation("AnswerSummary of PraxisTraining id: {ItemId} updated", praxisTraining.ItemId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while updating training answer of PraxisTraining id: {TrainingId}. ErrorMessage: {Message}, StackTrace: {StackTrace}",
                    praxisTrainingAnswer.TrainingId, e.Message, e.StackTrace);
            }
        }

        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            try
            {
                var clientAdminAccessRole =
                    _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
                var clientManagerAccessRole =
                    _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

                var permission = new EntityReadWritePermission
                {
                    Id = Guid.Parse(itemId)
                };

                permission.RolesAllowedToRead.Add(clientAdminAccessRole);
                permission.RolesAllowedToRead.Add(clientManagerAccessRole);

                permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);
                permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);

                _mongoSecurityService.UpdateEntityReadWritePermission<PraxisTrainingAnswer>(permission);
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while updating RowLevelSecurity of PraxisTrainingAnswer id: {itemId} ErrorMessage: {Message},  StackTrace: {StackTrace}",
                    itemId, e.Message, e.StackTrace);
            }
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<PraxisTrainingAnswer>> GetTrainingAnswerData(string filter, string sort)
        {
            return await Task.Run(() =>
            {
                FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                if (!string.IsNullOrEmpty(filter)) queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);

                var securityContext = _securityContextProvider.GetSecurityContext();

                queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                    PdsActionEnum.Read,
                    securityContext,
                    securityContext.Roles.ToList()
                );

                long totalRecord = 0;

                var collections = _mongoDbDataContextProvider
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisTrainingAnswers")
                    .Aggregate()
                    .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();

                if (!string.IsNullOrEmpty(sort)) collections = collections.Sort(BsonDocument.Parse(sort));

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisTrainingAnswer>(document));

                return new EntityQueryResponse<PraxisTrainingAnswer>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };
            });
        }

        public async Task<Dictionary<string, TrainingAnswerQueryResponse>> GetPraxisTrainingAnswerWithAssignedMembers(
            GetTrainingAnswersQuery query)
        {
            var response = new Dictionary<string, TrainingAnswerQueryResponse>();
            try
            {
                var filterString = "{TrainingId: {$in: [\"" + string.Join("\", \"", query.TrainingIds) + "\"]}}";
                query.OrderBy ??= "{CreateDate: 1}";
                var praxisTrainingAnswers = (await GetTrainingAnswerData(filterString, query.OrderBy)).Results;

                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var personId = _repository.GetItem<PraxisUser>(p => p.UserId.Equals(userId)).ItemId;

                var trainings = _repository.GetItems<PraxisTraining>(training =>
                    query.TrainingIds.Contains(training.ItemId)
                ).ToList();

                var clientId = trainings.First().ClientId;

                var praxisUsers = _repository.GetItems<PraxisUser>(praxisUser =>
                    praxisUser.ClientList.Any(client => client.ClientId.Equals(clientId)) &&
                    praxisUser.Active && !(praxisUser.Roles != null && praxisUser.Roles.Contains(RoleNames.GroupAdmin)) &&
                    !praxisUser.IsMarkedToDelete
                ).ToList();

                var allRelatedPraxisTrainingAnswers = new List<PraxisTrainingAnswer>();

                foreach (var trainingAnswer in praxisTrainingAnswers)
                {
                    if (trainingAnswer.SubmissionNumber == 1 && trainingAnswer.Score < trainingAnswer.Qualification)
                    {
                        if (trainingAnswer.PersonId == personId)
                            allRelatedPraxisTrainingAnswers.Add(trainingAnswer);
                    }
                    else
                    {
                        allRelatedPraxisTrainingAnswers.Add(trainingAnswer);
                    }
                }

                foreach (var training in trainings)
                {
                    var assignedMembers = new List<string>();

                    if (training.SpecificControllingMembers.Any())
                    {
                        assignedMembers.AddRange(training.SpecificControllingMembers);
                    }

                    if (training.SpecificControlledMembers.Any())
                    {
                        assignedMembers.AddRange(training.SpecificControlledMembers);
                    }
                    else
                    {
                        foreach (var praxisUser in praxisUsers)
                        {
                            var roles = new List<string> { RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2 };
                            var userRoles = praxisUser.ClientList.First(client => client.ClientId.Equals(clientId)).Roles.ToList();
                            var isAssignedUser = userRoles.Any(role => roles.Any(r => role.Contains(r)));
                            if (isAssignedUser)
                            {
                                assignedMembers.Add(praxisUser.ItemId);
                            }
                        }
                    }

                    assignedMembers = assignedMembers.Distinct().ToList();

                    var trainingAnswers = allRelatedPraxisTrainingAnswers.ToList()
                        .FindAll(trainingAnswer => trainingAnswer.TrainingId.Equals(training.ItemId));

                    response.Add(training.ItemId, GetTrainingAnswerQueryResponse(
                        assignedMembers, trainingAnswers, query.WithAllTrainingAnswers
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during getting training answer. Exception message: {Message}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return response;
        }

        private TrainingAnswerQueryResponse GetTrainingAnswerQueryResponse(
            List<string> assignedMembers,
            List<PraxisTrainingAnswer> trainingAnswers,
            bool withTrainingAnswers
        )
        {
            var trainingAnswerQueryResponse = new TrainingAnswerQueryResponse { AssignedMembers = assignedMembers };
            try
            {
                trainingAnswerQueryResponse.TrainingAnswers = withTrainingAnswers ? trainingAnswers : null;

                trainingAnswerQueryResponse.AnswersSubmittedBy = trainingAnswers
                    .Select(trainingAnswer => trainingAnswer.PersonId).ToList();

                assignedMembers.AddRange(trainingAnswerQueryResponse.AnswersSubmittedBy);

                trainingAnswerQueryResponse.AnswersPendingBy = assignedMembers.FindAll(praxisUserId =>
                    !trainingAnswerQueryResponse.AnswersSubmittedBy.Contains(praxisUserId)
                );

                var notPassedMembers = trainingAnswerQueryResponse.AnswersPendingBy.ToList();

                foreach (var praxisUserId in trainingAnswerQueryResponse.AnswersSubmittedBy)
                    if (!trainingAnswers.Any(answer =>
                        answer.PersonId.Equals(praxisUserId) && answer.Score >= answer.Qualification))
                        notPassedMembers.Add(praxisUserId);

                trainingAnswerQueryResponse.AssignedMembers = assignedMembers.Distinct().ToList();
                trainingAnswerQueryResponse.AnswersPendingBy =
                    trainingAnswerQueryResponse.AnswersPendingBy.Distinct().ToList();
                trainingAnswerQueryResponse.NotPassedMembers = notPassedMembers.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during processing training answers. Exception message: {Message}. Exception details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            trainingAnswerQueryResponse.AssignedMembers = trainingAnswerQueryResponse
                .AssignedMembers.Distinct().ToList();
            return trainingAnswerQueryResponse;
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {PraxisTrainingAnswer} for client {ClientId}", nameof(PraxisTrainingAnswer), clientId);

            try
            {
                await _repository.DeleteAsync<PraxisTrainingAnswer>(trainingAnswer => trainingAnswer.ClientId.Equals(clientId));
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {PraxisTrainingAnswer} for client {ClientId}. Error: {Message}. Stacktrace: {StackTrace}", nameof(PraxisTrainingAnswer), clientId, e.Message, e.StackTrace);
            }
        }
    }
}