using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.DynamicRolePrefix;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class TaskManagementService : ITaskManagementService
    {
        private readonly ILogger<TaskManagementService> _logger;
        private readonly IMongoSecurityService mongoSecurityService;
        private readonly IRepository repository;
        private readonly IMongoClientRepository mongoClientRepository;
        private readonly IServiceClient _serviceClient;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly string _taskManagementServiceBaseUrl;
        private readonly string _taskManagementServiceVersion;
        private readonly string _updateTaskPath;
        private readonly string _removeTaskSchedulePath;

        public TaskManagementService(
            ILogger<TaskManagementService> logger,
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            IMongoClientRepository mongoClientRepository,
            IServiceClient serviceClient,
            IConfiguration configuration,
            ISecurityContextProvider securityContextProvider)
        {
            _logger = logger;
            this.mongoSecurityService = mongoSecurityService;
            this.repository = repository;
            this.mongoClientRepository = mongoClientRepository;
            _serviceClient = serviceClient;
            _securityContextProvider = securityContextProvider;
            _taskManagementServiceBaseUrl = configuration["TaskManagementServiceBaseUrl"];
            _taskManagementServiceVersion = configuration["TaskManagementServiceVersion"];
            _updateTaskPath = configuration["UpdateTaskPath"];
            _removeTaskSchedulePath = configuration["RemoveTaskSchedulePath"];
        }

        public void AddTaskScheduleRowLevelSecurity(string taskScheduleId, string clientId)
        {
            var clientAdminAccessRole = mongoSecurityService.GetRoleName(PraxisClientAdmin, clientId);
            var clientReadAccessRole = mongoSecurityService.GetRoleName(PraxisClientRead, clientId);
            var clientManagerAccessRole =
                mongoSecurityService.GetRoleName(PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(taskScheduleId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);
            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);

            mongoSecurityService.UpdateEntityReadWritePermission<TaskSchedule>(permission);
        }

        public void AddTaskSummaryRowLevelSecurity(string taskSummaryId, string clientId)
        {
            var clientAdminAccessRole = mongoSecurityService.GetRoleName(PraxisClientAdmin, clientId);
            var clientReadAccessRole = mongoSecurityService.GetRoleName(PraxisClientRead, clientId);
            var clientManagerAccessRole = mongoSecurityService.GetRoleName(PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(taskSummaryId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);
            permission.RolesAllowedToUpdate.Add(clientReadAccessRole);
            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);

            mongoSecurityService.UpdateEntityReadWritePermission<TaskSummary>(permission);
        }

        public TaskSummary GetTaskSummary(string itemId)
        {
            return repository.GetItem<TaskSummary>(p => p.ItemId.Equals(itemId) && !p.IsMarkedToDelete);
        }

        public async Task<List<TaskSummary>> GetTaskSummarys(List<string> taskSummaryIds)
        {
            var filter = Builders<TaskSummary>.Filter.In(summary => summary.ItemId, taskSummaryIds) &
                         Builders<TaskSummary>.Filter.Eq(s => s.IsMarkedToDelete, false);

            var results = await mongoClientRepository.GetCollection<TaskSummary>().Find(filter).ToListAsync();

            return results;
        }

        public async Task<bool> UpdateTask(dynamic updateModel)
        {
            try
            {
                var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _taskManagementServiceBaseUrl, _taskManagementServiceVersion, _updateTaskPath,
                    updateModel,
                    _securityContextProvider.GetSecurityContext().OauthBearerToken
                );

                if (response.HttpStatusCode == HttpStatusCode.OK && response.StatusCode == 0 && response.Errors.IsValid)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("TaskManagementService http call -> Error occurred during UpdateTask call. Error: {Error}", JsonConvert.SerializeObject((object)response));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("TaskManagementService http call->Exception occurred in UpdateTask call: {UpdateModel}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", JsonConvert.SerializeObject((object)updateModel), ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> RemoveTask(dynamic updateModel)
        {
            try
            {
                var response = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _taskManagementServiceBaseUrl,
                    _taskManagementServiceVersion,
                    _removeTaskSchedulePath,
                    updateModel,
                    _securityContextProvider.GetSecurityContext().OauthBearerToken
                );
                if (response.HttpStatusCode == HttpStatusCode.OK && response.StatusCode == 0 && response.Errors.IsValid)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("TaskManagementService http call -> Error occurred during RemoveTask call. Error: {Error}", JsonConvert.SerializeObject((object)response));
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("TaskManagementService http call->Exception occurred in RemoveTask call: {UpdateModel}. Exception Message: {ErrorMessage}. Exception Details: {StackTrace}.", JsonConvert.SerializeObject((object)updateModel), ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}