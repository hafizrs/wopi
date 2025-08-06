using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisProcessGuideService : IPraxisProcessGuideService, IDeleteDataForClientInCollections
    {
        private readonly ICommonUtilService _commonUtilService;
        private readonly ILogger<PraxisProcessGuideService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IPraxisFileService _praxisFileService;
        private readonly IObjectArtifactFileQueryService _objectArtifactFileQueryService;
        private double TotalFileSizeofImages;
        private readonly double MaxTotalFileSizeofImagesInMb = 150.0;
        private readonly ICirsProcessGuideAttachmentService _cirsProcessGuideAttachmentService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly ISecurityHelperService _securityHelperService;
        public PraxisProcessGuideService(
            ICommonUtilService commonUtilService,
            ILogger<PraxisProcessGuideService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            IMongoSecurityService mongoSecurityService,
            IPraxisFileService praxisFileService,
            IObjectArtifactFileQueryService objectArtifactFileQueryService,
            ICirsProcessGuideAttachmentService cirsProcessGuideAttachmentService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            ISecurityHelperService securityHelperService
        )
        {
            _commonUtilService = commonUtilService;
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _mongoSecurityService = mongoSecurityService;
            _praxisFileService = praxisFileService;
            _objectArtifactFileQueryService = objectArtifactFileQueryService;
            _cirsProcessGuideAttachmentService = cirsProcessGuideAttachmentService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _securityHelperService = securityHelperService;
        }

        public async Task<EntityQueryResponse<PraxisProcessGuideWithClientCompletion>> GetPraxisProcessGuide(
            GetProcessGuideQuery filter
        )
        {
            try
            {
                var response = await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuideWithClientCompletion>(
                    filter.FilterString,
                    filter.SortBy,
                    "PraxisProcessGuides",
                    true,
                    filter.PageNumber,
                    filter.PageSize,
                    additionalStages: PrepareAdditionalStageDefinitionBuilder(filter)
                );
                var clientIds = response.Results.SelectMany(pg => pg.Clients.Select(y => y.ClientId)).Concat(filter.ClientIds ?? new List<string>()).Distinct().ToList();

                var clientFilterString = "{Active: true, Roles:{$nin: [\"" + $"{RoleNames.GroupAdmin}" + "\"]}, \"ClientList.ClientId\": {$in: [\"" +
                                         $"{string.Join("\", \"", clientIds)}" + "\"]}}";
                var securityContext = _securityContextProvider.GetSecurityContext();
                var praxisClients = _repository.GetItems<PraxisClient>(cl =>
                        !cl.IsMarkedToDelete &&
                        ((cl.RolesAllowedToRead != null && cl.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r))) ||
                        (cl.IdsAllowedToRead != null && cl.IdsAllowedToRead.Contains(securityContext.UserId)) &&
                        clientIds.Contains(cl.ItemId)))?
                    .Select(c => c.ItemId)
                    .ToList() ?? new List<string>();

                var praxisUsers = (await _commonUtilService.GetEntityQueryResponse<PraxisUser>(
                    clientFilterString, "{DisplayName: 1}"
                )).Results;

                var pgs = response.Results.ToList();
                foreach (var processGuide in pgs)
                {
                    processGuide.Clients = processGuide.Clients.Where(c => praxisClients.Contains(c.ClientId));
                    foreach (var client in processGuide.Clients)
                    {
                        if (client.ControlledMembers.Any()) continue;

                        client.ControlledMembers = praxisUsers
                            .Where(praxisUser => praxisUser.ClientList.Any(c => c.ClientId.Equals(client.ClientId)) && !(praxisUser.Roles != null && praxisUser.Roles.Contains(RoleNames.GroupAdmin)))
                            .Select(praxisUser => praxisUser.ItemId).ToList();
                    }
                }

                var queryResponse = new EntityQueryResponse<PraxisProcessGuideWithClientCompletion>
                {
                    Results = GetCompletedUser(pgs, praxisUsers, filter.IsAStandardTemplateView),
                    ErrorMessage = response.ErrorMessage,
                    StatusCode = response.StatusCode,
                    TotalRecordCount = response.TotalRecordCount
                };

                return queryResponse;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to fetch and process PraxisProcessGuide: {ErrorMessage}", e.Message);
                return new EntityQueryResponse<PraxisProcessGuideWithClientCompletion>
                {
                    Results = new List<PraxisProcessGuideWithClientCompletion>(),
                    TotalRecordCount = 0,
                    ErrorMessage = e.Message
                };
            }
        }

        private List<PipelineStageDefinition<BsonDocument, BsonDocument>> PrepareAdditionalStageDefinitionBuilder(GetProcessGuideQuery filter)
        {
            if (!filter.IsAStandardTemplateView) return null;


            var graphLookupStage = PrepareGraphLookUpForClonedGuides(filter.ClonedPgFilterString);

            var stages = new List<PipelineStageDefinition<BsonDocument, BsonDocument>>
            {
                graphLookupStage
            };

            if (!string.IsNullOrEmpty(filter?.SortBy)) stages.Add(PrepareSortFilterForNonEmptyCloneGuides(filter.SortBy));

            if (filter.HideStandardGuideIfNoCloneExists)
            {
                stages.Add(PrepareMatchFilterForNonEmptyCloneGuides());
            }
            stages.Add(PrepareAddFieldFilterToLimitSearchResult());

            return stages;
        }


        private PipelineStageDefinition<BsonDocument, BsonDocument> PrepareGraphLookUpForClonedGuides(string filter)
        {
            var graphLookUpDefinition = new BsonDocument("$graphLookup", new BsonDocument()
                .Add("from", $"{nameof(PraxisProcessGuide)}s")
                .Add("startWith", "$_id")
                .Add("connectFromField", "_id")
                .Add("connectToField", "StandardTemplateId")
                .Add("as", "StandardCloneGuides")
                .Add("maxDepth", 0)
                .Add("restrictSearchWithMatch", PrepareChildFindingFilter(filter)));
            return graphLookUpDefinition;
        }

        private PipelineStageDefinition<BsonDocument, BsonDocument> PrepareSortFilterForNonEmptyCloneGuides(string sortBy)
        {
            var sortFilter = new BsonDocument("$addFields", new BsonDocument
            {
                {
                    "StandardCloneGuides",
                    new BsonDocument("$sortArray", new BsonDocument
                    {
                        { "input", "$StandardCloneGuides" },
                        { "sortBy", BsonSerializer.Deserialize<BsonDocument>(sortBy) }
                    })
                }
            });

            return sortFilter;
        }

        private PipelineStageDefinition<BsonDocument, BsonDocument> PrepareMatchFilterForNonEmptyCloneGuides()
        {
            var queryFilter = new BsonDocument
            {
                { "$and", new BsonArray
                    {
                        new BsonDocument("StandardCloneGuides", new BsonDocument("$exists", true)),
                        new BsonDocument("StandardCloneGuides", new BsonDocument("$not", new BsonDocument("$size", 0)))
                    }
                }
            };
            return new BsonDocument("$match", queryFilter);
        }

        private PipelineStageDefinition<BsonDocument, BsonDocument> PrepareAddFieldFilterToLimitSearchResult()
        {
            var queryFilter = new BsonDocument
            {
                { "$addFields", new BsonDocument
                    {
                        { "StandardCloneGuides", new BsonDocument("$slice", new BsonArray { "$StandardCloneGuides", 10 }) }
                    }
                }
            };
            return queryFilter;
        }

        private BsonDocument PrepareChildFindingFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            var queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);
            return queryFilter;
        }
        private void ManageClientForTemplate(
            PraxisProcessGuide processGuide,
            string praxisClientId,
            PraxisClient praxisClient
            )
        {
            if (processGuide.IsATemplate)
            {
                if (praxisClient != null)
                {
                    var filteredClients = processGuide.Clients.Where(c => c.ClientId == praxisClientId).ToList();

                    if (!filteredClients.Any())
                    {
                        processGuide.Clients = processGuide.Clients.Concat(new List<ProcessGuideClientInfo>
                            {
                                new ProcessGuideClientInfo
                                {
                                    ClientId = praxisClient.ItemId,
                                    ClientName = praxisClient.ClientName
                                }
                            });
                        Console.WriteLine(processGuide);
                    }
                    else
                    {

                        processGuide.Clients = filteredClients;
                    }
                    processGuide.Clients = processGuide.Clients.ToList();
                }

            }


        }

        public List<string> GetProcessGuideIds(string configId)
        {
            try
            {
                var ids = string.IsNullOrEmpty(configId) ?
                        new List<string>() :
                        _repository.GetItems<PraxisProcessGuide>
                            (pg => !pg.IsMarkedToDelete && pg.PraxisProcessGuideConfigId == configId)?
                            .Select(pg => pg.ItemId)?
                            .ToList() ?? new List<string>();

                return ids;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occured in GetProcessGuideIds -> {Message}", e.Message);
                return null;
            }
        }

        public async Task<EntityQueryResponse<ProcessGuideDetailsResponse>> GetPraxisProcessGuideDetails(
            List<string> processGuideIds,
            string praxisClientId = null,
            int timezoneOffsetInMinutes = 0
        )
        {
            var queryResponse = new EntityQueryResponse<ProcessGuideDetailsResponse>
            {
                Results = new List<ProcessGuideDetailsResponse>()
            };
            try
            {
                if (processGuideIds == null) return queryResponse;

                var processGuideIdsAsString = "[\"" + string.Join("\",\"", processGuideIds) + "\"]";
                var processGuides = (await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuide>(
                    "{_id: {$in:" + processGuideIdsAsString + " }}", "{CreateDate: 1}"
                )).Results?.ToList() ?? new List<PraxisProcessGuide>();
                var allRelatedProcessGuideAnswers =
                    (await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuideAnswer>(
                        "{ProcessGuideId: {$in:" + processGuideIdsAsString + " }}"
                    )).Results;


                List<string> clientsWithEmptyControlledMembers = new List<string>();

                var praxisClient = string.IsNullOrEmpty(praxisClientId)
                    ? null
                    : _repository.GetItem<PraxisClient>(client => client.ItemId.Equals(praxisClientId));
                var praxisClientIds = processGuides.SelectMany(p => p.Clients.Select(c => c.ClientId)).Distinct().ToList();
                var securityContext = _securityContextProvider.GetSecurityContext();
                var praxisClients = _repository.GetItems<PraxisClient>(client =>
                        !client.IsMarkedToDelete &&
                        ((client.RolesAllowedToRead != null && client.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r))) ||
                        (client.IdsAllowedToRead != null && client.IdsAllowedToRead.Contains(securityContext.UserId)) &&
                        praxisClientIds.Contains(client.ItemId)))
                    .Select(c => c.ItemId)
                    .ToList();
                foreach (var pg in processGuides)
                {
                    if (!string.IsNullOrEmpty(praxisClientId)) ManageClientForTemplate(pg, praxisClientId, praxisClient);
                    var clients = pg.Clients.Where(c => praxisClients.Contains(c.ClientId)).ToList();
                    pg.Clients = clients;
                    var clientIds = pg.Clients.Where(client => !client.HasSpecificControlledMembers)
                                   .Select(client => client.ClientId)
                                   .Distinct();
                    Console.WriteLine(pg);
                    clientsWithEmptyControlledMembers.AddRange(clientIds);
                }



                var praxisUsers = await GetPraxisUserListFromClientIds(clientsWithEmptyControlledMembers.Distinct().AsEnumerable());
                var formIds = processGuides.Select(processGuide => processGuide.FormId);
                var pgIds = processGuides.Select(processGuide => processGuide.ItemId);
                // var forms = _repository.GetItems<PraxisForm>(form => formIds.Contains(form.ItemId));
                var forms = _repository.GetItems<AssignedTaskForm>
                                   (p => pgIds.Contains(p.AssignedEntityId)
                                   && p.AssignedEntityName == nameof(PraxisProcessGuide)).ToList();

                foreach (var processGuide in processGuides)
                {
                    ManageClientForTemplate(processGuide, praxisClientId, praxisClient);
                    var processGuideAnswers = allRelatedProcessGuideAnswers.Where(processGuideAnswer =>
                            processGuideAnswer.ProcessGuideId.Equals(processGuide.ItemId) &&
                            processGuideAnswer.Answers.Any()
                        )
                        .ToList();

                    var form = forms.FirstOrDefault(praxisForm => processGuide.FormId.Equals(praxisForm.ClonedFormId)) ?? new AssignedTaskForm();
                    var clientWiseCompletionResponse = GetClientWiseCompletionInfo(
                        processGuide, processGuideAnswers, form, praxisUsers
                    );
                    var permissionWiseResponse = clientWiseCompletionResponse;
                    if (praxisClientId != null && timezoneOffsetInMinutes != 0)
                    {
                        permissionWiseResponse = ProcessGuidePermissionHelper.
                          PrepareProcessGuidePermissionResponse(
                          _securityHelperService, _repository, _securityContextProvider,
                         praxisClientId, clientWiseCompletionResponse,
                         processGuide, form, praxisUsers, timezoneOffsetInMinutes
                      );
                    }


                    queryResponse.Results = queryResponse.Results.Append(permissionWiseResponse);
                }

                return queryResponse;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occurred while getting Praxis Process Guide Details. Message: {m} \nStacktree: {s}", e.Message, e.StackTrace);
                queryResponse.ErrorMessage = e.Message;
                return queryResponse;
            }
        }

        public async Task<bool> UpdateProcessGuideCompletionStatus(List<string> processGuideIds)
        {
            _logger.LogInformation("Entered in {MethodName}", nameof(UpdateProcessGuideCompletionStatus));

            try
            {
                var processGuideDetailsResponses = (await GetPraxisProcessGuideDetails(processGuideIds))?.Results?.ToList() ?? new List<ProcessGuideDetailsResponse>();
                foreach (var processGuideDetailsResponse in processGuideDetailsResponses)
                {
                    if (string.IsNullOrEmpty(processGuideDetailsResponse?.ProcessGuide?.ItemId)) continue;

                    var praxisProcessGuide = _repository.GetItem<PraxisProcessGuide>(processGuide =>
                        processGuide.ItemId == processGuideDetailsResponse.ProcessGuide.ItemId
                    );
                    try
                    {
                        var completionData = processGuideDetailsResponse?.ProcessGuideClientCompletionList ?? new List<ProcessGuideClientCompletion>();
                        praxisProcessGuide.CompletionStatus = (int)(completionData?.Select(clientCompletion =>
                                clientCompletion?.CompletionPercentage ?? 0
                            )?
                            .DefaultIfEmpty(0)?
                            .Average() ?? 0);

                        praxisProcessGuide.DraftStatus = (int)(completionData?.Select(clientCompletion =>
                                clientCompletion?.DraftPercentage ?? 0
                            )?
                            .DefaultIfEmpty(0)?
                            .Average() ?? 0);

                        if (praxisProcessGuide.CompletionStatus == 100)
                        {
                            var answerDateTimeList = processGuideDetailsResponse?.ProcessGuideAnswers?
                                .SelectMany(userWiseAnswer => userWiseAnswer.Answers?.ToList() ?? new List<PraxisProcessGuideSingleAnswer>())?
                                .Select(answer => answer.SubmittedOn)?
                                .ToList() ?? new List<DateTime>();
                            var latestDate = answerDateTimeList.OrderByDescending(d => d).First();
                            praxisProcessGuide.CompletionDate = latestDate;
                        }
                        else
                        {
                            praxisProcessGuide.CompletionDate = null;
                        }

                        _logger.LogInformation("Method Name: {MethodName}  Process guide Completion status: {CompletionStatus}  ItemId: {ItemId}",
                            nameof(UpdateProcessGuideCompletionStatus), praxisProcessGuide.CompletionStatus, praxisProcessGuide.ItemId);

                        praxisProcessGuide.ClientCompletionInfo = completionData;
                        praxisProcessGuide.LastUpdateDate = DateTime.Now;

                        _logger.LogInformation(
                            $"Updating process guide: {praxisProcessGuide.Title} with ItemId: {praxisProcessGuide.ItemId}"
                        );

                        await _repository.UpdateAsync(processGuide =>
                                processGuide.ItemId == praxisProcessGuide.ItemId,
                                PraxisConstants.PraxisTenant,
                                praxisProcessGuide
                        );

                        _logger.LogInformation("RelatedEntityName: {EntityName}, CompletionStatus: {Status}",
                            praxisProcessGuide.RelatedEntityName ?? nameof(PraxisProcessGuide), praxisProcessGuide.CompletionStatus);
                        if (praxisProcessGuide.RelatedEntityName is nameof(CirsGenericReport))
                        {
                            await _cirsProcessGuideAttachmentService.UpdateProcessGuideCompletionStatus(praxisProcessGuide.ItemId, praxisProcessGuide.CompletionStatus);
                        }

                        if (praxisProcessGuide.CompletionStatus > 0 && praxisProcessGuide.CompletionStatus < 100)
                        {
                            await _cockpitSummaryCommandService.UpdateSummeryForClonedProcessGuide(praxisProcessGuide.ItemId, nameof(PraxisProcessGuide));
                        }

                        if (praxisProcessGuide.CompletionStatus == 100)
                        {
                            await _cockpitSummaryCommandService.CreateSummary(praxisProcessGuide.ItemId, nameof(PraxisProcessGuide), true);
                        }
                        var answerId = _repository.GetItem<PraxisProcessGuideAnswer>(a =>
                            a.ProcessGuideId == praxisProcessGuide.ItemId)?.ItemId ?? string.Empty;
                        await _cockpitSummaryCommandService.SyncSubmittedAnswer(answerId, nameof(PraxisProcessGuideAnswer));
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Update failed for process guide: {title} with ItemId: {itemid} -> Message: {messgae} -> StackTrace: {s}", 
                            praxisProcessGuide.Title, praxisProcessGuide.ItemId, e.Message, e.StackTrace);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in {MethodName}. Message: {Message} Details: {StackTrace}", nameof(UpdateProcessGuideCompletionStatus), e.Message, e.StackTrace);
                return false;
            }

            return true;
        }

        // Remove later
        public async Task<bool> UpdateProcessGuideControlledMemberIds()
        {
            var processGuides =
                (await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuide>("{}", "{CreateDate: -1}")).Results;
            var response = true;
            foreach (var processGuide in processGuides)
            {
                try
                {
                    if (processGuide.Clients != null)
                    {
                        _logger.LogInformation("{ProcessGuideTitle} Created on {CreateDate}:", processGuide.Title, processGuide.CreateDate.AddHours(6));
                        foreach (var clientInfo in processGuide.Clients)
                        {
                            var ogControlledMemberIds = clientInfo.ControlledMembers.ToList();
                            var praxisUserDtos = _repository.GetItems<PraxisUserDto>(praxisUserDto =>
                                    praxisUserDto.Clients.Any(client => client.ClientId.Equals(clientInfo.ClientId))
                                )
                                .ToList();

                            clientInfo.ControlledMembers = ogControlledMemberIds
                                .Select(controlledMemberId =>
                                    praxisUserDtos.Find(dto =>
                                            dto.ItemId.Equals(controlledMemberId) ||
                                            dto.PraxisUserId.Equals(controlledMemberId)
                                        )
                                        ?.PraxisUserId ?? controlledMemberId
                                )
                                .Where(id => !string.IsNullOrEmpty(id))
                                .ToList();
                            _logger.LogInformation("{ClientName}: Updated {UpdatedIdsCount} ids in {TotalIdsCount} ids", clientInfo.ClientName, ogControlledMemberIds.Count - ogControlledMemberIds.Intersect(clientInfo.ControlledMembers).Count(), ogControlledMemberIds.Count);
                        }

                        processGuide.ControlledMembers = processGuide.Clients
                            .SelectMany(c => c.ControlledMembers)
                            .Distinct();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Update failed for process guide: {processGuide.Title} with ItemId: {processGuide.ItemId}"
                    );
                    _logger.LogError($"Message: {ex.Message} StackTrace: {ex.StackTrace}");
                    response = false;
                }
            }

            _commonUtilService.UpdateMany(processGuides);
            return response;
        }


        private List<PraxisProcessGuideMinialInfo> GetStandardCloneGuides(
            List<PraxisProcessGuideMinialInfo> guides,
            PraxisProcessGuideWithClientCompletion standardTemplate,
            IEnumerable<PraxisUser> praxisUsers
            )
        {
            var response = new List<PraxisProcessGuideMinialInfo>();

            var ids = guides.Select(x => x.ItemId).ToList();
            var processGuideAnswers = _repository.GetItems<PraxisProcessGuideAnswer>(answer =>
                     ids.Contains(answer.ProcessGuideId)
                  ).ToList();



            foreach (var processGuide in guides)
            {
                foreach (var client in processGuide.Clients)
                {
                    if (!client.ControlledMembers.Any())
                    {
                        client.ControlledMembers = praxisUsers
                            .Where(praxisUser => praxisUser.ClientList.Any(c => c.ClientId.Equals(client.ClientId)))
                            .Select(praxisUser => praxisUser.ItemId).ToList();
                    }
                    var stClients = standardTemplate.Clients?.ToList() ?? new List<ProcessGuideClientInfo>();
                    var stClient = standardTemplate.Clients?.FirstOrDefault(c => c.ClientId == client.ClientId);
                    if (stClient != null)
                    {
                        var members = stClient.ControlledMembers?.ToList() ?? new List<string>();
                        members.AddRange(client.ControlledMembers);
                        members = members.Distinct().ToList();
                        stClient.ControlledMembers = members;
                        standardTemplate.Clients = stClients;
                    }
                    else
                    {
                        stClients.Add(client);
                        standardTemplate.Clients = stClients;
                    }
                }

                var controlledMembers = processGuide.ControlledMembers?.ToList() ?? new List<string>();
                foreach (var client in processGuide.Clients)
                {
                    controlledMembers.AddRange(client.ControlledMembers?.ToList() ?? new List<string>());
                }
                processGuide.ControlledMembers = controlledMembers.Distinct().ToList();


                var filteredAnswers = processGuideAnswers.Where(x => x.ProcessGuideId.Equals(processGuide.ItemId)).ToList();
                var clientCompletetionInfo = processGuide.Clients.Select(client =>
                {
                    return new ProcessGuideClientInfo
                    {
                        ClientId = client.ClientId,
                        ClientName = client.ClientName,
                        ControlledMembers = filteredAnswers
                            .Where(answer => answer.ClientId.Equals(client.ClientId) && answer.Answers.Any())
                            .Select(answer => answer.SubmittedBy)
                    };
                });

                var processGuideData = new PraxisProcessGuideMinialInfo(processGuide);
                processGuideData.ClientCompletion = clientCompletetionInfo;
                response.Add(processGuideData);
            }

            return response.OrderBy(x => x.TaskSchedule.TaskDateTime).ToList();
        }

        private List<PraxisProcessGuideWithClientCompletion> GetCompletedUser(
            List<PraxisProcessGuideWithClientCompletion> processGuideWithCompletionInfos,
            IEnumerable<PraxisUser> praxisUsers,
            bool isTemplateView = false
        )
        {
            var response = new List<PraxisProcessGuideWithClientCompletion>();

            foreach (var processGuideWithCompletionInfo in processGuideWithCompletionInfos)
                try
                {
                    if (isTemplateView && processGuideWithCompletionInfo.IsATemplate)
                    {
                        processGuideWithCompletionInfo.StandardCloneGuides = GetStandardCloneGuides(
                            processGuideWithCompletionInfo.StandardCloneGuides?.ToList() ?? new List<PraxisProcessGuideMinialInfo>(),
                            processGuideWithCompletionInfo,
                            praxisUsers);
                    }

                    var controlledMembers = processGuideWithCompletionInfo.ControlledMembers?.ToList() ?? new List<string>();
                    foreach (var client in processGuideWithCompletionInfo.Clients)
                    {
                        controlledMembers.AddRange(client.ControlledMembers?.ToList() ?? new List<string>());
                    }
                    processGuideWithCompletionInfo.ControlledMembers = controlledMembers.Distinct().ToList();

                    var processGuideAnswers = _repository.GetItems<PraxisProcessGuideAnswer>(answer =>
                        answer.ProcessGuideId.Equals(processGuideWithCompletionInfo.ItemId)
                    );

                    //var processGuideWithCompletionInfo = processGuide;

                    processGuideWithCompletionInfo.ClientCompletion = processGuideWithCompletionInfo.Clients.Select(client =>
                    {
                        return new ProcessGuideClientInfo
                        {
                            ClientId = client.ClientId,
                            ClientName = client.ClientName,
                            ControlledMembers = processGuideAnswers
                                .Where(answer => answer.ClientId.Equals(client.ClientId) && answer.Answers.Any())
                                .Select(answer => answer.SubmittedBy)
                        };
                    });
                    response.Add(processGuideWithCompletionInfo);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error occurred while calculating user completion, {ErrorMessage}", e.Message);
                }

            return response;
        }

        private IDictionary<string, List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>> OrganizeAnswersByQuestionId(
            IEnumerable<PraxisProcessGuideAnswer> processGuideAnswers
        )
        {
            try
            {
                var singleAnswerList = new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();
                foreach (var processGuideAnswer in processGuideAnswers)
                {
                    if (processGuideAnswer.Answers == null) continue;

                    singleAnswerList.AddRange(
                       processGuideAnswer.Answers.Select(singleAnswer =>
                           new PraxisProcessGuideSingleAnswerWithPraxisUserInfo(singleAnswer, processGuideAnswer.SubmittedBy, processGuideAnswer.ClientId
                           ))
                   );
                }

                return singleAnswerList?
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.QuestionId))?
                    .GroupBy(singleAnswer => singleAnswer.QuestionId)?
                    .ToDictionary(group => group.Key, group => group?.ToList() ?? new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>()) ?? new Dictionary<string, List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in OrganizeAnswersByQuestionId: Message -> {message} \n StackTrace -> {s}", ex.Message, ex.StackTrace);
            }

            return new Dictionary<string, List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>>();
        }

        private async Task<IDictionary<string, List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>>> OrganizeAnswersByQuestionIdAsync(
            IEnumerable<PraxisProcessGuideAnswer> processGuideAnswers,
            List<PraxisUser> praxisUsers
        )
        {
            praxisUsers ??= new List<PraxisUser>();
            var singleAnswerList = new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();
            foreach (var processGuideAnswer in processGuideAnswers)
            {
                if (processGuideAnswer.Answers == null) continue;

                singleAnswerList.AddRange(
                   processGuideAnswer.Answers.Select(
                       singleAnswer =>
                           new PraxisProcessGuideSingleAnswerWithPraxisUserInfo(
                               singleAnswer,
                               GetPraxisUser(praxisUsers, processGuideAnswer.SubmittedBy).DisplayName,
                               processGuideAnswer.ClientId
                           )
                   )
               );
            }

            singleAnswerList = processGuideAnswers.SelectMany(
                    processGuideAnswer => processGuideAnswer.Answers.Select(
                        singleAnswer =>
                            new PraxisProcessGuideSingleAnswerWithPraxisUserInfo(
                                singleAnswer,
                                GetPraxisUser(praxisUsers, processGuideAnswer.SubmittedBy).DisplayName,
                                processGuideAnswer.ClientId
                            )
                    )
                )
                .ToList();

            foreach (var singleAnswer in singleAnswerList)
            {
                singleAnswer.Files = await ProcessFilesForReportAsync(singleAnswer.Files.ToList());
            }

            return singleAnswerList
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.QuestionId))
                .GroupBy(singleAnswer => singleAnswer.QuestionId)
                .ToDictionary(group => group.Key, group => group?.ToList() ?? new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>());
        }

        public async Task<IEnumerable<PraxisDocument>> ProcessFilesForReportAsync(List<PraxisDocument> files)
        {
            files = files.Where(file => !file.IsDeleted).ToList();
            var imageFileIds = new List<string>();
            if (!files.Any()) return files;
            var artifactFiles = files?.Where(file => file.FileType == DmsConstants.LibraryFile)?.ToList() ?? new List<PraxisDocument>();
            var artifactIds = artifactFiles?.Select(f => f.DocumentId)?.ToList() ?? new List<string>();
            if (artifactIds?.Count > 0)
            {
                var query = new ObjectArtifactFileQuery()
                {
                    ObjectArtifactIds = artifactIds.ToArray(),
                    PageNumber = 1,
                    PageSize = artifactIds.Count
                };
                var artifacts = await _objectArtifactFileQueryService.InitiateGetFileArtifacts(query);
                artifacts = artifacts?.Where(a => !a.IsDeleted && !a.IsRestricted)?.ToList() ?? new List<ObjectArtifactFileResponse>();
                foreach (var file in artifactFiles)
                {
                    var artifact = artifacts?.Find(a => a.ItemId == file.DocumentId);
                    if (!string.IsNullOrEmpty(artifact?.FileStorageId))
                    {
                        file.DocumentId = artifact.FileStorageId;
                        file.DocumentName = artifact.Name;
                    }
                    else
                    {
                        files.Remove(file);
                    }
                }
            }

            foreach (var document in files.OrderBy(file => file.DocumentName))
            {
                var documentExt = document.DocumentName.Split('.').Last().ToLowerInvariant();
                document.FileType = ReportConstants.ImageExts.Contains(documentExt) ? "image" : "other";
                if (document.FileType.Equals("image"))
                {
                    imageFileIds.Add(document.DocumentId);
                }
            }
            try
            {
                var totalImageFileSizeInMb = await CheckFileSizeAsync(imageFileIds);
                TotalFileSizeofImages += totalImageFileSizeInMb;
                var fileIds = files?.Where(file => file.FileType != DmsConstants.LibraryFile)?.Select(file => file.DocumentId)?.ToList();
                var filesForReport = new List<PraxisDocument>();
                var convertedFileMaps = fileIds?.Count > 0 ? _repository.GetItems<ConvertedFileMap>(
                        file => fileIds.Contains(file.OriginalFileId)
                    )?.ToList() ?? new List<ConvertedFileMap>() : new List<ConvertedFileMap>();

                foreach (var convertedFileMap in convertedFileMaps)
                {
                    var file = files.First(f => f.DocumentId.Equals(convertedFileMap.OriginalFileId));
                    files.Remove(file);
                    file.DocumentId = convertedFileMap.ReportFileId;
                    filesForReport.Add(file);
                }

                filesForReport.AddRange(files);

                return filesForReport.OrderBy(file => file.FileType).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while processing converted file maps, returning originals");
                _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");

                return files;
            }
        }

        private async Task<double> CheckFileSizeAsync(List<string> fileIds)
        {
            var totalSize = 0.0;
            var fileInfos = await _praxisFileService.GetFilesInfoFromStorage(fileIds);
            foreach (var fileInfo in fileInfos)
            {
                var mb = (fileInfo.SizeInBytes / 1024d) / 1024d;
                totalSize += mb;
            }
            return totalSize;
        }


        private ProcessGuideDetailsResponse GetClientWiseCompletionInfo(
            PraxisProcessGuide processGuide,
            List<PraxisProcessGuideAnswer> processGuideAnswers,
            PraxisForm form,
            List<PraxisUser> praxisUsers

        )
        {
            try
            {
                var answersOrganizedByQuestionId = OrganizeAnswersByQuestionId(processGuideAnswers ?? new List<PraxisProcessGuideAnswer>());
                var processGuideDetailsResponse = new ProcessGuideDetailsResponse
                {
                    ProcessGuideClientCompletionList = new List<ProcessGuideClientCompletion>(),
                    ProcessGuideAnswers = processGuideAnswers ?? new List<PraxisProcessGuideAnswer>()
                };
                var ProcessGuideClientDraftedList = new List<ProcessGuideClientCompletion>();
                int totalTaskCount = 0, completedTaskCount = 0, draftedTaskCount = 0;
                foreach (var clientInfo in processGuide.Clients)
                {
                    try
                    {
                        _logger.LogInformation($"Calculating completion for {clientInfo.ClientName}");
                        var clientCompletion = new ProcessGuideClientCompletion
                        {
                            ClientId = clientInfo.ClientId,
                            ClientName = clientInfo.ClientName,
                            QuestionCompletion = new ItemCompletion
                            {
                                CompleteItems = new List<string>(),
                                DraftItems = new List<string>(),
                                IncompleteItems = new List<string>()
                            },
                            UserCompletion = new ItemCompletion
                            {
                                CompleteItems = new List<string>(),
                                DraftItems = new List<string>(),
                                IncompleteItems = new List<string>()
                            }
                        };

                        var clientSpecificProcessGuideAnswer = processGuideAnswers?.Where(answer =>
                            answer.ClientId == clientInfo.ClientId
                        ) ?? new List<PraxisProcessGuideAnswer>();

                        var processGuideTasks = form.ProcessGuideCheckList?
                            .Where(cl =>
                                clientInfo.ClientId == cl.ClientId ||
                                (cl.ClientInfos != null && cl.ClientInfos.Any(c => c.ClientId == clientInfo.ClientId)) ||
                                cl.OrganizationIds?.Any() == true
                            )?
                            .SelectMany(f => f.ProcessGuideTask)?
                            .ToList() ?? new List<ProcessGuideTask>();


                        if (processGuideTasks.Count == 0) continue;

                        int questionCompleted = 0, questionDrated = 0, completedTaskCountForClient = 0, draftedTaskCountForClient = 0, totalTaskCountForClient;
                        if (clientInfo.HasSpecificControlledMembers)
                        {
                            _logger.LogInformation($"{clientInfo.ClientName} has ControlledMembers");
                            totalTaskCountForClient = processGuideTasks.Count * clientInfo.ControlledMembers.Count();

                            var controlledMembers = clientInfo.ControlledMembers.ToList();
                            controlledMembers.Sort();

                            foreach (var processGuideTask in processGuideTasks)
                            {
                                var processGuideTaskId = processGuideTask.ProcessGuideTaskId;
                                var answersByTaskId = new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();
                                var isTaskCompletedByCurrentClient =
                                    answersOrganizedByQuestionId.ContainsKey(processGuideTaskId) &&
                                    answersOrganizedByQuestionId[processGuideTaskId]
                                        ?.Any(t => t.ClientId == clientInfo.ClientId && IsAnswerSubmitted(t)) == true;
                                if (isTaskCompletedByCurrentClient)
                                {
                                    answersByTaskId = answersOrganizedByQuestionId[processGuideTaskId]?
                                        .Where(f => f.ClientId == clientInfo.ClientId)?
                                        .ToList() ?? new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();
                                }
                                var completedAnswers = answersByTaskId?.Where(a => !IsAnswerDrafted(a))?.ToList() ?? new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();
                                var draftedAnswers = answersByTaskId?.Where(a => IsAnswerDrafted(a))?.ToList() ?? new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>();

                                completedTaskCountForClient += completedAnswers.Count;
                                draftedTaskCountForClient += draftedAnswers.Count;
                                var answeredMembers = completedAnswers.Select(answer => answer.SubmittedBy).ToList();
                                answeredMembers.Sort();

                                if (
                                    completedAnswers.Count == controlledMembers.Count &&
                                    answeredMembers.SequenceEqual(controlledMembers)
                                )
                                {
                                    _logger.LogInformation("{ProcessGuideTaskTitle} is completed", processGuideTask.ProcessGuideTaskTitle);
                                    questionCompleted++;
                                    clientCompletion.QuestionCompletion.CompleteItems.Add(processGuideTaskId);
                                }
                                else
                                {
                                    _logger.LogInformation("{ProcessGuideTaskTitle} is not completed", processGuideTask.ProcessGuideTaskTitle);
                                    clientCompletion.QuestionCompletion.IncompleteItems.Add(processGuideTaskId);
                                }
                            }

                            foreach (var userWiseAnswer in clientSpecificProcessGuideAnswer)
                            {
                                var user = GetPraxisUser(praxisUsers, userWiseAnswer.SubmittedBy);
                                try
                                {
                                    var draftedAnswers = userWiseAnswer?.Answers?.Where(a => a.MetaDataList?.Any(m => m.Key == "IsDraft" && m.MetaData?.Value == "1") == true)?.ToList() ?? new List<PraxisProcessGuideSingleAnswer>();
                                    if (draftedAnswers.Count == 0 && userWiseAnswer?.Answers?.Count() >= processGuideTasks.Count)
                                    {
                                        _logger.LogInformation(
                                             "{DisplayName} has completed tasks for {ClientName}",
                                             user.DisplayName, clientInfo.ClientName
                                         );
                                    }
                                    else
                                    {
                                        _logger.LogInformation(
                                            "{DisplayName} has not completed all tasks for {ClientName}",
                                            user.DisplayName, clientInfo.ClientName
                                        );
                                    }

                                    if (userWiseAnswer.Answers?.Count() != draftedAnswers.Count) clientCompletion.UserCompletion.CompleteItems.Add(userWiseAnswer.SubmittedBy);
                                    if (draftedAnswers.Count > 0) clientCompletion.UserCompletion.DraftItems.Add(userWiseAnswer.SubmittedBy);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(
                                        "Error occured while processing user completion. Error in data  " +
                                        $"Message: {e.Message}" +
                                        $"Duplicate {nameof(PraxisProcessGuideAnswer)} found for {user.DisplayName}" +
                                        $"({userWiseAnswer.SubmittedBy}), ProcessGuide {userWiseAnswer.ProcessGuideId}"
                                    );
                                }
                            }

                            foreach (var controlledMember in controlledMembers)
                            {
                                if (!clientCompletion.UserCompletion.CompleteItems.Contains(controlledMember) && !clientCompletion.UserCompletion.DraftItems.Contains(controlledMember))
                                {
                                    var user = GetPraxisUser(praxisUsers, controlledMember);
                                    _logger.LogInformation(
                                        "{DisplayName} has not completed any tasks for {ClientName}",
                                        user.DisplayName, clientInfo.ClientName
                                    );
                                    clientCompletion.UserCompletion.IncompleteItems.Add(controlledMember);
                                }
                            }

                        }
                        else
                        {
                            _logger.LogInformation(
                                "{ClientName} doesn't have ControlledMembers. " +
                                "Calculating completion data from all the process guide answers for this client",
                                clientInfo.ClientName
                            );
                            totalTaskCountForClient = processGuideTasks.Count;
                            foreach (var processGuideTask in processGuideTasks)
                            {
                                var isTaskCompletedByCurrentClient = answersOrganizedByQuestionId.ContainsKey(processGuideTask.ProcessGuideTaskId) &&
                                    answersOrganizedByQuestionId[processGuideTask.ProcessGuideTaskId]
                                        ?.Any(t => t.ClientId == clientInfo.ClientId && IsAnswerSubmitted(t) && !IsAnswerDrafted(t)) == true;

                                var isTaskDrafted = answersOrganizedByQuestionId.ContainsKey(processGuideTask.ProcessGuideTaskId) &&
                                    answersOrganizedByQuestionId[processGuideTask.ProcessGuideTaskId]
                                        ?.Any(t => t.ClientId == clientInfo.ClientId && IsAnswerSubmitted(t) && IsAnswerDrafted(t)) == true;

                                if (isTaskCompletedByCurrentClient)
                                {
                                    _logger.LogInformation("{ProcessGuideTaskTitle} is completed", processGuideTask.ProcessGuideTaskTitle);
                                    questionCompleted++;
                                    clientCompletion.QuestionCompletion.CompleteItems.Add(
                                        processGuideTask.ProcessGuideTaskId
                                    );
                                }
                                else if (isTaskDrafted)
                                {
                                    questionDrated++;
                                }
                                else
                                {
                                    _logger.LogInformation("{ProcessGuideTaskTitle} is not completed", processGuideTask.ProcessGuideTaskTitle);
                                    clientCompletion.QuestionCompletion.IncompleteItems.Add(
                                        processGuideTask.ProcessGuideTaskId
                                    );
                                }
                            }

                            clientCompletion.UserCompletion.CompleteItems = clientSpecificProcessGuideAnswer
                                .Where(c => c?.Answers?.Any(a => !IsAnswerDrafted(a)) == true).Select(
                                    answer => answer.SubmittedBy
                                )
                                .ToList();

                            clientCompletion.UserCompletion.DraftItems = clientSpecificProcessGuideAnswer
                                .Where(c => c?.Answers?.Any(a => IsAnswerDrafted(a)) == true).Select(
                                    answer => answer.SubmittedBy
                                )
                                .Where(id => !clientCompletion.UserCompletion.CompleteItems.Contains(id))
                                .ToList();

                            completedTaskCountForClient = questionCompleted;
                            draftedTaskCountForClient = questionDrated;

                            var controlledMembers = new List<string>();
                            foreach (var praxisUser in praxisUsers.Where(pu => pu.ClientList.Any(c => c.ClientId.Equals(clientInfo.ClientId))))
                            {
                                controlledMembers.Add(praxisUser.ItemId);
                            }

                            clientInfo.ControlledMembers = controlledMembers;
                        }

                        completedTaskCount += completedTaskCountForClient;
                        draftedTaskCount += draftedTaskCountForClient;
                        totalTaskCount += totalTaskCountForClient;
                        clientCompletion.CompletionPercentage = completedTaskCountForClient * 100 / totalTaskCountForClient;
                        clientCompletion.DraftPercentage = draftedTaskCountForClient * 100 / totalTaskCountForClient;
                        processGuideDetailsResponse.ProcessGuideClientCompletionList.Add(clientCompletion);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Exception occurred during completion data calculation. Message: {Message}, StackTrace: {StackTrace}", e.Message, e.StackTrace);
                    }
                }

                if (totalTaskCount > 0)
                {
                    processGuide.CompletionStatus = completedTaskCount * 100 / totalTaskCount;
                    processGuide.DraftStatus = draftedTaskCount * 100 / totalTaskCount;
                }
                processGuideDetailsResponse.ProcessGuide = processGuide;
                processGuideDetailsResponse.ProcessGuide.ClientCompletionInfo =
                    processGuideDetailsResponse.ProcessGuideClientCompletionList;
                return processGuideDetailsResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in GetClientWiseCompletionInfo: Message -> {message} \n StackTrace -> {s}", ex.Message, ex.StackTrace);
            }
            return null;
        }

        private PraxisUser GetPraxisUser(List<PraxisUser> praxisUsers, string itemId)
        {
            return praxisUsers.Find(u => u.ItemId == itemId)
                   ?? _repository.GetItem<PraxisUser>(u => u.ItemId == itemId)
                   ?? new PraxisUser { DisplayName = "DeletedUser" };
        }

        private bool IsAnswerSubmitted(PraxisProcessGuideSingleAnswer answer)
        {
            if (answer != null && answer.MetaDataList != null)
            {
                return answer.MetaDataList
                .Any(m => m.Key == "IsAnswerSubmitted" &&
                     m.MetaData.Type == "Boolean" &&
                     bool.TryParse(m.MetaData.Value, out bool result) && result);
            }
            return false;

        }

        private bool IsAnswerDrafted(PraxisProcessGuideSingleAnswer answer)
        {
            if (answer != null && answer.MetaDataList != null)
            {
                return answer.MetaDataList.Any(m => m.Key == "IsDraft" && m.MetaData?.Value == "1");
            }
            return false;

        }
        public async Task<EntityQueryResponse<PraxisProcessGuide>> GetProcessGuideData(string filter, string sort)
        {
            return await Task.Run(() =>
            {
                FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                if (!string.IsNullOrEmpty(filter))
                {
                    queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);
                }

                var securityContext = _securityContextProvider.GetSecurityContext();

                queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                    PdsActionEnum.Read,
                    securityContext,
                    securityContext.Roles.ToList()
                );

                long totalRecord = 0;
                var collections = _mongoDbDataContextProvider
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>($"PraxisProcessGuides")
                    .Aggregate()
                    .Match(queryFilter)
                    .Sort(BsonDocument.Parse(sort));

                totalRecord = collections.ToEnumerable().Count();

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisProcessGuide>(document));

                return new EntityQueryResponse<PraxisProcessGuide>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };
            });
        }

        public bool AddRowLevelSecurity(string itemId, string userId)
        {
            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };
            permission.IdsAllowedToUpdate = new List<string> { userId };
            permission.IdsAllowedToDelete = new List<string> { userId };
            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisProcessGuide>(permission);
            _logger.LogInformation("Permission successfully updated for process guide create event");
            return true;
        }

        public async Task<PraxisGenericReportResult> PrepareProcessGuidePhotoDocumentationData(
            GetReportQuery filter
        )
        {
            var response = new List<PraxisProcessGuideForReport>();
            var metaDataList = new List<MetaData>();
            TotalFileSizeofImages = 0;
            try
            {
                _logger.LogInformation("Preparing ProcessGuide PhotoDocumentation Data");
                var processGuides =
                    (await _commonUtilService.GetEntityQueryResponse<PraxisProcessGuide>(
                        filter.FilterString,
                        filter.SortBy
                    )).Results;
                foreach (var processGuide in processGuides)
                {
                    _logger.LogInformation("ProcessGuide Title: {ProcessGuideTitle}", processGuide.Title);
                    try
                    {
                        var praxisForm = _repository.GetItem<AssignedTaskForm>
                                        (p => p.AssignedEntityId == processGuide.ItemId
                                        && p.ClonedFormId == processGuide.FormId
                                        && p.AssignedEntityName == nameof(PraxisProcessGuide));
                        // var praxisForm = _repository.GetItem<PraxisForm>(f => f.ItemId.Equals(processGuide.FormId));
                        var clientNames = processGuide?.Clients?.Select(c => c.ClientName) ?? new List<string>();
                        var processGuideForReport = new PraxisProcessGuideForReport
                        {
                            FormName = praxisForm.Title,
                            ClientNames = string.Join(", ", clientNames),
                            PatientName = processGuide.PatientName,
                            PatientId = processGuide.PatientId,
                            TopicValue = processGuide.TopicValue,
                            Description = praxisForm.Description,
                            Title = processGuide.Title,
                            PatientDateOfBirth = processGuide.PatientDateOfBirth,
                            DueDate = processGuide.DueDate,
                            CompletionStatus = processGuide.CompletionStatus,
                            CompletionDate = processGuide.CompletionDate,
                            EffectiveCost = 0
                        };

                        processGuideForReport.Budget = praxisForm?.ProcessGuideCheckList
                            ?.SelectMany(cl => cl?.ProcessGuideTask?.Select(task => task.Budget ?? 0) ?? new List<double>())?
                            .Sum() ?? 0;

                        var praxisUsers = await GetPraxisUserListFromClientIds(
                            processGuide.Clients?.Select(cl => cl.ClientId) ?? new List<string>()
                        );
                        var processGuideAnswers = _repository.GetItems<PraxisProcessGuideAnswer>(
                                answer => answer.ProcessGuideId == processGuide.ItemId
                            )
                            .ToList();
                        var answersOrganizedByQuestionId = await OrganizeAnswersByQuestionIdAsync(
                            processGuideAnswers, praxisUsers
                        );

                        var clientInfoListForReport = await Task.WhenAll((processGuide.Clients ?? new List<ProcessGuideClientInfo>()).Select(
                            async client =>
                            {
                                var checkListForClient = praxisForm?.ProcessGuideCheckList?.First(
                                    cl => cl.ClientId == client.ClientId || (cl.ClientInfos != null && cl.ClientInfos.Any(c => c.ClientId == client.ClientId))
                                ) ?? new ClientSpecificCheckList();
                                var clientInfoForReport = new ProcessGuideClientInfoForReport
                                {
                                    ClientId = client.ClientId,
                                    ClientName = client.ClientName,
                                    CategoryName = client.CategoryName,
                                    SubCategoryName = client.SubCategoryName,
                                    CompletionPercentage = processGuide?.ClientCompletionInfo?
                                        .Find(c => c.ClientId == client.ClientId)
                                        ?.CompletionPercentage ?? 0,
                                    CheckList = await Task.WhenAll(checkListForClient?.ProcessGuideTask?.Select(
                                        async task =>
                                            new ProcessGuideTaskForReport
                                            {
                                                TaskTitle = task.ProcessGuideTaskTitle,
                                                TaskResponseList =
                                                    answersOrganizedByQuestionId.ContainsKey(task.ProcessGuideTaskId)
                                                        ? answersOrganizedByQuestionId[task.ProcessGuideTaskId]
                                                        : new List<PraxisProcessGuideSingleAnswerWithPraxisUserInfo>(),
                                                Budget = task.Budget,
                                                Files = await ProcessFilesForReportAsync(task?.Files?.ToList() ?? new List<PraxisDocument>())
                                            }
                                    )),
                                    Budget = 0,
                                    EffectiveCost = 0
                                };
                                var completedUserNames = clientInfoForReport?.CheckList?.SelectMany(
                                    task => task.TaskResponseList.Select(res => res.SubmittedBy)
                                )?.Distinct() ?? new List<string>();
                                clientInfoForReport.CompletedUsers = string.Join(", ", completedUserNames);

                                IEnumerable<PraxisUser> praxisUsersForClient;
                                if (client.HasSpecificControlledMembers)
                                {
                                    praxisUsersForClient = client?.ControlledMembers?.Select(
                                        praxisUserId => GetPraxisUser(praxisUsers, praxisUserId)
                                    ) ?? new List<PraxisUser>();
                                }
                                else
                                {
                                    praxisUsersForClient = praxisUsers.Where(
                                        praxisUser => praxisUser.ClientList.Any(c => c.ClientId.Equals(client.ClientId)) && !(praxisUser.Roles != null && praxisUser.Roles.Contains(RoleNames.GroupAdmin))
                                    );
                                }
                                clientInfoForReport.AssignedUsers = string.Join(", ", (praxisUsersForClient?.Select(praxisUser => praxisUser.DisplayName) ?? new List<string>()));
                                return clientInfoForReport;
                            }
                        ));

                        processGuideForReport.Clients = clientInfoListForReport ?? new ProcessGuideClientInfoForReport[] { };
                        foreach (var client in processGuideForReport.Clients)
                        {
                            if (client.CheckList != null)
                            {
                                foreach (var task in client.CheckList)
                                {
                                    task.EffectiveCost = task?.TaskResponseList?.Select(res => res.ActualBudget ?? 0).Sum() ?? 0;
                                    client.Budget += (task.Budget ?? 0);
                                    client.EffectiveCost += (task.EffectiveCost ?? 0);
                                }
                            }

                            processGuideForReport.EffectiveCost += (client.EffectiveCost ?? 0);
                        }

                        response.Add(processGuideForReport);

                        if (TotalFileSizeofImages > MaxTotalFileSizeofImagesInMb)
                        {
                            _logger.LogInformation("Image file size exceeded" + MaxTotalFileSizeofImagesInMb + "mb");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            $"Error occured while preparing ProcessGuide PhotoDocumentation Data for '{processGuide.Title}'"
                        );
                        _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");
                    }
                }

                metaDataList.Add(new MetaData()
                {
                    Name = "ProcessGuides",
                    Values = response.Select(processGuide =>
                            JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                JsonConvert.SerializeObject(processGuide)))
                        .ToList()
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while trying to prepare ProcessGuide PhotoDocumentation Data");
                _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");
            }

            bool isFileSizeExceeded = false;
            if (TotalFileSizeofImages > MaxTotalFileSizeofImagesInMb)
            {
                isFileSizeExceeded = true;
                Console.WriteLine("Images FileSize exceeded" + MaxTotalFileSizeofImagesInMb + "mb");
            }

            return new PraxisGenericReportResult()
            {
                MetaDataList = metaDataList,
                ClientIds = response.SelectMany(client => client.Clients.Select(pg => pg.ClientId)).Distinct(),
                IsFileSizeExceeded = isFileSizeExceeded,
                PraxisProcessGuidesForReport = response
            };
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation(
                $"Going to delete {nameof(PraxisProcessGuide)} and " +
                $"{nameof(PraxisProcessGuideAnswer)} for client {clientId}"
            );
            try
            {
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var admin = await _repository.GetItemAsync<PraxisUser>(pu => pu.Roles.Contains("admin"));

                var processGuides = _repository.GetItems<PraxisProcessGuide>(processGuide =>
                    processGuide.Clients.Any(client => client.ClientId.Equals(clientId))
                );

                var multipleClientProcessGuides = processGuides.Where(processGuide => processGuide.Clients.Count() > 1);
                foreach (var processGuide in multipleClientProcessGuides)
                {
                    processGuide.Clients = processGuide.Clients.Where(client => !client.ClientId.Equals(clientId));
                    processGuide.ClientCompletionInfo = processGuide.ClientCompletionInfo
                        .Where(client => !client.ClientId.Equals(clientId))
                        .ToList();
                    if (processGuide.ClientId.Equals(clientId))
                    {
                        if (!processGuide.Clients.Any())
                        {
                            processGuide.ClientId = processGuide.Clients.ElementAt(0).ClientId;
                            processGuide.ClientName = processGuide.Clients.ElementAt(0).ClientName;
                        }
                        else
                        {
                            processGuide.ClientId = admin.ClientId;
                            processGuide.ClientName = admin.ClientName;
                        }
                    }

                    processGuide.LastUpdateDate = DateTime.Now;
                    processGuide.LastUpdatedBy = userId;
                    await _repository.UpdateAsync(pg => pg.ItemId.Equals(processGuide.ItemId), processGuide);
                }

                var singleClientProcessGuideIds = processGuides.Where(processGuide => processGuide.Clients.Count() == 1)
                    .Select(processGuide => processGuide.ItemId)
                    .ToList();
                if (singleClientProcessGuideIds.Any())
                {
                    await _repository.DeleteAsync<PraxisProcessGuide>(processGuide =>
                        singleClientProcessGuideIds.Contains(processGuide.ItemId)
                    );
                }

                await _repository.DeleteAsync<PraxisProcessGuideAnswer>(processGuideAnswer =>
                    processGuideAnswer.ClientId.Equals(clientId));
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"Error occurred while trying to delete {nameof(PraxisProcessGuide)} and " +
                    $"{nameof(PraxisProcessGuideAnswer)}for client {clientId}  " +
                    $"Error: {e.Message} Stacktrace: {e.StackTrace}"
                );
            }
        }

        private async Task<List<PraxisUser>> GetPraxisUserListFromClientIds(IEnumerable<string> clientIds)
        {
            var clientFilterString = "{Active: true, Roles:{$nin: [\"" + $"{RoleNames.GroupAdmin}" + "\"]}, \"ClientList.ClientId\": {$in: [\"" +
                                     $"{string.Join("\", \"", clientIds)}" + "\"]}}";

            return (await _commonUtilService.GetEntityQueryResponse<PraxisUser>(
                clientFilterString, "{DisplayName: 1}"
            )).Results.ToList();
        }
        private IEnumerable<string> GetProcessGuideControlledMembers(PraxisProcessGuide guide)
        {
            if (guide == null) return Enumerable.Empty<string>();

            var controlledMembers = guide.ControlledMembers ?? Enumerable.Empty<string>();

            if (guide.Clients?.Any() == true)
            {
                var clientControlledMembers = guide.Clients
                    .Where(client => client.ControlledMembers?.Any() == true)
                    .SelectMany(client => client.ControlledMembers);

                return controlledMembers.Concat(clientControlledMembers).Distinct();
            }

            return controlledMembers.Distinct();
        }

        public async Task<bool> UpdateRowLevelSecurity(string processGuideId)
        {
            try
            {
                var praxisProcessGuide = await Task.Run(() => _repository.GetItem<PraxisProcessGuide>(
                    processGuide => processGuide.ItemId.Equals(processGuideId)
                ));

                if (praxisProcessGuide == null) return false;

                var permission = new EntityReadWritePermission
                {
                    Id = Guid.Parse(processGuideId)
                };
                permission.RolesAllowedToReadForRemove.AddRange(new[] { RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung, RoleNames.AppUser });
                permission.RolesAllowedToUpdateForRemove.AddRange(new[] { RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung });
                var assignedPraxisUserIds = GetProcessGuideControlledMembers(praxisProcessGuide)?.ToList() ?? new List<string>();

                foreach (var client in praxisProcessGuide.Clients)
                {
                    if (!assignedPraxisUserIds.Any())
                    {
                        var clientReadAccessRole = _mongoSecurityService.GetRoleName(RoleNames.MpaGroup_Dynamic, client.ClientId);
                        permission.RolesAllowedToRead.Add(clientReadAccessRole);
                    }

                    var clientAdminAccessRole = _mongoSecurityService.GetRoleName(RoleNames.PowerUser_Dynamic, client.ClientId);
                    var clientManagerAccessRole = _mongoSecurityService.GetRoleName(RoleNames.Leitung_Dynamic, client.ClientId);

                    permission.RolesAllowedToRead.AddRange(new[] { clientAdminAccessRole, clientManagerAccessRole });
                    permission.RolesAllowedToUpdate.AddRange(new[] { clientAdminAccessRole, clientManagerAccessRole });
                }

                if (assignedPraxisUserIds.Any())
                {
                    var praxisUsers = await Task.Run(() => _repository.GetItems<PraxisUser>(pu => assignedPraxisUserIds.Contains(pu.ItemId)).ToList());

                    if (praxisUsers?.Any() == true)
                    {
                        var ids = praxisUsers.Select(x => x.UserId).ToList();
                        permission.IdsAllowedToRead.AddRange(ids);
                        permission.IdsAllowedToUpdate.AddRange(ids);
                    }
                }

                if (!string.IsNullOrEmpty(praxisProcessGuide.OrganizationId))
                {
                    var orgReadAccessRole = _mongoSecurityService.GetRoleName(RoleNames.Organization_Read_Dynamic, praxisProcessGuide.OrganizationId);
                    permission.RolesAllowedToRead.Add(orgReadAccessRole);
                    var praxisClients = _repository.GetItems<PraxisClient>(cl => cl.ParentOrganizationId.Equals(praxisProcessGuide.OrganizationId)).ToList();
                    if (praxisClients?.Any() == true)
                    {
                        foreach (var client in praxisClients)
                        {
                            var orgClientReadRole = _mongoSecurityService.GetRoleName(RoleNames.MpaGroup_Dynamic, client.ItemId);
                            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(RoleNames.PowerUser_Dynamic, client.ItemId);
                            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(RoleNames.Leitung_Dynamic, client.ItemId);

                            permission.RolesAllowedToRead.AddRange(new[] { orgClientReadRole, clientAdminAccessRole, clientManagerAccessRole });
                            permission.RolesAllowedToUpdate.AddRange(new[] { clientAdminAccessRole, clientManagerAccessRole });
                        }
                    }
                }
                await Task.Run(() => _mongoSecurityService.UpdateEntityReadWritePermission<PraxisProcessGuide>(permission));
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred while trying to update ProcessGuide UpdateRowLevelSecurity Data Id {processGuideId}");
                _logger.LogError($"Error message: {e.Message} StackTrace: {e.StackTrace}");
                return false;
            }
        }



    }
}