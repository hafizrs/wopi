using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule
{
    public class QuickTaskAssignService : IQuickTaskAssignService
    {
        private readonly IRepository _repository;
        private readonly ILogger<QuickTaskAssignService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly string _taskManagementServiceBaseUrl;
        private readonly IServiceClient _serviceClient;
        private readonly AccessTokenProvider _accessTokenProvider;
        // Add cockpit summary or other dependencies as needed

        public QuickTaskAssignService(
            IRepository repository,
            ILogger<QuickTaskAssignService> logger,
            ISecurityContextProvider securityContextProvider,
            IConfiguration configuration,
            AccessTokenProvider accessTokenProvider,
            IServiceClient serviceClient
            // Add cockpit summary or other dependencies as needed
        )
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _taskManagementServiceBaseUrl = configuration["TaskManagementServiceBaseUrl"];
            _serviceClient = serviceClient;
            _accessTokenProvider = accessTokenProvider;
        }

        public async Task AssignTasks(RiqsQuickTaskPlan quickTaskPlan)
        {
            var taskTitles = quickTaskPlan.QuickTaskShift.TaskList.Select(t => t.Title).ToList();
            string departmentId = quickTaskPlan.DepartmentId;

            var endDate = DateTime.UtcNow;
            var startDate = endDate.Date;

            var formIds = quickTaskPlan.QuickTaskShift?.TaskList?
                .SelectMany(task => task?.AttachedFormInfos?.Select(a => a.FormId)?.ToList() ?? new List<string>())?
                .ToList() ?? new List<string>();
            foreach (var formId in formIds)
            {
                try
                {
                    if (string.IsNullOrEmpty(formId)) continue;
                    var processGuide = _repository
                          .GetItems<PraxisProcessGuide>(pg =>
                                !pg.IsMarkedToDelete &&
                                pg.FormId == formId &&
                                pg.CreateDate >= startDate &&
                                pg.CreateDate <= endDate &&
                                pg.ClientId == departmentId &&
                                pg.Shifts != null &&
                                pg.RelatedEntityName == nameof(RiqsQuickTaskPlan))
                          .FirstOrDefault();

                    var tasks = quickTaskPlan.QuickTaskShift?.TaskList?
                        .Where(t => t.AttachedFormInfos != null && t.AttachedFormInfos.Any(a => a.FormId == formId))?
                        .Select(t => new PraxisShift
                        {
                            ItemId = t.ItemId,
                            Name = t.Title,
                        })?
                        .ToList() ?? new List<PraxisShift>();

                    if (processGuide == null)
                    {
                        await AssignTask(tasks, formId, departmentId, quickTaskPlan.ItemId);
                    }
                    else
                    {
                        await UpdateProcessGuide(tasks, processGuide, quickTaskPlan);
                        await UpdateQuickTaskPlanForProcessGuideCreated(new List<string> { quickTaskPlan.ItemId }, processGuide.ItemId, formId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    _logger.LogError("Failed to assign task for {formId} for DepartmentId -> {DepartmentId}", formId, departmentId);
                }
            }
        }

        private async Task UpdateProcessGuide(List<PraxisShift> tasks, PraxisProcessGuide processGuide, RiqsQuickTaskPlan plan)
        {
            var controllMembers = (List<string>)processGuide.ControlledMembers;
            controllMembers.AddRange(plan.AssignedUsers);
            controllMembers = controllMembers.Distinct().ToList();
            var clientList = (List<ProcessGuideClientInfo>)processGuide.Clients;
            if (clientList != null)
            {
                clientList[0].ControlledMembers = controllMembers;
            }
            processGuide.Clients = clientList;
            processGuide.ControlledMembers = controllMembers;
            var shifts = processGuide.Shifts?.ToList() ?? new List<PraxisShift>();
            shifts.AddRange(tasks);
            processGuide.Shifts = shifts;
            processGuide.MetaDataList ??= new List<MetaDataKeyPairValue>();
            var relatedEnttityIdsData = processGuide.MetaDataList.FirstOrDefault(m => m.Key == "RelatedEntityIds");
            if (relatedEnttityIdsData != null)
            {
                var relatedEntityIds = JsonConvert.DeserializeObject<List<string>>(relatedEnttityIdsData?.MetaData?.Value ?? JsonConvert.SerializeObject(new List<string>()));
                relatedEntityIds.Add(plan.ItemId);
                relatedEnttityIdsData.MetaData.Value = JsonConvert.SerializeObject(relatedEntityIds.Distinct().ToList());
            }
            else
            {
                relatedEnttityIdsData = new MetaDataKeyPairValue
                {
                    Key = "RelatedEntityIds",
                    MetaData = new MetaValuePair { Type = "string", Value = JsonConvert.SerializeObject(new List<string> { plan.ItemId }) }
                };
                processGuide.MetaDataList.Add(relatedEnttityIdsData);
            }
            await _repository.UpdateAsync(pg => pg.ItemId == processGuide.ItemId, processGuide);
        }

        public async Task UpdateQuickTaskPlanForProcessGuideCreated(List<string> quickTaskPlanIds, string processGuideId, string formId)
        {
            var quickTaskPlans = _repository.GetItems<RiqsQuickTaskPlan>(t => quickTaskPlanIds.Contains(t.ItemId)).ToList();
            if (quickTaskPlans?.Count > 0)
            {
                foreach (var quickTaskPlan in quickTaskPlans)
                {
                    if (quickTaskPlan.QuickTaskShift?.TaskList?.Any() != true) return;

                    foreach (var task in quickTaskPlan.QuickTaskShift.TaskList)
                    {
                        if (task.AttachedFormInfos == null)
                            continue;
                        foreach (var form in task.AttachedFormInfos)
                        {
                            if (form.FormId == formId)
                            {
                                form.ProcessGuideId = processGuideId;
                                form.IsProcessGuideCreated = true;
                            }
                        }
                    }
                    await _repository.UpdateAsync(sp => sp.ItemId == quickTaskPlan.ItemId, quickTaskPlan);
                }
            }
        }

        private async Task AssignTask(List<PraxisShift> tasks, string formId, string departmentId, string quickTaskPlanId)
        {
            var praxisForm = await _repository.GetItemAsync<PraxisForm>(s => s.ItemId == formId);
            if (praxisForm == null)
            {
                _logger.LogError("PraxisForm not found with formId for quickTask: {formId}", formId);
                return;
            }
            var processGuideConfig = await SaveNewProcessGuideConfig(formId, praxisForm, departmentId);
            var taskSchedulerModel = GetTaskSchedulerModel(processGuideConfig, praxisForm.Description, tasks, quickTaskPlanId);

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_taskManagementServiceBaseUrl + "TaskManagementService/TaskManagementCommand/CreateTaskSchedule"),
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(taskSchedulerModel),
                        Encoding.UTF8,
                        "application/json")
                };
                var token = await GetAdminToken();
                request.Headers.Add("Authorization", $"bearer {token}");

                HttpResponseMessage response = await _serviceClient.SendToHttpAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to create process guide for quick task plan with formId -> {id}", formId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message} StackTrace: {StackTrace} Failed to create process guide for quick task plan with formId -> {id}",
                    ex.Message, ex.StackTrace, formId);
            }
        }

        private List<string> GetAssignedUserIds(string formId, string departmentId)
        {
            var utcDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
            var userIds = _repository.GetItems<RiqsQuickTaskPlan>
                (sp => sp.QuickTaskDate >= utcDate && sp.QuickTaskDate <= utcDate && sp.QuickTaskShift.TaskList != null &&
                sp.QuickTaskShift.TaskList.Any(t => t.AttachedFormInfos != null && t.AttachedFormInfos.Any(a => a.FormId == formId) && t.DepartmentId == departmentId))
                .SelectMany(sp => sp.AssignedUsers).Distinct().ToList();
            return userIds;
        }

        private List<ProcessGuideClientInfo> GetUserClients(List<string> userIds, string departmentId)
        {
            var clients = new List<ProcessGuideClientInfo>();
            var userClient = _repository.GetItem<PraxisClient>(c => c.ItemId == departmentId);
            var client = new ProcessGuideClientInfo()
            {
                ClientId = userClient.ItemId,
                ClientName = userClient.ClientName,
                CategoryId = "",
                SubCategoryId = "",
                SubCategoryName = "",
                ControlledMembers = userIds,
                HasSpecificControlledMembers = true
            };
            clients.Add(client);

            return clients;
        }

        private async Task<PraxisProcessGuideConfig> SaveNewProcessGuideConfig(string formId, PraxisForm praxisForm, string departmentId)
        {
            var assignedUSerIds = GetAssignedUserIds(formId, departmentId);
            var Assignedclients = GetUserClients(assignedUSerIds, departmentId);

            var utcDate = DateTime.UtcNow;
            var subissionDates = new List<DateTime>() { utcDate };
            var taskTimeTable = new PraxisTaskTimetable();
            taskTimeTable.SubmissionDates = subissionDates;

            var newPgConfig = new PraxisProcessGuideConfig()
            {
                ItemId = Guid.NewGuid().ToString(),
                FormId = praxisForm.ItemId,
                TopicKey = praxisForm.TopicKey,
                TopicValue = praxisForm.TopicValue,
                Title = praxisForm.Title,
                TaskTimetable = taskTimeTable,
                Clients = Assignedclients,
                ControlledMembers = assignedUSerIds,
                DueDate = utcDate,
                RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser },
                RolesAllowedToUpdate = new string[] { RoleNames.Admin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung },
                RolesAllowedToDelete = new string[] { RoleNames.Admin },
                IdsAllowedToDelete = new string[] { _securityContextProvider.GetSecurityContext().UserId }
            };

            await _repository.SaveAsync(newPgConfig);

            return newPgConfig;
        }

        private CreateTaskScheduleRequestModel GetTaskSchedulerModel(PraxisProcessGuideConfig processGuideConfig, string formName, List<PraxisShift> shifts, string quickTaskPlanId)
        {
            var taskDatas = new List<TaskData>();
            var taskData = new TaskData()
            {
                HasRelatedEntity = true,
                HasTaskScheduleIntoRelatedEntity = true,
                RelatedEntityName = nameof(PraxisProcessGuide),
                TaskSummaryId = Guid.NewGuid().ToString(),
                Title = processGuideConfig.Title,
                RelatedEntityObject = GetRelatedEngityObject(processGuideConfig, formName, shifts, quickTaskPlanId)
            };
            taskDatas.Add(taskData);

            var submissionDates = new List<string>() { DateTime.UtcNow.ToString("yyyy-MM-dd") };
            var taskScheduleDetails = new TaskScheduleDetails()
            {
                HasToMoveNextDay = true,
                IsRepeat = false,
                SubmissionDates = submissionDates
            };

            return new CreateTaskScheduleRequestModel()
            {
                TaskScheduleDetails = taskScheduleDetails,
                TaskDatas = taskDatas,
                AssignMembers = new List<object>()
            };
        }

        private RelatedEntityObject GetRelatedEngityObject(PraxisProcessGuideConfig processGuideConfig, string formName, List<PraxisShift> shifts, string quickTaskPlanId)
        {
            return new RelatedEntityObject()
            {
                ItemId = Guid.NewGuid().ToString(),
                FormId = processGuideConfig.FormId,
                FormName = formName,
                Title = processGuideConfig.Title,
                Tags = new[] { "Is-Valid-PraxisProcessGuide" },
                Language = "en-US",
                TopicKey = processGuideConfig.TopicKey,
                TopicValue = processGuideConfig.TopicValue,
                Description = processGuideConfig.Title,
                PatientDateOfBirth = DateTime.UtcNow,
                IsActive = true,
                ControlledMembers = processGuideConfig.ControlledMembers,
                Clients = processGuideConfig.Clients,
                ClientId = processGuideConfig.Clients.FirstOrDefault()?.ClientId,
                ClientName = processGuideConfig.Clients.FirstOrDefault()?.ClientName,
                DueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PraxisProcessGuideConfigId = processGuideConfig.ItemId,
                Shifts = shifts,
                RelatedEntityId = quickTaskPlanId,
                RelatedEntityName = nameof(RiqsQuickTaskPlan)
            };
        }
        private async Task<string> GetAdminToken()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var tokenInfo = new TokenInfo
            {
                UserId = "1bb370d7-7d42-4e9a-afde-9382fa96c417",
                TenantId = securityContext.TenantId,
                SiteId = securityContext.SiteId,
                SiteName = securityContext.SiteName,
                Origin = securityContext.RequestOrigin,
                DisplayName = "Kawsar Ahmed",
                UserName = "kawsar.ahmed@selise.ch",
                Language = securityContext.Language,
                PhoneNumber = securityContext.PhoneNumber,
                Roles = new List<string> { RoleNames.Admin, RoleNames.SystemAdmin, RoleNames.Tenantadmin }
            };
            var accessToken = await _accessTokenProvider.CreateForUserAsync(tokenInfo);
            return accessToken;
        }
    }
} 