using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.DataFixServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Linq;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Licensing;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Microsoft.Extensions.Configuration;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DataFixServices
{
    public class DmsDataCorrectionService : IResolveProdDataIssuesService
    {
        private readonly ILogger<DmsDataCorrectionService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IOrganizationDataProcessService _organizationDataProcessService;
        private readonly IChangeLogService _changeLogService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;

        private readonly string _systemOrganizationId;


        public DmsDataCorrectionService(
            ILogger<DmsDataCorrectionService> logger,
            IConfiguration configuration,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IOrganizationDataProcessService organizationDataProcessService,
            IChangeLogService changeLogService,
            IObjectArtifactUtilityService objectArtifactUtilityService)
        {
            _logger = logger;
            _systemOrganizationId = configuration["SystemOrganizationId"];
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _organizationDataProcessService = organizationDataProcessService;
            _changeLogService = changeLogService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
        }

        public async Task<bool> InitiateFix(ResolveProdDataIssuesCommand command)
        {
            _logger.LogInformation("Entered service: {ServiceName}", nameof(DmsDataCorrectionService));

            var response = await FixDmsData();

            if (response)
            {
                _logger.LogInformation("Successfully corrected dms related data");
                _logger.LogInformation("Exiting service: {ServiceName}", nameof(DmsDataCorrectionService));
            }
            else
            {
                _logger.LogError("Error occured during dms related data correction");
                _logger.LogInformation("Exiting service: {ServiceName}", nameof(DmsDataCorrectionService));
            }

            return response;
        }

        private async Task<bool> FixDmsData()
        {

            var list = await Task.Run(GetAllObjectArtifacts);
            foreach (var objectArtifact in list)
            {
                await _objectArtifactUtilityService.SetObjectArtifactExtension(objectArtifact);
            }

            return true;
        }

        private async Task<bool> FixArtifactData(List<ObjectArtifact> objectArtifacts, List<PraxisClient> departments)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            objectArtifacts.ForEach(artifact =>
            {
                if (!string.IsNullOrEmpty(artifact.OrganizationId))
                {
                    var organizationId = departments.FirstOrDefault(d => d.ItemId == artifact.OrganizationId)?.ParentOrganizationId;
                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        listOfTasks.Add(UpdateObjectArtifact(artifact, organizationId));
                        if (GetSharedObjectArtifact(artifact.ItemId) != null)
                        {
                            listOfTasks.Add(CreateSharedObjectArtifact(artifact, organizationId));
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"OrganizationId not found for department: {artifact.OrganizationId}.");
                    }
                }
            });

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private List<PraxisClient> GetAllDepartments()
        {
            return _repository.GetItems<PraxisClient>(o => !o.IsMarkedToDelete)
                .Select(c => new PraxisClient
                {
                    ItemId = c.ItemId,
                    ParentOrganizationId = c.ParentOrganizationId
                })
                .ToList();
        }

        private List<ObjectArtifact> GetAllObjectArtifacts()
        {
            return _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete && o.ArtifactType == ArtifactTypeEnum.File)
                .Select(o => new ObjectArtifact
                {
                    ItemId = o.ItemId,
                    MetaData = o.MetaData,
                    Name = o.Name,
                })
                .ToList();
        }

        private List<ObjectArtifact> GetAllObjectArtifactMetaData()
        {
            return _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete)
                .Select(o => new ObjectArtifact
                {
                    ItemId = o.ItemId,
                    MetaData = o.MetaData
                })
                .ToList();
        }

        private SharedObjectArtifact GetSharedObjectArtifact(string objectArtifatId)
        {
            return _repository.GetItem<SharedObjectArtifact>(
                soa => !soa.IsMarkedToDelete && soa.OriginalArtifactId == objectArtifatId);
        }

        private async Task<bool> UpdateObjectArtifact(ObjectArtifact objectArtifact, string organizationId)
        {
            var updates = PrepareDepartmentIdMetaDataMissingObjectArtifactUpdates(objectArtifact, organizationId, objectArtifact.OrganizationId);
            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", objectArtifact.ItemId);

            _logger.LogInformation("Going to update ObjectArtifact with id -> {ItemId},  Updates: {UpdatedData}.",objectArtifact.ItemId,
                JsonConvert.SerializeObject(updates, Formatting.Indented));

            var response = await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
            if (!response)
            {
                _logger.LogInformation("ObjectArtifact update wasn't successful with id -> {ItemId},", objectArtifact.ItemId);
            }
            return response;
        }

        private Dictionary<string, object> PrepareObjectArtifactUpdates(ObjectArtifact objectArtifact, string organizationId, string departmentId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var adminBrole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";

            var tags = objectArtifact.Tags.ToList();
            tags.Add(adminBrole);

            var rolesAllowedToread = objectArtifact.RolesAllowedToRead.ToList();
            rolesAllowedToread.Add(adminBrole);

            var sharedOrganizationList = objectArtifact.SharedOrganizationList;
            sharedOrganizationList.Add(
                new SharedOrganizationInfo
                {
                    OrganizationId = organizationId,
                    SharedPersonList = new List<string> { },
                    Tags = new string[] { RoleNames.AdminB }
                });

            var sharedRoleList = objectArtifact.SharedRoleList;
            sharedRoleList.Add(adminBrole);

            objectArtifact.MetaData = objectArtifact.MetaData == null ? new Dictionary<string, MetaValuePair>() { } : objectArtifact.MetaData;
            var departmentIdMetaDataValue = new MetaValuePair { Type = "string", Value = departmentId };
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];

            if (objectArtifact.MetaData.TryGetValue(departmentIdKey, out MetaValuePair value))
            {
                objectArtifact.MetaData[departmentIdKey] = departmentIdMetaDataValue;
            }
            else
            {
                objectArtifact.MetaData.Add(departmentIdKey, departmentIdMetaDataValue);
            }

            var updates = new Dictionary<string, object>
            {
                { "LastUpdateDate",  DateTime.UtcNow.ToLocalTime() },
                { "LastUpdatedBy", securityContext.UserId },
                { "Tags", tags.ToArray()},
                { "RolesAllowedToRead", rolesAllowedToread.ToArray() },
                { "OrganizationId", organizationId },
                { "SharedOrganizationList", sharedOrganizationList },
                { "SharedRoleList", sharedRoleList },
                { "MetaData", objectArtifact.MetaData }
            };

            return updates;
        }

        private async Task<bool> CreateSharedObjectArtifact(ObjectArtifact objectArtifact, string organizationId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var adminBrole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";

            var sharedObjectArtifact = new SharedObjectArtifact()
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext.UserId,
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdatedBy = securityContext.UserId,
                OriginalArtifactId = objectArtifact.ItemId,
                OrganizationId = organizationId,
                SharingTime = DateTime.UtcNow.ToLocalTime(),
                SharedByUserId = securityContext.UserId,
                SharedByUserName = securityContext.UserName,
                ParentId = objectArtifact.ParentId,
                OwnerId = objectArtifact.OwnerId,
                OwnerName = objectArtifact.OwnerName,
                ArtifactType = objectArtifact.ArtifactType,
                Extension = objectArtifact.Extension,
                Name = objectArtifact.Name,
                Description = objectArtifact.Description,
                FileStorageId = objectArtifact.FileStorageId,
                RolesAllowedToRead = new string[] { adminBrole },
                Color = objectArtifact.Color,
                Secured = objectArtifact.Secured,
                IsTopSharedItem = true
            };

            await _repository.SaveAsync(sharedObjectArtifact);

            return true;
        }

        private PraxisClientSubscription GetLatestSubscriptionData(string organizationId)
        {
            return _repository.GetItem<PraxisClientSubscription>(p => !p.IsMarkedToDelete && p.OrganizationId == organizationId && p.IsActive && p.IsLatest);
        }

        private async Task<bool> FixArtifactMetaData(List<ObjectArtifact> objectArtifactCommand)
        {
            var objectArtifacts = GetAllObjectArtifactMetaData();

            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            objectArtifactCommand.ForEach(artifactCommand =>
            {
                if (artifactCommand.MetaData != null)
                {
                    var objectArtifact = objectArtifacts.Find(o => o.ItemId == artifactCommand.ItemId);
                    if (objectArtifact != null)
                    {
                        listOfTasks.Add(UpdateObjectArtifactMetaData(objectArtifact, artifactCommand.MetaData));
                    }
                }
            });

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private async Task<bool> UpdateObjectArtifactMetaData(ObjectArtifact objectArtifact, IDictionary<string, MetaValuePair> metaData)
        {
            var updates = PrepareObjectArtifactMetaDataUpdates(objectArtifact, metaData);
            var builder = Builders<BsonDocument>.Filter;
            var updateFilters = builder.Eq("_id", objectArtifact.ItemId);

            return await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
        }

        private Dictionary<string, object> PrepareObjectArtifactMetaDataUpdates(ObjectArtifact objectArtifact, IDictionary<string, MetaValuePair> metaDataCommand)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var dictionaries = new List<IDictionary<string, MetaValuePair>>
            {
                metaDataCommand
            };

            if (objectArtifact.MetaData != null)
            {
                dictionaries.Add(objectArtifact.MetaData);
            }

            var metaData = dictionaries.SelectMany(dict => dict)
                         .ToDictionary(pair => pair.Key, pair => pair.Value);

            var updates = new Dictionary<string, object>
            {
                { "LastUpdateDate",  DateTime.UtcNow.ToLocalTime() },
                { "LastUpdatedBy", securityContext.UserId },
                { "MetaData", metaData }
            };

            return updates;
        }

        private List<ObjectArtifact> GetAllObjectArtifactsMissingDepartmentIdMetaData()
        {
            var objectArtifacts = _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete)
            .Select(o => new ObjectArtifact
            {
                ItemId = o.ItemId,
                OrganizationId = o.OrganizationId,
                Tags = o.Tags != null ? o.Tags : new string[] { },
                RolesAllowedToRead = o.RolesAllowedToRead != null ? o.RolesAllowedToRead : new string[] { },
                SharedOrganizationList = o.SharedOrganizationList != null ? o.SharedOrganizationList : new List<SharedOrganizationInfo> { },
                SharedRoleList = o.SharedRoleList != null ? o.SharedRoleList : new List<string> { },
                MetaData = o.MetaData
            })
            .ToList();

            return objectArtifacts.Where(o => !IsMetaDataDepartmentIdExists(o.MetaData)).ToList();
        }

        private bool IsMetaDataDepartmentIdExists(IDictionary<string, MetaValuePair> metaData)
        {
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];
            var departmentIdExits = metaData != null && metaData.TryGetValue(departmentIdKey, out MetaValuePair departmentIdValue);
            return departmentIdExits;
        }

        private async Task<bool> FixMissingDepartmentIdMetaDataObjectArtifactData(List<ObjectArtifact> objectArtifacts, List<PraxisClient> departments)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            objectArtifacts.ForEach(artifact =>
            {
                if (!string.IsNullOrEmpty(artifact.OrganizationId))
                {
                    var organizationId = departments.FirstOrDefault(d => d.ItemId == artifact.OrganizationId)?.ParentOrganizationId;
                    if (!string.IsNullOrEmpty(organizationId))
                    {
                        listOfTasks.Add(UpdateObjectArtifact(artifact, organizationId));
                    }
                    else
                    {
                        _logger.LogInformation($"OrganizationId not found for department: {artifact.OrganizationId}.");
                    }
                }
            });

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private Dictionary<string, object> PrepareDepartmentIdMetaDataMissingObjectArtifactUpdates(
            ObjectArtifact objectArtifact, string organizationId, string departmentId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var adminBrole = $"{RoleNames.AdminB_Dynamic}_{organizationId}";

            var tags = objectArtifact.Tags.ToList();
            if (!tags.Contains(adminBrole))
            {
                tags.Add(adminBrole);
            }

            var rolesAllowedToread = objectArtifact.RolesAllowedToRead.ToList();
            if (!rolesAllowedToread.Contains(adminBrole))
            {
                rolesAllowedToread.Add(adminBrole);
            }

            var sharedOrganizationList = objectArtifact.SharedOrganizationList;
            if (sharedOrganizationList.Find(so => so.OrganizationId == organizationId) == null)
            {
                sharedOrganizationList.Add(
                new SharedOrganizationInfo
                {
                    OrganizationId = organizationId,
                    SharedPersonList = new List<string> { },
                    Tags = new string[] { RoleNames.AdminB }
                });
            }

            var sharedRoleList = objectArtifact.SharedRoleList;
            if (!sharedRoleList.Contains(adminBrole))
            {
                sharedRoleList.Add(adminBrole);
            }

            var departmentIdMetaDataValue = new MetaValuePair { Type = "string", Value = departmentId };
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];

            if (objectArtifact.MetaData.TryGetValue(departmentIdKey, out MetaValuePair value))
            {
                objectArtifact.MetaData[departmentIdKey] = departmentIdMetaDataValue;
            }
            else
            {
                objectArtifact.MetaData.Add(departmentIdKey, departmentIdMetaDataValue);
            }

            var updates = new Dictionary<string, object>
            {
                { "Tags", tags.ToArray()},
                { "RolesAllowedToRead", rolesAllowedToread.ToArray() },
                { "OrganizationId", organizationId },
                { "SharedOrganizationList", sharedOrganizationList },
                { "SharedRoleList", sharedRoleList },
                { "MetaData", objectArtifact.MetaData }
            };

            return updates;
        }

        private async Task<bool> FixPortalFileUploadIssue()
        {
            var systemObjectArtifactFixResponse = await UpdateSystemObjectArtifact(_systemOrganizationId);
            if (systemObjectArtifactFixResponse)
            {
                var organizations = GetAllOrganizations();
                if (organizations != null)
                {
                    await CreateOrganizationDefaultFolder(organizations);
                }
            }

            return systemObjectArtifactFixResponse;
        }

        private async Task<bool> UpdateSystemObjectArtifact(string objectArtifactId)
        {
            var update = PrepareSystemObjectArtifactUpdates();
            var builder = Builders<BsonDocument>.Filter;
            var updateFilter = builder.Eq("_id", objectArtifactId);

            _logger.LogInformation("Going to update ObjectArtifact with id -> {ObjectArtifactId},   Updates: {UpdatedData}.",
                objectArtifactId, JsonConvert.SerializeObject(update, Formatting.Indented));

            var response = await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilter, update);

            if (!response)
            {
                _logger.LogInformation("ObjectArtifact update wasn't successful with id -> -> {ObjectArtifactId},  ", objectArtifactId);
            }
            return response;
        }

        private Dictionary<string, object> PrepareSystemObjectArtifactUpdates()
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var updates = new Dictionary<string, object>
            {
                { "LastUpdateDate",  DateTime.UtcNow.ToLocalTime() },
                { "LastUpdatedBy", securityContext.UserId },
                { "Tags", new string[] { "create_folder" } },
                { "RolesAllowedToRead", new string[] { "admin", "task_controller" } },
                { "SharedOrganizationList", new List<SharedOrganizationInfo> { } },
                { "SharedRoleList", new List<string> { } }
            };

            return updates;
        }

        private List<PraxisOrganization> GetAllOrganizations()
        {
            return _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete && o.ItemId != _systemOrganizationId)?
                .Select(o => new PraxisOrganization
                {
                    ItemId = o.ItemId,
                    ClientName = o.ClientName,
                    UserLimit = o.UserLimit
                })
                .ToList();
        }

        private async Task<bool> CreateOrganizationDefaultFolder(List<PraxisOrganization> organizations)
        {
            foreach (var organization in organizations)
            {
                if (!string.IsNullOrEmpty(organization.ItemId))
                {
                    _logger.LogInformation($"Going to create folder for org:  {organization.ItemId}.");
                    await _organizationDataProcessService.ProcessOrganizationStorageSpaceAllocation(organization);
                }
            }

            return true;
        }

        private async Task<bool> FixArcFeatureLicensingData(List<PraxisOrganization> organizations)
        {
            List<Task<bool>> listOfTasks = new List<Task<bool>>();

            organizations.ForEach(organization =>
            {
                listOfTasks.Add(CreateOrUpdateOrganizationArcFeatureLicensing(organization.ItemId, organization.UserLimit));
            });

            var response = await Task.WhenAll<bool>(listOfTasks);
            var isSuccess = response.All(r => r);

            return isSuccess;
        }

        private async Task<bool> CreateOrUpdateOrganizationArcFeatureLicensing(string organizationId, int orgUserLimit)
        {
            var departments = GetAllOrganizationDepartments(organizationId);
            var departmentIds = departments.Select(d => d.ItemId).ToList();
            var departmentLicenses = GetDepartmentArcFeatureLicensings(departmentIds);

            var orgLicenceDataUpdates = PrepareOrganizationLicensingDataFromDepartmentLicenses(departmentLicenses, orgUserLimit);
            var existingOrgArcFeatureLicensing = GetOrganizationFeatureLicensing(organizationId);

            var orgArcFeatureLicensing =
                existingOrgArcFeatureLicensing == null ?
                PrepareOrganizationLicensing(organizationId, orgLicenceDataUpdates) :
                PrepareOrganizationLicensingUpdate(existingOrgArcFeatureLicensing, orgLicenceDataUpdates);
            var response =
                existingOrgArcFeatureLicensing == null ?
                await CreateArcFeatureLicensing(orgArcFeatureLicensing) :
                await UpdateArcFeatureLicensing(existingOrgArcFeatureLicensing, orgArcFeatureLicensing);

            return response;
        }

        private List<PraxisClient> GetAllOrganizationDepartments(string organizationId)
        {
            var departments = _repository.GetItems<PraxisClient>(d => d.ParentOrganizationId == organizationId && !d.IsMarkedToDelete)?
                .Select(d => new PraxisClient
                {
                    ItemId = d.ItemId
                })
                .ToList();

            return departments == null ? new List<PraxisClient> { } : departments;
        }

        private List<ArcFeatureLicensing> GetDepartmentArcFeatureLicensings(List<string> departmentIds)
        {
            var licences = _repository.GetItems<ArcFeatureLicensing>(l => departmentIds.Contains(l.OrganizationId))?
                .Select(l => new ArcFeatureLicensing
                {
                    ItemId = l.ItemId,
                    UsageLimit = l.UsageLimit,
                    Usage = l.Usage
                })
                .ToList();

            return licences == null ? new List<ArcFeatureLicensing> { } : licences;
        }

        private ArcFeatureLicensing PrepareOrganizationLicensingDataFromDepartmentLicenses(List<ArcFeatureLicensing> departmentLicenses, double orgUserLimit)
        {
            var departmentUsageLimitSum = departmentLicenses.Sum(l => l.UsageLimit);
            var orgStorageLimit = (orgUserLimit * 0.5) * Math.Pow(1024, 3);

            var orgArcFeatureLicensing = new ArcFeatureLicensing()
            {
                UsageLimit = Math.Max(departmentUsageLimitSum, orgStorageLimit),
                Usage = departmentLicenses.Sum(l => l.Usage)
            };

            return orgArcFeatureLicensing;
        }

        private ArcFeatureLicensing GetOrganizationFeatureLicensing(string organizationId)
        {
            return _repository.GetItem<ArcFeatureLicensing>(l => l.OrganizationId == organizationId);
        }

        private ArcFeatureLicensing PrepareOrganizationLicensing(string organizationId, ArcFeatureLicensing data)
        {
            var orgArcFeatureLicensing = new ArcFeatureLicensing()
            {
                ItemId = Guid.NewGuid().ToString(),
                FeatureId = "praxis-license",
                OrganizationId = organizationId,
                IsLicensed = true,
                IsLimitEnable = true,
                UsageLimit = data.UsageLimit,
                Usage = data.Usage,
                OverUseLimit = 0
            };

            return orgArcFeatureLicensing;
        }

        private ArcFeatureLicensing PrepareOrganizationLicensingUpdate(ArcFeatureLicensing currentLicenseData, ArcFeatureLicensing newLicenceData)
        {
            currentLicenseData.UsageLimit = newLicenceData.UsageLimit;
            currentLicenseData.Usage = newLicenceData.Usage;

            return currentLicenseData;
        }

        private async Task<bool> CreateArcFeatureLicensing(ArcFeatureLicensing data)
        {
            try
            {
                await _repository.SaveAsync(data);
                _logger.LogInformation("ArcFeatureLicensing created for OrganizationId: {ItemId} with Updates: {Updates}.",
                    data.ItemId, JsonConvert.SerializeObject(data, Formatting.Indented));
                return true;
            }
            catch
            {
                _logger.LogError("ArcFeatureLicensing creation failed for OrganizationId: {ItemId}.", data.ItemId);

                return false;
            }
        }

        private async Task<bool> UpdateArcFeatureLicensing(ArcFeatureLicensing currentLicenseData, ArcFeatureLicensing newLicenceData)
        {
            _logger.LogInformation("ArcFeatureLicensing found for OrganizationId: {ItemId} with Updates: {Updates}.",
                newLicenceData.ItemId, JsonConvert.SerializeObject(currentLicenseData, Formatting.Indented));
            try
            {
                await _repository.UpdateAsync(data => data.ItemId == newLicenceData.ItemId, newLicenceData);

                _logger.LogInformation(
                    "ArcFeatureLicensing updated for OrganizationId: {ItemId} with Updates: {Updates}.",
                    newLicenceData.ItemId, JsonConvert.SerializeObject(newLicenceData, Formatting.Indented));
                return true;
            }
            catch
            {
                _logger.LogError("ArcFeatureLicensing update failed for OrganizationId: {ItemId}.",newLicenceData.ItemId);
                return false;
            }
        }
    }
}
