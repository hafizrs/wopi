using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Events;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class AssignLibraryAdminsService : IAssignLibraryAdminsService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IServiceClient _serviceClient;
        private readonly ILogger<AssignLibraryAdminsService> _logger;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ISecurityHelperService _securityHelperService;

        public AssignLibraryAdminsService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IServiceClient serviceClient, ILogger<AssignLibraryAdminsService> logger,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ISecurityHelperService securityHelperService)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _logger = logger;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _securityHelperService = securityHelperService;
        }

        public async Task<bool> AssignLibraryRights(LibraryRightsAssignCommand command)
        {
            try
            {
                //get current organization
                var organization = await GetOrganizationById(command.OrganizationId);
                if (organization == null) return false;

                //get current setting
                var controlMechanism = await
                    GetRiqsLibraryControlMechanism(command.OrganizationId, organization.LibraryControlMechanism);
                if (controlMechanism == null) return false;

                //get approving data
                if (command.ApprovingBody != null)
                {
                    if (command.ApprovingBody.AddedIds != null)
                    {
                        AddNewUsers(command.ApprovingBody.AddedIds, controlMechanism.ApprovalAdmins);
                    }

                    if (command.ApprovingBody.RemovedIds != null)
                    {
                        RemoveExistingUsers(command.ApprovingBody.RemovedIds, controlMechanism.ApprovalAdmins);
                    }
                }

                //get uploading data
                if (command.UploadingBody != null)
                {
                    if (command.UploadingBody.AddedIds != null)
                    {
                        AddNewUsers(command.UploadingBody.AddedIds, controlMechanism.UploadAdmins);
                    }

                    if (command.UploadingBody.RemovedIds != null)
                    {
                        RemoveExistingUsers(command.UploadingBody.RemovedIds, controlMechanism.UploadAdmins);
                    }
                }

                //update mechanism
                await UpdateRiqsLibraryControlMechanism(controlMechanism);

                //published events
                PublishLibraryAdminAssignedEvent(controlMechanism.ItemId);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in InitiateAssignLibraryRights: {Message} -> {StackTrace}", e.Message,
                    e.StackTrace);
                return false;
            }
        }

        public async Task<bool> AssignLibraryRightsForDepartment(LibraryRightsAssignCommand command)
        {
            try
            {
                //get current setting
                var controlMechanism = await
                    GetRiqsLibraryControlMechanismByDeptId(command.DepartmentId);

                var isCreate = false;
                if (controlMechanism == null)
                {
                    isCreate = true;
                    controlMechanism = new RiqsLibraryControlMechanism()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        DepartmentId = command.DepartmentId,
                        ApprovalAdmins = new List<UserPraxisUserIdPair>(),
                        UploadAdmins = new List<UserPraxisUserIdPair>()
                    };
                }
                //get approving data
                if (command.ApprovingBody != null)
                {
                    if (command.ApprovingBody.AddedIds != null)
                    {
                        AddNewUsers(command.ApprovingBody.AddedIds, controlMechanism.ApprovalAdmins);
                    }

                    if (command.ApprovingBody.RemovedIds != null)
                    {
                        RemoveExistingUsers(command.ApprovingBody.RemovedIds, controlMechanism.ApprovalAdmins);
                    }
                }

                //get uploading data
                if (command.UploadingBody != null)
                {
                    if (command.UploadingBody.AddedIds != null)
                    {
                        AddNewUsers(command.UploadingBody.AddedIds, controlMechanism.UploadAdmins);
                    }

                    if (command.UploadingBody.RemovedIds != null)
                    {
                        RemoveExistingUsers(command.UploadingBody.RemovedIds, controlMechanism.UploadAdmins);
                    }
                }

                //update mechanism
                await CreateUpdateRiqsLibraryControlMechanism(controlMechanism, isCreate);

                //published events
                PublishLibraryAdminAssignedEvent(controlMechanism.ItemId);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in InitiateAssignLibraryRights: {Message} -> {StackTrace}", e.Message,
                    e.StackTrace);
                return false;
            }
        }

        public async Task<RiqsLibraryControlMechanism> GetLibraryRights(LibraryRightsGetQuery query)
        {
            //get current organization
            var organization = await GetOrganizationById(query.OrganizationId);

            if (organization == null || string.IsNullOrEmpty(organization.LibraryControlMechanism)) return null;

            //return current mechanism for the organization
            var rights = await GetRiqsLibraryControlMechanism(query.OrganizationId, organization.LibraryControlMechanism);
            
            return _securityHelperService.IsADepartmentLevelUser()
                ? GetDepartmentLibraryControlMechanism(rights) 
                : rights;
        }

        public async Task<RiqsLibraryControlMechanism> GetLibraryRightsForDepartment(LibraryRightsGetQuery query)
        {
            RiqsLibraryControlMechanism deptRights;

            if (!string.IsNullOrEmpty(query.DepartmentId))
            {
                deptRights = await GetRiqsLibraryControlMechanismByDeptId(query.DepartmentId);
                return deptRights;
            }

            return new RiqsLibraryControlMechanism() { DepartmentId = query.DepartmentId };
        }

        private RiqsLibraryControlMechanism GetDepartmentLibraryControlMechanism(RiqsLibraryControlMechanism rights)
        {
            var approvalAdmins = GetPraxisUsersByIds(rights.ApprovalAdmins.Select(a => a.PraxisUserId).ToArray());
            var uploadAdmins = GetPraxisUsersByIds(rights.UploadAdmins.Select(a => a.PraxisUserId).ToArray());
            var clientId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

            rights.ApprovalAdmins = GetFilteredUsers(approvalAdmins, clientId);
            rights.UploadAdmins = GetFilteredUsers(uploadAdmins, clientId);
            
            return rights;
        }

        private List<UserPraxisUserIdPair> GetFilteredUsers(List<PraxisUser> praxisUsers, string clientId)
        {
            return praxisUsers
                .Where(user => user.ClientList.Any(client => client.ClientId == clientId))
                .Select(user => new UserPraxisUserIdPair { PraxisUserId = user.ItemId, UserId = user.UserId })
                .ToList();
        }

        private void PublishLibraryAdminAssignedEvent(string itemId)
        {
            var libraryAdminAssignedEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryRightsUpdatedEvent,
                JsonPayload = JsonConvert.SerializeObject(itemId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryAdminAssignedEvent);
        }

        private async Task<PraxisOrganization> GetOrganizationById(string id)
        {
            return
                await _repository
                    .GetItemAsync<PraxisOrganization>(i => i.ItemId == id && !i.IsMarkedToDelete);
        }

        private IEnumerable<UserPraxisUserIdPair> GetPraxisUsers(string[] ids)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            return
                _repository
                    .GetItems<PraxisUser>(i =>
                        ids.Contains(i.ItemId) &&
                        !i.IsMarkedToDelete &&
                        (i.RolesAllowedToRead.Any(r => securityContext.Roles.Contains(r)) ||
                         i.IdsAllowedToRead.Contains(securityContext.UserId)))
                    .Select(pu => new UserPraxisUserIdPair
                    {
                        PraxisUserId = pu.ItemId,
                        UserId = pu.UserId,
                    })
                    .ToList();
        }

        private async Task<RiqsLibraryControlMechanism> GetRiqsLibraryControlMechanism(string organizationId, string
            controlMechanism)
        {
            return
                await _repository
                    .GetItemAsync<RiqsLibraryControlMechanism>(
                        i =>
                            i.OrganizationId == organizationId &&
                            i.ControlMechanismName == controlMechanism
                            && !i.IsMarkedToDelete
                    );
        }

        private async Task<RiqsLibraryControlMechanism> GetRiqsLibraryControlMechanismByDeptId(string deptId)
        {
            return
                await _repository
                    .GetItemAsync<RiqsLibraryControlMechanism>(
                        i =>
                            i.DepartmentId == deptId
                            && !i.IsMarkedToDelete
                    );
        }

        private async Task UpdateRiqsLibraryControlMechanism(RiqsLibraryControlMechanism data)
        {
            await _repository.UpdateAsync(d => d.ItemId == data.ItemId, data);

            LibraryControlMechanismConstant.ResetLibraryControlMechanism(data);
        }

        private async Task CreateUpdateRiqsLibraryControlMechanism(RiqsLibraryControlMechanism data, bool isCreate)
        {
            if (isCreate)
            {
                await _repository.SaveAsync(data);
            }
            await _repository.UpdateAsync(d => d.ItemId == data.ItemId, data);
        }

        private void AddNewUsers(IEnumerable<string> addedUsers, List<UserPraxisUserIdPair> existingUser)
        {
            var addedIds = addedUsers
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct().ToArray();

            //add newly added items only
            addedIds = addedIds
                .Where(uuid => existingUser.All(obj => obj.PraxisUserId != uuid))
                .ToArray();

            //get praxis user praxis id pair
            existingUser.AddRange(GetPraxisUsers(addedIds));
        }

        private static void RemoveExistingUsers(IEnumerable<string> removedUsers,
            List<UserPraxisUserIdPair> existingUser)
        {
            var removedAdminIds =
                removedUsers
                    .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct()
                    .ToArray();

            //delete removed ids from current data
            existingUser
                .RemoveAll(user => removedAdminIds.Contains(user.PraxisUserId));
        }
        private List<PraxisUser> GetPraxisUsersByIds(string[] ids)
        {
            return _repository.GetItems<PraxisUser>(pu => ids.Contains(pu.ItemId))?.
                    Select(pu => new PraxisUser()
                    {
                        ItemId = pu.ItemId,
                        UserId = pu.UserId,
                        ClientList = pu.ClientList ?? new List<PraxisClientInfo>()
                    }).ToList() ?? new List<PraxisUser>();
        }
    }
}
