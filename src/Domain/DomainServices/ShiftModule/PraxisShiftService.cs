using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisShiftService : IPraxisShiftService
    {
        private readonly ILogger<PraxisShiftService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IPraxisShiftPermissionService _praxisShiftPermissionService;
        private readonly IShiftTaskAssignService _shiftTaskAssignService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;

        public PraxisShiftService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IPraxisShiftPermissionService praxisShiftPermissionService,
            IShiftTaskAssignService shiftTaskAssignService,
            IGenericEventPublishService genericEventPublishService,
            ICockpitSummaryCommandService cockpitSummaryCommandService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            ILogger<PraxisShiftService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _praxisShiftPermissionService = praxisShiftPermissionService;
            _shiftTaskAssignService = shiftTaskAssignService;
            _genericEventPublishService = genericEventPublishService;
            _cockpitSummaryCommandService = cockpitSummaryCommandService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _logger = logger;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
        }

        public async Task CreateShift(CreateShiftCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var sequence = _repository.GetItems<RiqsShift>(s => s.DepartmentId == command.DepartmentId).Count();

            var newRiqsShift = new RiqsShift
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext.UserId,
                TenantId = securityContext.TenantId,
                Language = securityContext.Language,
                Tags = new[] { PraxisTag.IsValidRiqsShift },
                RolesAllowedToRead = _praxisShiftPermissionService.GetRolesAllowedToRead(command.DepartmentId),
                RolesAllowedToUpdate = _praxisShiftPermissionService.GetRolesAllowedToUpdate(command.DepartmentId),
                RolesAllowedToDelete = _praxisShiftPermissionService.GetRolesAllowedToDelete(command.DepartmentId),
                ShiftName = command.ShiftName,
                PraxisFormIds = command.PraxisFormIds,
                DepartmentId = command.DepartmentId,
                OrganizationId = GetOrganisationId(command.DepartmentId),
                Sequence = ++sequence,
                Files = command.Files,
                LibraryForms = command.LibraryForms
            };

            await _repository.SaveAsync(newRiqsShift);
            _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(newRiqsShift);
        }

        public List<RiqsShiftResponse> GetShifts(string departmentId)
        {
            var shifts = _repository.GetItems<RiqsShift>(s => s.DepartmentId == departmentId).OrderBy(s => s.Sequence)
                .ToList();

            var shiftResponseList = new List<RiqsShiftResponse>();

            foreach (var shift in shifts)
            {
                var shiftResponse = new RiqsShiftResponse(shift);
                shiftResponse.PraxisForms = GetPraxisFormsByIds(shift.PraxisFormIds);
                shiftResponseList.Add(shiftResponse);
            }

            return shiftResponseList;
        }

        public List<RiqsShift> GetShiftDropdown(string departmentId)
        {
            return _repository.GetItems<RiqsShift>(s => s.DepartmentId == departmentId).OrderBy(s => s.Sequence)
                .ToList();
        }

        public async Task CreateShiftPlan(CreateShiftPlanCommand command)
        {
            foreach (var shiftPlan in command.ShiftPlans)
            {
                var shiftDate = shiftPlan.Date;
                var shiftDateUtc = DateTime.SpecifyKind(shiftDate, DateTimeKind.Utc);
                var securityContext = _securityContextProvider.GetSecurityContext();
                var shift = GetShifByPraxisShiftId(shiftPlan.ShiftId) ?? shiftPlan.SingleShift;

                var existingShiftPlan = !string.IsNullOrEmpty(shift.ItemId) ?
                    _repository.GetItems<RiqsShiftPlan>(s => s.Shift.ItemId == shift.ItemId && s.ShiftDate == shiftDate).FirstOrDefault()
                    : null ;

                _logger.LogInformation("Has Existing Shift Plan: {Response}", existingShiftPlan != null);

                string shiftPlanId = existingShiftPlan?.ItemId ?? string.Empty;

                if (existingShiftPlan == null)
                {
                    var newRiqsShiftPlan = new RiqsShiftPlan
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = securityContext.UserId,
                        TenantId = securityContext.TenantId,
                        Language = securityContext.Language,
                        Tags = new[] { PraxisTag.IsValidRiqsShiftPlan },
                        RolesAllowedToRead = _praxisShiftPermissionService.GetRolesAllowedToRead(shift.DepartmentId),
                        RolesAllowedToUpdate = _praxisShiftPermissionService.GetRolesAllowedToUpdate(shift.DepartmentId),
                        RolesAllowedToDelete = _praxisShiftPermissionService.GetRolesAllowedToDelete(shift.DepartmentId),
                        Shift = shift,
                        ShiftDate = shiftDate,
                        PraxisUserIds = shiftPlan.PraxisUserIds,
                        IsProcessGuidCreated = false,
                        TimezoneOffsetInMinutes = shiftPlan.TimezoneOffsetInMinutes,
                        Color = shiftPlan.Color,
                        AttachedMaintenances = shiftPlan.MaintenanceAttachments,
                        Location = shiftPlan.Location,
                        DepartmentId = command.DepartmentId
                    };

                    await _repository.SaveAsync(newRiqsShiftPlan);

                    _logger.LogInformation("Created Shift Plan with ItemId: {ItemId}", newRiqsShiftPlan.ItemId);

                    shiftPlanId = newRiqsShiftPlan.ItemId;

                    if (shiftPlan.CloneToDates.Count > 0 && !string.IsNullOrEmpty(command.DepartmentId))
                    {
                        var cloneToDates = shiftPlan.CloneToDates.Select(cloneToDate => DateTime.SpecifyKind(cloneToDate, DateTimeKind.Utc)).ToList();
                        if (cloneToDates.Count > 0)
                        {
                            var dates = cloneToDates.Where(date => date.ToShortDateString() != newRiqsShiftPlan.ShiftDate.ToShortDateString()).ToList();
                            shiftPlan.CloneToDates = dates;
                        }
                    }

                    if (shiftPlan.AssignTask)
                    {
                        await _shiftTaskAssignService.AssignTasks(newRiqsShiftPlan);
                    }
                    var localDate = DateTime.UtcNow.AddMinutes(shiftPlan.TimezoneOffsetInMinutes);
                    _logger.LogInformation("Shift Date: {ShiftDate}, Local Date: {UtcDate}", shiftDateUtc, localDate);
                    if (shiftDateUtc.Date == localDate.Date)
                    {
                        await _cockpitSummaryCommandService.CreateSummary(newRiqsShiftPlan.ItemId, nameof(RiqsShiftPlan));
                        var files = shift.LibraryForms?
                            .Select(f => f.LibraryFormId)
                            .ToList() ?? new List<string>();
                        if (files.Any())
                        {
                            var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
                            await _cockpitDocumentActivityMetricsGenerationService
                                .OnDocumentUsedInShiftPlanGenerateActivityMetrics(files.ToArray(), activityName, newRiqsShiftPlan.ItemId);
                        }
                    }
                }

                if (shiftPlan.CloneToDates.Count > 0 && !string.IsNullOrEmpty(command.DepartmentId))
                {
                    var cloneShiftPlansCommand = new CloneShiftPlansCommand
                    {
                        CloneToDates = shiftPlan.CloneToDates,
                        ShiftPlanIds = new List<string> { shiftPlanId },
                        DepartmentId = command.DepartmentId
                    };

                    await CloneShiftPlans(cloneShiftPlansCommand);
                }

            }
        }

        public async Task UpdateShiftSequence(string[] shiftIds)
        {
            var sequence = 1;

            foreach (var shiftId in shiftIds)
            {
                var shift = await _repository.GetItemAsync<RiqsShift>(sft => sft.ItemId == shiftId);
                shift.Sequence = sequence;

                await _repository.UpdateAsync<RiqsShift>(sft => sft.ItemId == shift.ItemId, shift);
                await UpdateShiftSequenceInShiftPlan(shiftId, shift.Sequence);
                sequence++;
            }

            TempPraxisFormShiftPlanIdAssign();
        }

        private void TempPraxisFormShiftPlanIdAssign()
        {
            var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(-10), DateTimeKind.Utc);
            var shiftPlans = _repository.GetItems<RiqsShiftPlan>(
                sp => sp.IsProcessGuidCreated && sp.ProcessGuideId == null &&
                      sp.ShiftDate >= utcStartDate
            ).ToList();

            var formIds = shiftPlans.SelectMany(sp => sp.Shift.PraxisFormIds).ToHashSet();

            var praxisProcessGuides = _repository.GetItems<PraxisProcessGuide>(
                pg =>
                    formIds.Contains(pg.FormId) &&
                    pg.Shifts == null &&
                    pg.CreateDate >= utcStartDate
            ).ToList();


            foreach (var praxisProcessGuide in praxisProcessGuides)
            {
                var filteredShiftPlans = shiftPlans.Where(x => praxisProcessGuide.CreateDate.Date == x.ShiftDate.Date)
                    .ToList();

                var shifts = filteredShiftPlans.Where(
                        x => x.Shift.PraxisFormIds.Contains(praxisProcessGuide.FormId) &&
                             praxisProcessGuide.Clients.Any(y => y.ClientId == x.Shift.DepartmentId)
                    )
                    .Select(z =>
                        z.Shift
                    )
                    .ToList();
                var s = shifts.Select(x => new PraxisShift()
                {
                    ItemId = x.ItemId,
                    Name = x.ShiftName
                }).ToList();
                praxisProcessGuide.Shifts = s;
                _repository.Update<PraxisProcessGuide>(pg => pg.ItemId == praxisProcessGuide.ItemId,
                    praxisProcessGuide);

                var sids = s.Select(x => x.ItemId).ToList();
                var _shiftPlans = filteredShiftPlans.Where(x => sids.Contains(x.Shift.ItemId)).ToList();

                foreach (var _shiftPlan in _shiftPlans)
                {
                    _shiftPlan.ProcessGuideId = praxisProcessGuide.ItemId;
                    _repository.Update<RiqsShiftPlan>(sp => sp.ItemId == _shiftPlan.ItemId, _shiftPlan);
                }
            }
        }

        private async Task UpdateShiftSequenceInShiftPlan(string shiftId, int sequence)
        {
            var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
            var shiftPlans = _repository.GetItems<RiqsShiftPlan>(sp =>
                sp.Shift.ItemId == shiftId &&
                sp.IsProcessGuidCreated == false
            ).ToList();


            if (shiftPlans.Any())
            {
                foreach (var shiftPlan in shiftPlans)
                {
                    if (shiftPlan.ShiftDate.Date >= utcStartDate.Date)
                    {
                        shiftPlan.Shift.Sequence = sequence;
                        await _repository.UpdateAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlan.ItemId, shiftPlan);
                    }
                }
            }
        }

        public List<ShiftPlanQueryResponse> GetShiftPlans(GetShiftPlanQuery query)
        {
            var utcStartDate = DateTime.SpecifyKind(query.StartDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(query.EndDate, DateTimeKind.Utc);
            var shiftPlanGroupedByDate = _repository.GetItems<RiqsShiftPlan>(sp =>
                    sp.ShiftDate >= utcStartDate && sp.ShiftDate <= utcEndDate &&
                    sp.Shift.DepartmentId == query.DepartmentId)
                .GroupBy(shiftPlan => shiftPlan.ShiftDate)
                .Select(g => new ShiftPlanQueryResponse
                {
                    ShiftDate = g.Key,
                    ShiftPlans = g.Select(sp => new RiqsShiftPlanResponse
                    {
                        ItemId = sp.ItemId,
                        ShiftDate = sp.ShiftDate,
                        IsProcessGuidCreated = sp.IsProcessGuidCreated,
                        PraxisUserIds = sp.PraxisUserIds,
                        Shift = new RiqsShiftResponse
                        {
                            ItemId = sp.Shift.ItemId,
                            ShiftName = sp.Shift.ShiftName,
                            PraxisFormIds = sp.Shift.PraxisFormIds,
                            Sequence = sp.Shift.Sequence,
                            Files = sp.Shift.Files,
                            LibraryForms = sp.Shift.LibraryForms
                        },
                        Color = sp.Color
                    })
                    .OrderBy(sp => sp.Shift.Sequence)
                    .ToList()
                }).ToList();

            PopulatePraxisUser(shiftPlanGroupedByDate);

            return shiftPlanGroupedByDate;
        }

        public void SortShiftPlans(List<ShiftPlanQueryResponse> items)
        {
            foreach (var item in items)
            {
                item.ShiftPlans = item.ShiftPlans.OrderBy(sp => sp.Shift.Sequence).ToList();
            }
        }

        public async Task CloneShiftPlans(CloneShiftPlansCommand command)
        {
            if (command.CloneToDates != null && command.ShiftPlanIds != null)
            {
                //get the shift plans for the provided ids
                var shiftPlans = _repository
                    .GetItems<RiqsShiftPlan>(sp =>
                        command.ShiftPlanIds.Contains(sp.ItemId)
                        && sp.Shift.DepartmentId == command.DepartmentId
                        );

                var securityContext = _securityContextProvider.GetSecurityContext();
                var readRoles = _praxisShiftPermissionService.GetRolesAllowedToRead(command.DepartmentId);
                var deleteRoles = _praxisShiftPermissionService.GetRolesAllowedToDelete(command.DepartmentId);
                var updateRoles = _praxisShiftPermissionService.GetRolesAllowedToUpdate(command.DepartmentId);

                foreach (var shiftPlan in shiftPlans)
                {
                    foreach (var newShiftPlan in from cloneToDate in command.CloneToDates
                                                 select DateTime.SpecifyKind(cloneToDate, DateTimeKind.Utc)
                                            into utcShiftPlanDate
                                                 where !IsShiftPlanExist(utcShiftPlanDate, shiftPlan.Shift.ItemId)
                                                 let shift = shiftPlan.Shift
                                                 select new RiqsShiftPlan
                                                 {
                                                     ItemId = Guid.NewGuid().ToString(),
                                                     CreateDate = DateTime.UtcNow.ToLocalTime(),
                                                     LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                                                     CreatedBy = securityContext.UserId,
                                                     TenantId = securityContext.TenantId,
                                                     Language = securityContext.Language,
                                                     Tags = new[] { PraxisTag.IsValidRiqsShiftPlan },
                                                     RolesAllowedToRead = readRoles,
                                                     RolesAllowedToUpdate = updateRoles,
                                                     RolesAllowedToDelete = deleteRoles,
                                                     Shift = shift,
                                                     ShiftDate = utcShiftPlanDate,
                                                     PraxisUserIds = shiftPlan.PraxisUserIds,
                                                     IsProcessGuidCreated = false,
                                                     Color = shiftPlan.Color,
                                                     Location = shiftPlan.Location,
                                                     AttachedMaintenances = shiftPlan.AttachedMaintenances,
                                                     DepartmentId = shiftPlan.DepartmentId,
                                                     OrganizationId = shiftPlan.OrganizationId
                                                 })
                    {
                        await _repository.SaveAsync(newShiftPlan);
                    }
                }
            }
        }

        public async Task CloneShiftPlan(CloneShiftPlanCommand command)
        {
            if (command.CloneToDates != null)
            {
                var shift = GetShifByPraxisShiftId(command.ShiftId);
                if (shift == null)
                {
                    return;
                }

                var securityContext = _securityContextProvider.GetSecurityContext();
                var readRoles = _praxisShiftPermissionService.GetRolesAllowedToRead(shift.DepartmentId);
                var deleteRoles = _praxisShiftPermissionService.GetRolesAllowedToDelete(shift.DepartmentId);
                var updateRoles = _praxisShiftPermissionService.GetRolesAllowedToUpdate(shift.DepartmentId);

                foreach (var shiftPlan in command.CloneToDates
                    .Select(date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
                    .Where(exDate => !IsShiftPlanExist(exDate, shift.ItemId))
                    .Select(utcDate => new RiqsShiftPlan
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = securityContext.UserId,
                        TenantId = securityContext.TenantId,
                        Language = securityContext.Language,
                        Tags = new[] { PraxisTag.IsValidRiqsShiftPlan },
                        RolesAllowedToRead = readRoles,
                        RolesAllowedToUpdate = updateRoles,
                        RolesAllowedToDelete = deleteRoles,
                        Shift = shift,
                        ShiftDate = utcDate,
                        PraxisUserIds = command.PraxisUserIds,
                        IsProcessGuidCreated = false
                    }))
                {
                    await _repository.SaveAsync(shiftPlan);
                }
            }
        }

        public RiqsShiftPlanResponse GetShiftPlanById(string id)
        {
            var shiftPlan = _repository.GetItems<RiqsShiftPlan>(sp => sp.ItemId == id).FirstOrDefault();
            var shiftPlanResponse = new RiqsShiftPlanResponse(shiftPlan);
            shiftPlanResponse.PraxisPersons = GetPraxisUsers(shiftPlanResponse.PraxisUserIds ?? new List<string>());
            shiftPlanResponse.Shift.PraxisForms = GetPraxisFormsByIds(shiftPlanResponse.Shift.PraxisFormIds);
            if (shiftPlanResponse.IsProcessGuidCreated)
            {
                shiftPlanResponse.ProcessGuideId = shiftPlan.ProcessGuideId ?? GetProcessGuideIdByShiftPlan(shiftPlan);
            }

            var shiftCloned = shiftPlan?.Shift != null
                ? _repository.GetItems<RiqsShiftPlan>(sp =>
                      sp.Shift != null &&
                      sp.Shift.ItemId == shiftPlan.Shift.ItemId &&
                      sp.ShiftDate >= shiftPlan.ShiftDate
                  )?.ToList()
                : new List<RiqsShiftPlan>();

            if (shiftCloned?.Count > 0)
            {
                shiftPlanResponse.CloneShiftDates = shiftCloned.Select(clone => new CloneShiftDate
                {
                    ItemId = clone.ItemId,
                    ShiftDate = clone.ShiftDate,
                }).OrderBy(o => o.ShiftDate).ToList();

                shiftPlanResponse.ClonePraxisUserIds = shiftCloned
                  .Where(clone => clone.PraxisUserIds != null)
                  .SelectMany(clone => clone.PraxisUserIds.Select(userId => userId.ToString()))
                  .Distinct()
                  .ToList();
            }

            return shiftPlanResponse;
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
                        var isComplete = _objectArtifactUtilityService.IsACompletedFormResponse(metaData);


                        var originalFormId = _objectArtifactUtilityService.GetMetaDataValueByKey(metaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                                                $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"]);

                        if (entityName == EntityName.RiqsShiftPlan && !string.IsNullOrEmpty(entityId))
                        {
                            var shiftPlan = _repository.GetItem<RiqsShiftPlan>
                                            (p => p.ItemId == entityId && !p.IsMarkedToDelete);

                            if (shiftPlan != null)
                            {
                                var libraryFormResponse = shiftPlan?.Shift?.LibraryFormResponses?
                                                .Find(l => l.OriginalFormId == originalFormId && l.CompletedBy == praxisUserId);
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
                                    var responses = shiftPlan?.Shift?.LibraryFormResponses?.ToList() ?? new List<PraxisLibraryFormResponse>();
                                    responses.Add(libraryFormResponse);
                                    if (shiftPlan?.Shift != null) shiftPlan.Shift.LibraryFormResponses = responses;
                                }
                                await _repository.UpdateAsync(p => p.ItemId == shiftPlan.ItemId, shiftPlan);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in UpdateEquipmentMaintenanceLibraryFormResponse: {ex.Message}");
            }
        }

        private string GetProcessGuideIdByShiftPlan(RiqsShiftPlan shiftPlan)
        {
            var processGuideId = shiftPlan.ProcessGuideId;
            var shiftDateUtc = DateTime.SpecifyKind(shiftPlan.ShiftDate, DateTimeKind.Utc);
            var processGuides = _repository.GetItems<PraxisProcessGuide>(x =>
                x.Shifts.Any(y => y.ItemId == shiftPlan.Shift.ItemId) &&
                x.ClientId == shiftPlan.Shift.DepartmentId).ToList();


            if (processGuides.Any())
            {
                foreach (var processGuide in processGuides.Where(processGuide => processGuide.CreateDate.Date == shiftDateUtc.Date))
                {
                    processGuideId = processGuide.ItemId;
                }
            }

            return processGuideId;
        }

        public bool ValidateShiftInfo(ValidateShiftInfo query)
        {
            var exists = _repository.GetItems<RiqsShift>()
                .Any(s => s.ShiftName == query.ShiftName && s.DepartmentId == query.DepartmentId);
            return !exists;
        }

        public bool ValidateShiftPlanInfo(ValidateShiftPlanInfoQuery query)
        {
            var existingShiftPlans = _repository.GetItems<RiqsShiftPlan>(s => s.Shift.ItemId == query.ShiftId).ToList();

            if (existingShiftPlans.Count > 0)
            {
                var existingDates = existingShiftPlans.Select(sp => sp.ShiftDate.Date).ToList();

                return !existingDates.Any(date => query.Dates.Contains(date));
            }

            return true;
        }

        public async Task UpdateShiftPlan(UpdateShiftPlanCommand command)
        {
            if (command?.ShiftPlanIds != null && command.ShiftPlanIds.Count() > 0)
            {
                var updateTasks = command.ShiftPlanIds.Select(itemId =>
                {
                    var updates = new Dictionary<string, object>
                        {
                            {"PraxisUserIds", command.PraxisUserIds}
                        };

                    return _repository.UpdateAsync<RiqsShiftPlan>(sp => sp.ItemId == itemId, updates);
                });

                await Task.WhenAll(updateTasks);
            }
        }

        public async Task DeleteShiftPlan(List<string> shiftPlansIds)
        {
            if (shiftPlansIds != null && shiftPlansIds.Count > 0)
            {
                var deleteTasks = shiftPlansIds.Select(async id =>
                {
                    await RemoveShiftProcessGuid(id);
                    await RemoveShiftLibraryForms(id);
                    await _repository.DeleteAsync<RiqsShiftPlan>(sp => sp.ItemId == id);
                });

                await Task.WhenAll(deleteTasks);

                await _cockpitSummaryCommandService.DeleteSummaryAsync(shiftPlansIds, CockpitTypeNameEnum.RiqsShiftPlan);
            }
        }

        public async Task DeleteShift(string id)
        {
            var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
            var shiftPlans = _repository.GetItems<RiqsShiftPlan>(sp =>
                sp.Shift.ItemId == id &&
                sp.IsProcessGuidCreated == false
            ).ToList();
            if (shiftPlans.Any())
            {
                foreach (var shiftPlan in shiftPlans)
                {
                    if (shiftPlan.ShiftDate.Date >= utcStartDate.Date)
                    {
                        await _repository.DeleteAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlan.ItemId);
                    }
                }
            }

            var shift = _repository.GetItem<RiqsShift>(s => s.ItemId == id);
            await _repository.DeleteAsync<RiqsShift>(s => s.ItemId == id);

            if (shift != null)
            {
                _genericEventPublishService.PublishDmsArtifactUsageReferenceDeleteEvent(shift);
            }
        }

        public async Task EditShift(EditShiftCommand command)
        {
            var shift = _repository.GetItem<RiqsShift>(s => s.ItemId == command.ItemId);

            if (shift != null)
            {
                var utcStartDate = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
                shift.ShiftName = command.ShiftName;
                shift.PraxisFormIds = command.PraxisFormIds;
                shift.Files = command.Files;
                shift.LibraryForms = command.LibraryForms;
                await _repository.UpdateAsync<RiqsShift>(s => s.ItemId == command.ItemId, shift);

                var shiftPlans = _repository.GetItems<RiqsShiftPlan>(sp =>
                    sp.Shift.ItemId == command.ItemId &&
                    sp.IsProcessGuidCreated == false
                ).ToList();

                if (shiftPlans.Any())
                {
                    foreach (var shiftPlan in shiftPlans)
                    {
                        if (shiftPlan.ShiftDate.Date >= utcStartDate.Date)
                        {
                            shiftPlan.Shift = shift;
                            await _repository.UpdateAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlan.ItemId,
                                shiftPlan);
                        }
                    }
                }
                _genericEventPublishService.PublishDmsArtifactUsageReferenceEvent(shift);
            }
        }

        private async Task RemoveShiftProcessGuid(string shiftPlanId)
        {
            var shiftPlan = await _repository.GetItemAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlanId);
            if (shiftPlan.IsProcessGuidCreated)
            {
                foreach (var formId in shiftPlan.Shift.PraxisFormIds)
                {
                    DateTime endDate = DateTime.UtcNow;
                    DateTime startDate = endDate.Date;
                    var processGuide = _repository
                        .GetItems<PraxisProcessGuide>(pg =>
                            !pg.IsMarkedToDelete &&
                            pg.FormId == formId &&
                            pg.CreateDate >= startDate &&
                            pg.CreateDate <= endDate &&
                            pg.ClientId == shiftPlan.Shift.DepartmentId &&
                            pg.Shifts != null)
                        .FirstOrDefault();

                    if (processGuide != null)
                    {
                        var controllMembers = (List<string>)processGuide.ControlledMembers;
                        controllMembers.Remove(shiftPlan.PraxisUserIds?.FirstOrDefault());

                        var clientList = (List<ProcessGuideClientInfo>)processGuide.Clients;
                        if (clientList != null)
                        {
                            clientList[0].ControlledMembers = controllMembers;
                        }

                        processGuide.Clients = clientList;
                        processGuide.ControlledMembers = controllMembers;
                        var shifts = (List<PraxisShift>)processGuide.Shifts;
                        var shiftIndex = shifts?.FindIndex(x => x.ItemId == shiftPlan.Shift.ItemId);
                        if (shiftIndex != null && shiftIndex != -1)
                        {
                            shifts.RemoveAt(shiftIndex.Value);
                        }

                        processGuide.Shifts = shifts;
                        if (controllMembers.Count > 0)
                        {
                            await UpdateProcessGuide(processGuide);
                        }
                        else
                        {
                            await DeleteProcessGuide(processGuide.ItemId);
                            await _cockpitFormDocumentActivityMetricsGenerationService
                                .OnDeleteTaskRemoveSummaryFromActivityMetrics(new List<string> { processGuide.ItemId }, nameof(PraxisProcessGuide));
                        }
                        await _cockpitSummaryCommandService.DeleteSummaryAsync(new List<string> { processGuide.ItemId }, CockpitTypeNameEnum.PraxisProcessGuide);
                    }
                }
            }
        }

        private async Task RemoveShiftLibraryForms(string shiftPlanId)
        {
            var shiftPlan = await _repository.GetItemAsync<RiqsShiftPlan>(sp => sp.ItemId == shiftPlanId);
            var libraryForms = shiftPlan.Shift.LibraryForms?.Select(f => f.LibraryFormId).ToArray();
            if (libraryForms?.Any() != true) return;
            var activityName = $"{CockpitDocumentActivityEnum.PENDING_FORMS_TO_SIGN}";
            await _cockpitDocumentActivityMetricsGenerationService.OnDeletingShiftPlanDeleteFormsSummary(libraryForms, activityName, shiftPlan);
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

        private RiqsShift GetShifByPraxisShiftId(string shiftId)
        {
            if (string.IsNullOrEmpty(shiftId))
            {
                return null;
            }
            return _repository.GetItems<RiqsShift>(s => s.ItemId == shiftId).FirstOrDefault();
        }

        private void PopulatePraxisUser(List<ShiftPlanQueryResponse> shiftPlanResponses)
        {
            foreach (var shiftPlanResponse in shiftPlanResponses)
            {
                foreach (var shiftPlan in shiftPlanResponse.ShiftPlans)
                {
                    shiftPlan.PraxisPersons = GetPraxisUsers(shiftPlan.PraxisUserIds ?? new List<string>());
                }
            }
        }

        private List<PraxisUser> GetPraxisUsers(List<string> praxisUserIds)
        {
            return _repository.GetItems<PraxisUser>(pu => praxisUserIds.Contains(pu.ItemId))
                .ToList();
        }

        private List<PraxisFormResponse> GetPraxisFormsByIds(List<string> ids)
        {
            var forms = new List<PraxisFormResponse>();

            foreach (var id in ids)
            {
                var form = _repository.GetItems<PraxisForm>(p => p.ItemId == id)
                    .Select(p => new PraxisFormResponse
                    {
                        Id = p.ItemId,
                        Name = p.Description
                    }).FirstOrDefault();
                forms.Add(form);
            }

            return forms;
        }

        private bool IsShiftPlanExist(DateTime utcDate, string shiftId)
        {
            if (string.IsNullOrEmpty(shiftId)) return false;

            var existingShiftPlan = _repository
                .GetItems<RiqsShiftPlan>(s => s.Shift.ItemId == shiftId && s.ShiftDate == utcDate)
                .FirstOrDefault();
            return existingShiftPlan != null;
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            var deleteTasks = new List<Task>
            {
                _repository.DeleteAsync<RiqsShift>(Shift => Shift.DepartmentId.Equals(clientId)),
                _repository.DeleteAsync<RiqsShiftPlan>(Shift => Shift.Shift.DepartmentId.Equals(clientId))
            };
            await Task.WhenAll(deleteTasks);
        }
    }
}
