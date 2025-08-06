
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class CreateLibraryGroupService : ICreateLibraryGroupService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;

        public CreateLibraryGroupService
        (
            IRepository repository,
            ISecurityContextProvider securityContextProvider
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
        }

        public async Task InitiateLibraryGroupCreationAsync(CreateLibraryGroupCommand command)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var mainGroup = await GetOrCreateGroupAsync(
                command.OrganizationId,
                null,
                Contracts.Constants.LibraryGroupType.MAIN_GROUP,
                command.GroupName,
                securityContext.UserId
            );

            if (string.IsNullOrWhiteSpace(command.SubGroupName))
            {
                return;
            }

            var subGroup = await GetOrCreateGroupAsync(
                command.OrganizationId,
                mainGroup.ItemId,
                Contracts.Constants.LibraryGroupType.SUB_GROUP,
                command.SubGroupName,
                securityContext.UserId
            );

            if (string.IsNullOrWhiteSpace(command.SubSubGroupName))
            {
                return;
            }

            await GetOrUpdateGroupAsync(
                command.OrganizationId,
                subGroup.ItemId,
                Contracts.Constants.LibraryGroupType.SUB_SUB_GROUP,
                command.SubSubGroupName,
                securityContext.UserId
            );
        }

        private async Task<RiqsLibraryGroup> GetOrCreateGroupAsync(string organizationId, string parentId, Contracts.Constants.LibraryGroupType groupType, string groupName, string userId)
        {
            var trimmedGroupName = groupName.Trim();
            var orgReadRole = $"{RoleNames.Organization_Read_Dynamic}_{organizationId}";

            var existingGroup = await _repository.GetItemAsync<RiqsLibraryGroup>(x =>
                x.OrganizationId == organizationId &&
                x.ParentId == parentId &&
                x.GroupType == groupType &&
                x.RolesAllowedToRead.Contains(orgReadRole) &&
                x.Name.ToLower() == trimmedGroupName.ToLower()
            );

            if (existingGroup != null)
            {
                return existingGroup;
            }

            var newGroup = BuildNewGroup(trimmedGroupName, organizationId, parentId, groupType, userId, orgReadRole);
            await _repository.SaveAsync(newGroup);

            return newGroup;
        }

        private async Task<RiqsLibraryGroup> GetOrUpdateGroupAsync(string organizationId, string parentId, Contracts.Constants.LibraryGroupType groupType, string groupName, string userId)
        {
            var trimmedGroupName = groupName.Trim();
            var orgReadRole = $"{RoleNames.Organization_Read_Dynamic}_{organizationId}";

            var existingGroup = await _repository.GetItemAsync<RiqsLibraryGroup>(x =>
                x.OrganizationId == organizationId &&
                x.ParentId == parentId &&
                x.GroupType == groupType &&
                x.RolesAllowedToRead.Contains(orgReadRole)
            );

            if (existingGroup == null)
            {
                var newGroup = BuildNewGroup(trimmedGroupName, organizationId, parentId, groupType, userId, orgReadRole);
                await _repository.SaveAsync(newGroup);
                return newGroup;
            }

            existingGroup.Name = trimmedGroupName;
            existingGroup.LastUpdateDate = DateTime.UtcNow;
            existingGroup.LastUpdatedBy = userId;

            await _repository.UpdateAsync(x => x.ItemId == existingGroup.ItemId, existingGroup);
            return existingGroup;
        }

        private RiqsLibraryGroup BuildNewGroup(string groupName, string organizationId, string parentId, Contracts.Constants.LibraryGroupType groupType, string userId, string orgReadRole)
        {
            return new RiqsLibraryGroup()
            {
                ItemId = Guid.NewGuid().ToString(),
                Name = groupName,
                OrganizationId = organizationId,
                GroupType = groupType,
                ParentId = parentId,
                CreateDate = DateTime.UtcNow,
                CreatedBy = userId,
                LastUpdateDate = DateTime.UtcNow,
                LastUpdatedBy = userId,
                RolesAllowedToRead = new string[] { orgReadRole }
            };
        }

    }
}
