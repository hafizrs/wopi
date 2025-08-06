using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactFolderPermissionService : IObjectArtifactFolderPermissionService
    {
        private readonly ILogger<ObjectArtifactFolderPermissionService> _logger;
        private readonly IRepository _repository;
        private readonly IObjectArtifactPermissionHelperService _objectArtifactPermissionHelperService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IChangeLogService _changeLogService;

        public ObjectArtifactFolderPermissionService(
            ILogger<ObjectArtifactFolderPermissionService> logger,
            IRepository repository,
            IObjectArtifactPermissionHelperService objectArtifactPermissionHelperService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IChangeLogService changeLogService)
        {
            _logger = logger;
            _repository = repository;
            _objectArtifactPermissionHelperService = objectArtifactPermissionHelperService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _changeLogService = changeLogService;
        }

        public async Task<bool> SetObjectArtifactFolderPermissions(ObjectArtifact objectArtifact)
        {
            var response = false;

            if (objectArtifact != null)
            {
                var updates = PrepareObjectArtifactFolderPermissionModel(objectArtifact);
                if (updates != null)
                {
                    response = await UpdateObjectArtifact(objectArtifact.ItemId, updates);
                }
            }

            return response;
        }

        public Dictionary<string, object> PrepareObjectArtifactFolderPermissionModel(ObjectArtifact objectArtifact)
        {
            var organization = _objectArtifactUtilityService.GetOrganizationById(objectArtifact.OrganizationId);
            var departmentId = _objectArtifactUtilityService.GetObjectArtifactDepartmentId(objectArtifact.MetaData);
            var department = _objectArtifactUtilityService.GetDepartmentById(departmentId);

            var updates =
                (organization != null) ?
                PrepareUseCaseWiseFolderPermissionModel(objectArtifact, department) :
                null;

            return updates;
        }

        private Dictionary<string, object> PrepareUseCaseWiseFolderPermissionModel(ObjectArtifact objectArtifact, PraxisClient department)
        {
            Dictionary<string, object> updates = null;
            if (_objectArtifactUtilityService.IsASecretArtifact(objectArtifact?.MetaData))
            {
                updates = PrepareSecretFolderPermissions(objectArtifact, department);
            }
            else if (_objectArtifactPermissionHelperService.IsAAdminBUpload(objectArtifact.CreatedBy, objectArtifact.OrganizationId) || department == null)
            {
                updates = PrepareAdminBUploadedFolderPermissions(objectArtifact);
            }
            else if ((_objectArtifactPermissionHelperService.IsALibraryAdminUpload(objectArtifact, objectArtifact.CreatedBy) 
                && _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType)) || _objectArtifactUtilityService.IsAOrgLevelArtifact(objectArtifact.MetaData, objectArtifact.ArtifactType))
            {
                updates = PrepareLibraryAdminUploadedFolderPermissions(objectArtifact);
            }
            else if (department != null && _objectArtifactPermissionHelperService.IsAPowerUserUpload(objectArtifact.CreatedBy, department.ItemId))
            {
                updates = PreparePowerUserUploadedFolderPermissions(objectArtifact, department);
            }
            else if (department != null)
            {
                updates = PreparePowerUserUploadedFolderPermissions(objectArtifact, department);
            }

            return updates;
        }

        private Dictionary<string, object> PrepareAdminBUploadedFolderPermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", authorizedRoles
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareSecretFolderPermissions(ObjectArtifact artifact, PraxisClient department)
        {
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact, onlyDeptLevel: true);
            var emptyArray = new string[] { };
            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", new string[] {RoleNames.Admin}
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", emptyArray
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", emptyArray
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", new string[] {RoleNames.Admin}
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PrepareLibraryAdminUploadedFolderPermissions(ObjectArtifact artifact)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetOrganizationLevelObjectArtifactRoles(organizationId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete", authorizedRoles
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private Dictionary<string, object> PreparePowerUserUploadedFolderPermissions(ObjectArtifact artifact, PraxisClient department)
        {
            var organizationId = artifact.OrganizationId;
            var authorizedRoles = _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRoles(organizationId, department.ItemId);
            var authorizedIds = _objectArtifactPermissionHelperService.GetObjectArtifactAuthorizedIds(artifact);

            var updates = new Dictionary<string, object>
            {
                {
                    "RolesAllowedToRead", authorizedRoles
                },
                {
                    "IdsAllowedToRead", authorizedIds
                },
                {
                    "RolesAllowedToUpdate", authorizedRoles
                },
                {
                    "IdsAllowedToUpdate", authorizedIds
                },
                {
                    "RolesAllowedToWrite", authorizedRoles
                },
                {
                    "IdsAllowedToWrite", authorizedIds
                },
                {
                    "RolesAllowedToDelete",
                    _objectArtifactPermissionHelperService.GetDepartmentLevelObjectArtifactRemoverRoles(organizationId, department)
                },
                {
                    "IdsAllowedToDelete", authorizedIds
                }
            };

            return updates;
        }

        private async Task<bool> UpdateObjectArtifact(string objectArtifactId, Dictionary<string, object> updates)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", objectArtifactId);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), filter, updates);
        }
    }
}