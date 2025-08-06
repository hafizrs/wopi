using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class AssignEquipmentAdminsService : IAssignEquipmentAdminsService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<AssignEquipmentAdminsService> _logger;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ICirsReportUpdateService _cirsReportUpdateService;
        public AssignEquipmentAdminsService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<AssignEquipmentAdminsService> logger,
            ISecurityHelperService securityHelperService,
            ICirsReportUpdateService cirsReportUpdateService
            )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
            _securityHelperService = securityHelperService;
            _cirsReportUpdateService = cirsReportUpdateService;
        }


        private async Task<bool> AssignEquipmentAdmins(
            AssignEquipmentAdminsCommand command,
            bool IsOrganizationLevelRight,
            string organizationId,
            Func<string, Task<PraxisEquipmentRight>> getPraxisEquipmentRight,
            Func<PraxisEquipmentRight, Task> savePraxisEquipmentRight,
            Func<PraxisEquipmentRight, Task> updatePraxisEquipmentRight
        )
        {
            var praxisEquipmentRight = await getPraxisEquipmentRight(command.DepartmentId);
            var existingAdmins = praxisEquipmentRight?.AssignedAdmins ?? new List<UserPraxisUserIdPair>();

            if (command.AddedAdminIds?.Count > 0)
            {
                AddNewAdmins(command.AddedAdminIds, existingAdmins);
            }

            if (command.RemovedAdminIds?.Count > 0)
            {
                RemoveExistingAdmins(command.RemovedAdminIds, existingAdmins);
            }

            if (praxisEquipmentRight == null)
            {
                praxisEquipmentRight = new PraxisEquipmentRight()
                {
                    ItemId = Guid.NewGuid().ToString(),
                    CreateDate = DateTime.UtcNow,
                    DepartmentId = command?.DepartmentId,
                    OrganizationId = organizationId,
                    AssignedAdmins = existingAdmins,
                    EquipmentId = command.EquipmentId,
                    IsOrganizationLevelRight = IsOrganizationLevelRight
                };

                await savePraxisEquipmentRight(praxisEquipmentRight);
            }
            else
            {
                praxisEquipmentRight.AssignedAdmins = existingAdmins;
                praxisEquipmentRight.LastUpdateDate = DateTime.UtcNow;
                await updatePraxisEquipmentRight(praxisEquipmentRight);
            }

            if (!string.IsNullOrEmpty(command.EquipmentId))
            {
                await UpdateRightsToEquipmentTable(praxisEquipmentRight);
                await _cirsReportUpdateService.UpdateFaultPermissions(command.EquipmentId, praxisEquipmentRight);
            }

            return true;
        }

        private async Task UpdateRightsToEquipmentTable(PraxisEquipmentRight right)
        {
            if (!string.IsNullOrEmpty(right?.EquipmentId))
            {
                var equipment = await _repository.GetItemAsync<PraxisEquipment>(e => !e.IsMarkedToDelete && e.ItemId == right.EquipmentId);
                if (equipment != null)
                {
                    var metaValues = equipment.MetaValues?.Where(m => m.Key != "AssignedRights").ToList() ?? new List<PraxisKeyValue>();
                    var adminIds = right.AssignedAdmins?.Select(a => a.PraxisUserId)?.ToList() ?? new List<string>();
                    var praxisUsers = GetPraxisUserByIds(adminIds);
                    var assignedRights = adminIds.Select(id => new
                    {
                        PraxisUserId = id,
                        Name = praxisUsers?.FirstOrDefault(pu => pu.ItemId == id)?.DisplayName
                    }).ToList();

                    if (assignedRights?.Count > 0)
                    {
                        metaValues.Add(new PraxisKeyValue()
                        {
                            Key = "AssignedRights",
                            Value = JsonConvert.SerializeObject(assignedRights)
                        });
                    }
                    equipment.MetaValues = metaValues;
                    await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
                }
            }
        }

        private async Task<bool> AssignEquipmentAdminsEquipementLevel(AssignEquipmentAdminsCommand command, string organizationId)
        {
            return await AssignEquipmentAdmins(
                command,
                false,
                organizationId,
                (departmentId) => GetPraxisEquipmentRights(departmentId, command.EquipmentId),
                (newPraxisEquipmentRight) => _repository.SaveAsync(newPraxisEquipmentRight),
                (updatedPraxisEquipmentRight) => _repository.UpdateAsync(i => i.ItemId.Equals(updatedPraxisEquipmentRight.ItemId), updatedPraxisEquipmentRight)
            );
        }

        private async Task<bool> AssignEquipmentAdminsOrgLevel(AssignEquipmentAdminsCommand command, string organizationId)
        {
            return await AssignEquipmentAdmins(
                command,
                true,
                organizationId,
                (departmentId) => GetPraxisEquipmentRight(organizationId),
                (newPraxisEquipmentRight) => _repository.SaveAsync(newPraxisEquipmentRight),
                (updatedPraxisEquipmentRight) => _repository.UpdateAsync(i => i.ItemId.Equals(updatedPraxisEquipmentRight.ItemId), updatedPraxisEquipmentRight)
            );
        }


        public async Task<bool> AssignEquipmentAdmins(AssignEquipmentAdminsCommand command)
        {
            try
            {
                var department = await GetDepartmentById(command.DepartmentId);
                if (department == null)
                {
                    return false;
                }
            
                var organizationId = department.ParentOrganizationId;
                if (command.IsOrganizationLevelRight && string.IsNullOrEmpty(command.EquipmentId))
                {
                    return await AssignEquipmentAdminsOrgLevel(command, organizationId);
                }else
                {
                    return await AssignEquipmentAdminsEquipementLevel(command, organizationId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in initiate AssignEquipmentRights: {Message} -> {StackTrace}", ex.Message,
                    ex.StackTrace);
                return false;
            }
        }

      
        public async Task<PraxisEquipmentRight> GetEquipmentRights(GetEquipmentRightsQuery query)
        {

            if(string.IsNullOrEmpty(query.EquipmentId) && query.IsOrganizationLevelRight)
            {
                string parentOrganizationId = string.Empty;
                if (!string.IsNullOrEmpty(query.DepartmentId))
                {
                    var department = await GetDepartmentById(query.DepartmentId);
                    parentOrganizationId = department.ParentOrganizationId;
                } else
                {
                    parentOrganizationId = _securityHelperService.ExtractOrganizationFromOrgLevelUser();
                }

                return await GetPraxisEquipmentRight(parentOrganizationId);
            }
            return await GetPraxisEquipmentRights(query.DepartmentId, query.EquipmentId);
        }

        private IEnumerable<UserPraxisUserIdPair> GetPraxisUserSubset(List<string> itemIds)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            
            return _repository
                .GetItems<PraxisUser>(user =>
                    itemIds.Contains(user.ItemId) &&
                    !user.IsMarkedToDelete &&
                    (user.RolesAllowedToRead.Any(role => securityContext.Roles.Contains(role)) ||
                    user.IdsAllowedToRead.Contains(securityContext.UserId)))
                .Select(pc => new UserPraxisUserIdPair
                {
                    PraxisUserId = pc.ItemId,
                    UserId = pc.UserId,
                })
                .ToList();
        }

        private async Task<PraxisClient> GetDepartmentById(string clientId)
        {
            return await _repository.GetItemAsync<PraxisClient>(
                pc => pc.ItemId.Equals(clientId) && !pc.IsMarkedToDelete);
        }

        private List<PraxisUser> GetPraxisUserByIds(List<string> itemIds)
        {
            return _repository.GetItems<PraxisUser>(pu => !pu.IsMarkedToDelete && itemIds.Contains(pu.ItemId))?.ToList() ?? new List<PraxisUser>();
        }

        private async Task<PraxisEquipmentRight> GetPraxisEquipmentRights(string departmentId, string equipmentId)
        {
            return await _repository.GetItemAsync<PraxisEquipmentRight>(
                per => per.DepartmentId.Equals(departmentId) && per.EquipmentId.Equals(equipmentId) &&
                       !per.IsMarkedToDelete);
        }
        private async Task<PraxisEquipmentRight> GetPraxisEquipmentRight(string organizationId)
        {
            return await _repository.GetItemAsync<PraxisEquipmentRight>(
                per => per.OrganizationId.Equals(organizationId) &&
                per.IsOrganizationLevelRight &&
                       !per.IsMarkedToDelete);
        }

        private void AddNewAdmins(IEnumerable<string> addedAdminIds, List<UserPraxisUserIdPair>existingAdminIds)
        {
            var idsToBeAdded = addedAdminIds?
                .Distinct()
                .Where(uuid => !string.IsNullOrWhiteSpace(uuid) && existingAdminIds.All(id => id.PraxisUserId != uuid))
                .ToList() ??
                new List<string> ();
            
            existingAdminIds?
                .AddRange(GetPraxisUserSubset(idsToBeAdded));
        }

        private void RemoveExistingAdmins(IEnumerable<string> removedAdminIds, List<UserPraxisUserIdPair> existingAdminIds)
        {
            var idsToBeRemoved = removedAdminIds?
                .Distinct()
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList() ??
                new List<string> ();
            
            existingAdminIds?
                .RemoveAll(id => idsToBeRemoved.Contains(id.PraxisUserId));
        }

    }
}
