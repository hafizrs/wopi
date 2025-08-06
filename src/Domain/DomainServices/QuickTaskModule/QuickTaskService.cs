using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.HuberSchindler;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.QuickTaskModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule
{
    public class QuickTaskService : IQuickTaskService
    {
        private readonly ILogger<QuickTaskService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IQuickTaskPermissionService _quickTaskPermissionService;
        private readonly IQuickTaskAssignService _quickTaskAssignService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly object _lock = new object();


        public QuickTaskService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IQuickTaskPermissionService quickTaskPermissionService,
            IQuickTaskAssignService quickTaskAssignService,
            ILogger<QuickTaskService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService

        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _quickTaskPermissionService = quickTaskPermissionService;
            _quickTaskAssignService = quickTaskAssignService;
            _logger = logger;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }

        public async Task CreateQuickTask(CreateQuickTaskCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var sequence = _repository.GetItems<RiqsQuickTask>(s => s.DepartmentId == command.DepartmentId).Count();

            var taskList = command.TaskList?.ToList() ?? new List<RiqsSingleQuickTask>();

            foreach (var task in taskList)
            {
                task.ItemId ??= Guid.NewGuid().ToString();
            }

            var newQuickTask = new RiqsQuickTask
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext.UserId,
                TenantId = securityContext.TenantId,
                Language = securityContext.Language,
                TaskGroupName = command.TaskGroupName,
                DepartmentId = command.DepartmentId,
                OrganizationId = command.OrganizationId,
                Sequence = ++sequence,
                Tags = new[] { PraxisTag.IsValidRiqsQuickTask },
                RolesAllowedToRead = _quickTaskPermissionService.GetRolesAllowedToRead(command.DepartmentId),
                RolesAllowedToUpdate = _quickTaskPermissionService.GetRolesAllowedToUpdate(command.DepartmentId),
                RolesAllowedToDelete = _quickTaskPermissionService.GetRolesAllowedToDelete(command.DepartmentId),
                TaskList = taskList
            };

            await _repository.SaveAsync(newQuickTask);
            //_genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(quickTask);
        }

        public List<RiqsQuickTaskResponse> GetQuickTasks(string departmentId)
        {
            var tasks = _repository.GetItems<RiqsQuickTask>(s => s.DepartmentId == departmentId).OrderBy(s => s.Sequence).ToList();
            var responseList = new List<RiqsQuickTaskResponse>();
            foreach (var task in tasks)
            {
                var response = new RiqsQuickTaskResponse(task);
                responseList.Add(response);
            }
            return responseList;
        }

        public List<RiqsQuickTask> GetQuickTaskDropdown(string departmentId)
        {
            return _repository.GetItems<RiqsQuickTask>(s => s.DepartmentId == departmentId).OrderBy(s => s.Sequence).ToList();
        }

        public async Task CreateQuickTaskPlan(CreateQuickTaskPlanCommand command)
        {
            foreach (var quickTaskPlan in command.QuickTaskPlans)
            {
                var quickTaskDate = quickTaskPlan.QuickTaskDate.Date;
                var quickTaskDateUtc = DateTime.SpecifyKind(quickTaskDate, DateTimeKind.Utc);
                var securityContext = _securityContextProvider.GetSecurityContext();
                var quickTask = GetQuickTaskById(quickTaskPlan.QuickTaskId);

                var existingPlan = _repository.GetItems<RiqsQuickTaskPlan>(s => s.QuickTaskShift.ItemId == quickTask.ItemId && s.QuickTaskDate == quickTaskDate).FirstOrDefault();
                string planId = existingPlan?.ItemId ?? string.Empty;

                if (existingPlan == null)
                {
                    var newPlan = new RiqsQuickTaskPlan
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = securityContext.UserId,
                        TenantId = securityContext.TenantId,
                        Language = securityContext.Language,
                        QuickTaskShift = quickTask,
                        QuickTaskDate = quickTaskDate,
                        AssignedUsers = quickTaskPlan.AssignedUsers,
                        TimezoneOffsetInMinutes = quickTaskPlan.TimezoneOffsetInMinutes,
                        DepartmentId = quickTask.DepartmentId,
                        OrganizationId = quickTask.OrganizationId ?? GetOrganisationId(quickTask.DepartmentId),
                        Tags = new[] { PraxisTag.IsValidRiqsQuickTaskPlan },
                        RolesAllowedToRead = _quickTaskPermissionService.GetRolesAllowedToRead(quickTask.DepartmentId),
                        RolesAllowedToUpdate = _quickTaskPermissionService.GetRolesAllowedToUpdate(quickTask.DepartmentId),
                        RolesAllowedToDelete = _quickTaskPermissionService.GetRolesAllowedToDelete(quickTask.DepartmentId),
                    };
                    await _repository.SaveAsync(newPlan);
                    planId = newPlan.ItemId;

                    if (quickTaskPlan?.CloneToDates?.Count > 0 && !string.IsNullOrEmpty(command.DepartmentId))
                    {
                        var cloneToDates = quickTaskPlan.CloneToDates.Select(cloneToDate => DateTime.SpecifyKind(cloneToDate, DateTimeKind.Utc)).ToList();
                        if (cloneToDates.Count > 0)
                        {
                            var dates = cloneToDates.Where(date => date.ToShortDateString() != newPlan.QuickTaskDate.ToShortDateString()).ToList();
                            quickTaskPlan.CloneToDates = dates;
                        }
                    }

                    if (quickTaskPlan.AssignTask)
                    {
                        await _quickTaskAssignService.AssignTasks(newPlan);
                    }

                    var localDate = DateTime.UtcNow.AddMinutes(quickTaskPlan.TimezoneOffsetInMinutes);
                    _logger.LogInformation("Shift Date: {ShiftDate}, Local Date: {UtcDate}", quickTaskDate, localDate);
                    if (quickTaskDateUtc.Date == localDate.Date)
                    {
                        //await _cockpitSummaryCommandService.CreateSummary(newPlan.ItemId, nameof(RiqsQuickTaskPlan));
                        var files = quickTask?.TaskList?.SelectMany(t => t.LibraryForms?
                            .Select(f => f.LibraryFormId).ToList() ?? new List<string>())?.ToList() ?? new List<string>();
                        var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
                        if (files.Any())
                        {
                            //await _cockpitDocumentActivityMetricsGenerationService
                            //    .OnDocumentUsedInShiftPlanGenerateActivityMetrics(files.ToArray(), activityName, newPlan.ItemId);
                        }
                    }
                }

                if (quickTaskPlan?.CloneToDates?.Count > 0 && !string.IsNullOrEmpty(command.DepartmentId))
                {
                    var clonePlansCommand = new CloneQuickTaskPlansCommand
                    {
                        CloneToDates = quickTaskPlan.CloneToDates,
                        QuickTaskPlanIds = new List<string> { planId },
                        DepartmentId = command.DepartmentId
                    };

                    await CloneQuickTaskPlans(clonePlansCommand);
                }
            }
        }

        public async Task UpdateQuickTaskSequence(string[] quickTaskIds)
        {
            var sequence = 1;
            foreach (var quickTaskId in quickTaskIds)
            {
                var quickTask = await _repository.GetItemAsync<RiqsQuickTask>(qt => qt.ItemId == quickTaskId);
                if (quickTask != null)
                {
                    quickTask.Sequence = sequence; // Set the sequence property
                    await _repository.UpdateAsync<RiqsQuickTask>(qt => qt.ItemId == quickTask.ItemId, quickTask);
                    await UpdateQuickTaskSequenceInPlan(quickTaskId, sequence);
                    sequence++;
                }
            }
            //TempQuickTaskFormPlanIdAssign();
        }

        private async Task UpdateQuickTaskSequenceInPlan(string quickTaskId, int sequence)
        {
            var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
            var plans = _repository.GetItems<RiqsQuickTaskPlan>(plan =>
                plan.QuickTaskShift.ItemId == quickTaskId &&
                (plan.QuickTaskShift == null || plan.QuickTaskShift.TaskList == null ||
                 !plan.QuickTaskShift.TaskList.Any(task => task.AttachedFormInfos != null &&
                     task.AttachedFormInfos.Any(info => info.IsProcessGuideCreated)))
            ).ToList();

            if (plans.Any())
            {
                foreach (var plan in plans)
                {
                    if (plan.QuickTaskDate.Date >= utcStartDate.Date)
                    {
                        plan.QuickTaskShift.Sequence = sequence;
                        await _repository.UpdateAsync<RiqsQuickTaskPlan>(qp => qp.ItemId == plan.ItemId, plan);
                    }
                }
            }
        }

        public List<QuickTaskPlanQueryResponse> GetQuickTaskPlans(GetQuickTaskPlanQuery query)
        {
            var utcStartDate = DateTime.SpecifyKind(query.StartDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(query.EndDate, DateTimeKind.Utc);
            var quickTaskPlans = _repository.GetItems<RiqsQuickTaskPlan>(sp =>
                    sp.QuickTaskDate >= utcStartDate && sp.QuickTaskDate <= utcEndDate &&
                    sp.DepartmentId == query.DepartmentId)
                .GroupBy(plan => plan.QuickTaskDate)
                .Select(g =>
                    new QuickTaskPlanQueryResponse
                    {
                        QuickTaskDate = g.Key,
                        QuickTaskPlans = g.Select(plan => new RiqsQuickTaskPlanResponse()
                        {
                            ItemId = plan.ItemId,
                            QuickTaskDate = plan.QuickTaskDate,
                            QuickTaskShift = new RiqsQuickTaskResponse()
                            {
                                ItemId = plan.QuickTaskShift.ItemId,
                                TaskGroupName = plan.QuickTaskShift.TaskGroupName,
                                TaskList = plan.QuickTaskShift.TaskList,
                                DepartmentId = plan.QuickTaskShift.DepartmentId,
                                OrganizationId = plan.QuickTaskShift.OrganizationId,
                                Sequence = plan.QuickTaskShift.Sequence
                            },
                            AssignedUsers = plan.AssignedUsers,
                            CompletionDate = plan.CompletionDate,
                            DepartmentId = plan.DepartmentId,
                            OrganizationId = plan.OrganizationId,
                        }).ToList()
                    })
                .ToList();

            PopulatePraxisUser(quickTaskPlans);
            SortQuickTaskPlans(quickTaskPlans);
            return quickTaskPlans;
        }

        public void SortQuickTaskPlans(List<QuickTaskPlanQueryResponse> items)
        {
            foreach (var item in items)
            {
                item.QuickTaskPlans = item.QuickTaskPlans.OrderBy(qtp => qtp.QuickTaskShift.Sequence).ToList();
            }
        }

        public async Task CloneQuickTaskPlan(CloneQuickTaskPlanCommand command)
        {
            if (command.CloneToDates != null)
            {
                var quickTask = GetQuickTaskById(command.QuickTaskId);
                if (quickTask == null)
                {
                    return;
                }

                var securityContext = _securityContextProvider.GetSecurityContext();
                var readRoles = _quickTaskPermissionService.GetRolesAllowedToRead(quickTask.DepartmentId);
                var deleteRoles = _quickTaskPermissionService.GetRolesAllowedToDelete(quickTask.DepartmentId);
                var updateRoles = _quickTaskPermissionService.GetRolesAllowedToUpdate(quickTask.DepartmentId);
                foreach (var plan in command.CloneToDates
                    .Select(date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
                    .Where(exDate => !IsQuickTaskPlanExist(exDate, quickTask.ItemId))
                    .Select(utcDate => new RiqsQuickTaskPlan
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = securityContext.UserId,
                        TenantId = securityContext.TenantId,
                        Language = securityContext.Language,
                        QuickTaskShift = quickTask,
                        QuickTaskDate = utcDate,
                        AssignedUsers = command.AssignedUsers,
                        DepartmentId = quickTask.DepartmentId,
                        OrganizationId = quickTask.OrganizationId,
                        Tags = new[] { PraxisTag.IsValidRiqsQuickTaskPlan },
                        RolesAllowedToRead = readRoles,
                        RolesAllowedToUpdate = updateRoles,
                        RolesAllowedToDelete = deleteRoles
                    }))
                {
                    await _repository.SaveAsync(plan);
                }
            }
        }

        public async Task CloneQuickTaskPlans(CloneQuickTaskPlansCommand command)
        {
            if (command.CloneToDates != null && command.QuickTaskPlanIds != null)
            {
                var quickTaskPlans = _repository
                    .GetItems<RiqsQuickTaskPlan>(qp =>
                        command.QuickTaskPlanIds.Contains(qp.ItemId)
                        && qp.DepartmentId == command.DepartmentId
                    );
                var securityContext = _securityContextProvider.GetSecurityContext();
                foreach (var quickTaskPlan in quickTaskPlans)
                {
                    await CloneQuickTaskPlan(new CloneQuickTaskPlanCommand()
                    {
                        QuickTaskId = quickTaskPlan.QuickTaskShift?.ItemId,
                        AssignedUsers = quickTaskPlan.AssignedUsers,
                        CloneToDates = command.CloneToDates
                    });
                }
            }
        }

        public RiqsQuickTaskPlanResponse GetQuickTaskPlanById(string id)
        {
            var plan = _repository.GetItems<RiqsQuickTaskPlan>(sp => sp.ItemId == id).FirstOrDefault();
            if (plan == null) return null;

            var planResponse = new RiqsQuickTaskPlanResponse(plan);
            planResponse.PraxisPersons = GetPraxisUsers(plan.AssignedUsers ?? new List<string>());

            var shiftCloned = plan?.QuickTaskShift != null
                ? _repository.GetItems<RiqsQuickTaskPlan>(sp =>
                      sp.QuickTaskShift != null &&
                      sp.QuickTaskShift.ItemId == plan.QuickTaskShift.ItemId &&
                      sp.QuickTaskDate >= plan.QuickTaskDate
                  )?.ToList()
                : new List<RiqsQuickTaskPlan>();

            if (shiftCloned?.Count > 0)
            {
                planResponse.CloneShiftDates = shiftCloned.Select(clone => new CloneShiftDate
                {
                    ItemId = clone.ItemId,
                    ShiftDate = clone.QuickTaskDate,
                }).OrderBy(o => o.ShiftDate).ToList();

                planResponse.ClonePraxisUserIds = shiftCloned
                  .Where(clone => clone.AssignedUsers != null)
                  .SelectMany(clone => clone.AssignedUsers.Select(userId => userId.ToString()))
                  .Distinct()
                  .ToList();
            }

            return planResponse;
        }

        public async Task UpdateQuickTaskPlan(UpdateQuickTaskPlanCommand command)
        {
            if (command?.QuickTaskPlanIds != null && command.QuickTaskPlanIds.Count() > 0)
            {
                var updateTasks = command.QuickTaskPlanIds.Select(itemId =>
                {
                    var updates = new Dictionary<string, object>
                        {
                            {"AssignedUsers", command.AssignedUsers}
                        };

                    return _repository.UpdateAsync<RiqsQuickTaskPlan>(sp => sp.ItemId == itemId, updates);
                });

                await Task.WhenAll(updateTasks);
            }
        }

        public async Task DeleteQuickTaskPlan(List<string> quickTaskPlanIds)
        {
            if (quickTaskPlanIds != null && quickTaskPlanIds.Count > 0)
            {
                var deleteTasks = quickTaskPlanIds.Select(async id =>
                {
                    await RemoveShiftProcessGuid(id);
                    await RemoveShiftLibraryForms(id);
                    await _repository.DeleteAsync<RiqsQuickTaskPlan>(sp => sp.ItemId == id);
                });
                await Task.WhenAll(deleteTasks);
                //await _cockpitSummaryCommandService.DeleteSummaryAsync(quickTaskPlanIds, CockpitTypeNameEnum.RiqsShiftPlan);
            }
        }

        public async Task DeleteQuickTask(string id)
        {
            var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
            var plans = _repository.GetItems<RiqsQuickTaskPlan>(plan =>
                plan.QuickTaskShift.ItemId == id &&
                (plan.QuickTaskShift == null ||  plan.QuickTaskShift.TaskList == null ||
                 !plan.QuickTaskShift.TaskList.Any(task => task.AttachedFormInfos != null &&
                     task.AttachedFormInfos.Any(info => info.IsProcessGuideCreated)))
            ).ToList();
            if (plans.Any())
            {
                foreach (var plan in plans)
                {
                    if (plan.QuickTaskDate.Date >= utcStartDate.Date)
                    {
                        await _repository.DeleteAsync<RiqsQuickTaskPlan>(sp => sp.ItemId == plan.ItemId);
                    }
                }
            }

            var task = _repository.GetItem<RiqsQuickTask>(s => s.ItemId == id);
            await _repository.DeleteAsync<RiqsQuickTask>(s => s.ItemId == id);

            if (task != null)
            {
                //_genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(task);
            }
        }

        public async Task EditQuickTask(EditQuickTaskCommand command)
        {
            var quickTask = _repository.GetItem<RiqsQuickTask>(s => s.ItemId == command.ItemId);

            if (quickTask != null)
            {
                quickTask.TaskGroupName = command.TaskGroupName;
                quickTask.TaskList = command.TaskList?.ToList() ?? new List<RiqsSingleQuickTask>();
                await _repository.UpdateAsync<RiqsQuickTask>(s => s.ItemId == command.ItemId, quickTask);
                //_genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(quickTask);
            }
        }

        public bool ValidateQuickTaskInfo(ValidateQuickTaskInfo query)
        {
            var exists = _repository.GetItems<RiqsQuickTask>()
                .Any(s => s.TaskGroupName == query.QuickTaskName && s.DepartmentId == query.DepartmentId);
            return !exists;
        }

        public bool ValidateQuickTaskPlanInfo(ValidateQuickTaskPlanInfoQuery query)
        {
            var existingPlans = _repository.GetItems<RiqsQuickTaskPlan>(s => s.QuickTaskShift.ItemId == query.QuickTaskPlanId).ToList();
            if (existingPlans.Count > 0)
            {
                var existingDates = existingPlans.Select(sp => sp.QuickTaskDate.Date).ToList();
                return !existingDates.Any(date => query.QuickTaskPlanId.Contains(date.ToString()));
            }
            return true;
        }

        public async Task UpdateLibraryFormResponse(ObjectArtifact artifact)
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
                        var isComplete = _objectArtifactUtilityService.IsACompletedFormResponse(metaData);


                        var originalFormId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                                                $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"]);

                        if (entityName == nameof(RiqsQuickTaskPlan) && !string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(taskId))
                        {
                            lock (_lock)
                            {
                                var quickTaskPlan = _repository.GetItem<RiqsQuickTaskPlan>(p => p.ItemId == entityId);
                                if (quickTaskPlan != null)
                                {
                                    var singleTask = quickTaskPlan?.QuickTaskShift?.TaskList?.Find(t => t.ItemId == taskId);
                                    if (singleTask == null) return;

                                    var libraryFormResponse = singleTask.LibraryFormResponses?.Find(l => l.OriginalFormId == originalFormId && l.CompletedBy == praxisUserId);
                                    if (libraryFormResponse != null)
                                    {
                                        libraryFormResponse.LibraryFormId = artifact.ItemId;
                                        libraryFormResponse.CompletedBy = praxisUserId;
                                        if (isComplete)
                                        {
                                            libraryFormResponse.IsComplete = isComplete;
                                            libraryFormResponse.CompletedOn = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        libraryFormResponse = new PraxisLibraryFormResponse()
                                        {
                                            OriginalFormId = originalFormId,
                                            LibraryFormId = artifact.ItemId,
                                            CompletedBy = praxisUserId
                                        };
                                        if (isComplete)
                                        {
                                            libraryFormResponse.IsComplete = isComplete;
                                            libraryFormResponse.CompletedOn = DateTime.UtcNow;
                                        }
                                        var responses = singleTask.LibraryFormResponses?.ToList() ?? new List<PraxisLibraryFormResponse>();
                                        responses.Add(libraryFormResponse);
                                        singleTask.LibraryFormResponses = responses;
                                    }
                                    _repository.Update(p => p.ItemId == quickTaskPlan.ItemId, quickTaskPlan);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in UpdateLibraryFormResponse: {ex.Message}");
            }
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            var deleteTasks = new List<Task>
            {
                _repository.DeleteAsync<RiqsQuickTask>(qt => qt.DepartmentId.Equals(clientId)),
                _repository.DeleteAsync<RiqsQuickTaskPlan>(qt => qt.DepartmentId.Equals(clientId))
            };
            await Task.WhenAll(deleteTasks);
        }

        private void PopulatePraxisUser(List<QuickTaskPlanQueryResponse> responses)
        {
            foreach (var response in responses)
            {
                foreach (var plan in response.QuickTaskPlans)
                {
                    plan.PraxisPersons = GetPraxisUsers(plan.AssignedUsers ?? new List<string>());
                }
            }
        }

        private List<PraxisUser> GetPraxisUsers(List<string> praxisUserIds)
        {
            return _repository.GetItems<PraxisUser>()
                .Where(user => praxisUserIds.Contains(user.ItemId))
                .ToList();
        }

        private RiqsQuickTask GetQuickTaskById(string quickTaskId)
        {
            return _repository.GetItems<RiqsQuickTask>(s => s.ItemId == quickTaskId).FirstOrDefault();
        }

        private bool IsQuickTaskPlanExist(DateTime utcDate, string quickTaskId)
        {
            var existingPlan = _repository
                .GetItems<RiqsQuickTaskPlan>(s => s.QuickTaskShift.ItemId == quickTaskId && s.QuickTaskDate == utcDate)
                .FirstOrDefault();
            return existingPlan != null;
        }

        private async Task RemoveShiftProcessGuid(string shiftPlanId)
        {
            //var shiftPlan = await _repository.GetItemAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlanId);
            //if (shiftPlan.IsProcessGuidCreated)
            //{
            //    foreach (var formId in shiftPlan.Shift.PraxisFormIds)
            //    {
            //        DateTime endDate = DateTime.UtcNow;
            //        DateTime startDate = endDate.Date;
            //        var processGuide = _repository
            //            .GetItems<PraxisProcessGuide>(pg =>
            //                !pg.IsMarkedToDelete &&
            //                pg.FormId == formId &&
            //                pg.CreateDate >= startDate &&
            //                pg.CreateDate <= endDate &&
            //                pg.ClientId == shiftPlan.Shift.DepartmentId &&
            //                pg.Shifts != null)
            //            .FirstOrDefault();

            //        if (processGuide != null)
            //        {
            //            var controllMembers = (List<string>)processGuide.ControlledMembers;
            //            controllMembers.Remove(shiftPlan.PraxisUserIds?.FirstOrDefault());

            //            var clientList = (List<ProcessGuideClientInfo>)processGuide.Clients;
            //            if (clientList != null)
            //            {
            //                clientList[0].ControlledMembers = controllMembers;
            //            }

            //            processGuide.Clients = clientList;
            //            processGuide.ControlledMembers = controllMembers;
            //            var shifts = (List<PraxisShift>)processGuide.Shifts;
            //            var shiftIndex = shifts?.FindIndex(x => x.ItemId == shiftPlan.Shift.ItemId);
            //            if (shiftIndex != null && shiftIndex != -1)
            //            {
            //                shifts.RemoveAt(shiftIndex.Value);
            //            }

            //            processGuide.Shifts = shifts;
            //            if (controllMembers.Count > 0)
            //            {
            //                await UpdateProcessGuide(processGuide);
            //            }
            //            else
            //            {
            //                await DeleteProcessGuide(processGuide.ItemId);
            //                await _cockpitFormDocumentActivityMetricsGenerationService
            //                    .OnDeleteTaskRemoveSummaryFromActivityMetrics(new List<string> { processGuide.ItemId }, nameof(PraxisProcessGuide));
            //            }
            //            //await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { processGuide.ItemId }, CockpitTypeNameEnum.PraxisProcessGuide);
            //        }
            //    }
            //}
        }

        private async Task RemoveShiftLibraryForms(string planId)
        {
            var plan = await _repository.GetItemAsync<RiqsQuickTaskPlan>(sp => sp.ItemId == planId);
            var libraryForms = plan.QuickTaskShift?.TaskList.SelectMany(t => t.LibraryForms?.Select(f => f.LibraryFormId)?.ToList() ?? new List<string>()).ToArray();
            if (libraryForms?.Any() != true) return;
            var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
            //await _cockpitDocumentActivityMetricsGenerationService.OnDeletingShiftPlanDeleteFormsSummary(libraryForms, activityName, plan);
        }

        private async Task UpdateProcessGuide(PraxisProcessGuide processGuide)
        {
            await _repository.UpdateAsync<PraxisProcessGuide>(pg => pg.ItemId == processGuide.ItemId, processGuide);
        }

        private async Task DeleteProcessGuide(string id)
        {
            await _repository.DeleteAsync<PraxisProcessGuide>(sp => sp.ItemId == id);
        }

        private string GetOrganisationId(string departmentId)
        {
            var organisation = _repository.GetItems<PraxisClient>(o => o.ItemId == departmentId).FirstOrDefault();
            if (organisation == null)
            {
                return string.Empty;
            }

            return organisation.ParentOrganizationId;
        }
    }
}