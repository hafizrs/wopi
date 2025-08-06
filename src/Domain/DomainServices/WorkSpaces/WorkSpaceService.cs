using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.WorkSpaces;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.WorkSpaces
{
    public class WorkSpaceService : IWorkSpaceService
    {
        private readonly IRepository _repository;
        private readonly ILogger<WorkSpaceService> _logger;
        private readonly long _sorageSpace;

        public WorkSpaceService(
            IRepository repository,
            ILogger<WorkSpaceService> logger
        )
        {
            _repository = repository;
            _logger = logger;
            _sorageSpace = 10000;
        }


        public void CreateUserWorkSpace(CreateUserWorkspaceCommand createUserWrokspaceCommand)
        {
            var user = this._repository.GetItem<User>(x => x.ItemId == createUserWrokspaceCommand.OwnerId);
            if (user == null && (!IsValidProposedUser(createUserWrokspaceCommand.OwnerId)))
            {
                _logger.LogError("User/ProposedUser not found for owner id: {OwnerId}", createUserWrokspaceCommand.OwnerId);
                return;
            }

            var userDefaultWorkSpace = this._repository.GetItem<Workspace>(x =>
                x.OwnerId == createUserWrokspaceCommand.OwnerId && x.IsDefault);


            if (userDefaultWorkSpace == null)
            {
                CreateSharedWorkspace("My Workspace", createUserWrokspaceCommand.OwnerId, createUserWrokspaceCommand.UserId.ToString(),
                                        createUserWrokspaceCommand.TotalStorageSpace, false, true);
            }
        }

        private bool IsValidProposedUser(string proposedUserId)
        {
            var person = _repository.GetItem<Person>(item => item.ProposedUserId == proposedUserId);

            if (person == null) return false;

            return true;
        }

        private void CreateSharedWorkspace(string workspaceName, string ownerId, string createdByUserId, long totalStorageSpace, bool isShared, bool isDefault)
        {
            _logger.LogInformation("Creating user workspace for user {OwnerId}", ownerId);
            Workspace workspace = new Workspace();
            workspace.ItemId = Guid.NewGuid().ToString();
            workspace.Name = workspaceName;
            workspace.Description = workspaceName;
            workspace.OwnerId = ownerId;
            workspace.CreatedBy = createdByUserId.ToString();
            workspace.IdsAllowedToRead = new[] { workspace.OwnerId };
            workspace.IdsAllowedToWrite = new[] { workspace.OwnerId };
            workspace.IsShared = isShared;
            workspace.IsDefault = isDefault;
            workspace.TotalStorageSpace = totalStorageSpace;
            workspace.UsedStorageSpace = 0;
            workspace.StorageAreaId = DmsConstants.DefaultStorageAreaId;
            this._repository.Save(workspace);
            _logger.LogInformation("Workspace created successfully!");
        }
    }
}