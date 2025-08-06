using System.Collections.Generic;
using MongoDB.Bson;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using MongoDB.Bson.Serialization;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Newtonsoft.Json;
using System.Data;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Aspose.Pdf;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using AutoMapper.Internal;

namespace Selise.Ecap.SC.PraxisMonitor.Domain
{
    public class ObjectArtifactSearchResponseGeneratorService : IObjectArtifactSearchResponseGeneratorService
    {
        private readonly ILogger<ObjectArtifactSearchResponseGeneratorService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactAuthorizationCheckerService _objectArtifactAuthorizationCheckerService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactSharedDataResponseGeneratorService _objectArtifactSharedDataResponseGeneratorService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public ObjectArtifactSearchResponseGeneratorService(
            ILogger<ObjectArtifactSearchResponseGeneratorService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IObjectArtifactAuthorizationCheckerService objectArtifactAuthorizationCheckerService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactSharedDataResponseGeneratorService objectArtifactSharedDataResponseGeneratorService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            IRiqsPediaViewControlService riqsPediaViewControlService
        )
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _objectArtifactAuthorizationCheckerService = objectArtifactAuthorizationCheckerService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactSharedDataResponseGeneratorService = objectArtifactSharedDataResponseGeneratorService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public List<IDictionary<string, object>> PrepareArtifactResponse(
            List<BsonDocument> documents,
            ObjectArtifactSearchCommand command)
        {
            RiqsObjectArtifactMappingConstant.ResetRiqsArtifactMappingData(null);
            var viewMode = _objectArtifactUtilityService.GetLibraryViewModeKey(command.Type);

            var entityDictionary = GetDependentArtifactResponseProperties(documents, command.OrganizationId).GetAwaiter().GetResult();

            var artifacts =
                viewMode == $"{LibraryViewModeEnum.ALL}" ? PrepareAllViewArtifactResponse(documents, entityDictionary) :
                viewMode == $"{LibraryViewModeEnum.APPROVAL_VIEW}" ? PrepareApprovalViewArtifactResponse(documents, entityDictionary) :
                viewMode == $"{LibraryViewModeEnum.DOCUMENT}" ? PrepareDocumentArtifactResponse(documents, entityDictionary) :
                PrepareFormArtifactResponse(documents, entityDictionary);

            return artifacts;
        }


        public async Task<ArtifactResponseEntityDictionary> GetDependentArtifactResponseProperties(List<BsonDocument> documents, string organizationId = null)
        {
            var entityDictionary = new ArtifactResponseEntityDictionary();
            try
            {
                List<PraxisUser> _praxisUsers;
                List<RiqsLibraryControlMechanism> _controlMechanismDatas;
                List<RiqsObjectArtifactMapping> _artifactMappingDatas;
                List<DocumentEditMappingRecord> _documentMappingDatas;
                List<PraxisClient> _praxisClients;
                List<PraxisOrganization> _praxisOrganizations;
                List<DmsArtifactUsageReference> _dmsArtifactUsageReferences;
                RiqsPediaViewControlResponse _riqsViewControl;

                var artifactIds = new HashSet<string>();
                var userIds = new HashSet<string>() { _securityContextProvider.GetSecurityContext().UserId };
                var praxisUserIds = new HashSet<string>();
                var userRoles = new HashSet<string>();
                var parentIds = new HashSet<string>();
                var orgIds = new HashSet<string>();
                var clientIds = new HashSet<string>();
                var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                bool isFileArtifact = false;

                if (!string.IsNullOrEmpty(departmentId)) clientIds.Add(departmentId);

                documents.ForEach(document =>
                {
                    var artifact = BsonSerializer.Deserialize<RiqsObjectArtifact>(document);
                    isFileArtifact = artifact.ArtifactType == ArtifactTypeEnum.File;

                    GetDependentIds(artifact, artifactIds, parentIds, userIds, praxisUserIds, userRoles, orgIds, clientIds);
                });

                var foundFormResponses = documents?
                            .Where(doc => doc.TryGetValue("FormResponses", out BsonValue value) && value != null)?
                            .SelectMany(doc => doc["FormResponses"].AsBsonArray)?
                            .Select(response => BsonSerializer.Deserialize<RiqsObjectArtifact>(response.AsBsonDocument))?
                            .ToList() ?? new List<RiqsObjectArtifact>();

                foundFormResponses.ForEach(formArtifact =>
                {
                    GetDependentIds(formArtifact, artifactIds, parentIds, userIds, praxisUserIds, userRoles, orgIds, clientIds);
                });

                var deptIdsforDeptLevelOfficers = _securityHelperService.IsADepartmentLevelUser() ? clientIds : new HashSet<string>();

                var praxisClientsTask = Task.Run(() => _repository.GetItems<PraxisClient>(c => clientIds.Contains(c.ItemId))?.ToList());
                var praxisOrganizationsTask = Task.Run(() => _repository.GetItems<PraxisOrganization>(o => orgIds.Contains(o.ItemId))?.ToList());
                var controlMechanismDatasTask = Task.Run(() => _repository.GetItems<RiqsLibraryControlMechanism>
                            (l => !l.IsMarkedToDelete && (orgIds.Contains(l.OrganizationId) || deptIdsforDeptLevelOfficers.Contains(l.DepartmentId)))?.ToList());
                var documentMappingDatasTask = Task.Run(() => _repository.GetItems<DocumentEditMappingRecord>(d => !d.IsMarkedToDelete && (artifactIds.Contains(d.ObjectArtifactId) ||
                            (artifactIds.Contains(d.ParentObjectArtifactId) && d.IsDraft && !string.IsNullOrEmpty(d.ObjectArtifactId))))?.ToList());
                var dmsArtifactUsageTask = Task.Run(() => GetDmsArtifactUsageReferencesOfDirectUsage(artifactIds.ToList()));

                var viewControltask = _riqsPediaViewControlService.GetRiqsPediaViewControl();

                await Task.WhenAll(praxisClientsTask, praxisOrganizationsTask, controlMechanismDatasTask, documentMappingDatasTask, dmsArtifactUsageTask, viewControltask);

                _praxisClients = await praxisClientsTask;
                _praxisOrganizations = await praxisOrganizationsTask;
                _controlMechanismDatas = await controlMechanismDatasTask;
                _documentMappingDatas = await documentMappingDatasTask;
                _dmsArtifactUsageReferences = await dmsArtifactUsageTask;
                _riqsViewControl = await viewControltask;

                entityDictionary = new ArtifactResponseEntityDictionary()
                {
                    RiqsViewControl = _riqsViewControl,
                    LoggedInDepartmentId = _securityHelperService.IsADepartmentLevelUser() ? departmentId : string.Empty,
                    LoggedInOrganizationId = organizationId ?? _securityHelperService.ExtractOrganizationFromOrgLevelUser(),
                    PraxisClients = _praxisClients,
                    PraxisOrganizations = _praxisOrganizations,
                    RiqsLibraryControlMechanisms = _controlMechanismDatas,
                    DocumentEditMappingRecords = _documentMappingDatas,
                    DmsArtifactUsageReferences = _dmsArtifactUsageReferences
                };

                _documentMappingDatas.ForEach(doc =>
                {
                    if (doc?.EditHistory?.Count > 0)
                    {
                        var lastEditUser = doc?.EditHistory?.OrderByDescending(editItem => editItem.EditDate)?
                                                    .FirstOrDefault();
                        if (!string.IsNullOrEmpty(lastEditUser?.EditorUserId))
                        {
                            userIds.Add(lastEditUser?.EditorUserId);
                        }
                    }
                });

                _artifactMappingDatas = _repository.GetItems<RiqsObjectArtifactMapping>(
                    a => !a.IsMarkedToDelete && (artifactIds.Contains(a.ObjectArtifactId) || parentIds.Contains(a.ObjectArtifactId))
                )?.ToList();
                entityDictionary.RiqsObjectArtifactMappings = _artifactMappingDatas;

                foreach (var mappingData in _artifactMappingDatas)
                {
                    string performBy = mappingData?.FormCompletionSummary?.Select(f => f.PerformedBy)?.FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? string.Empty;
                    if (!string.IsNullOrEmpty(performBy)) praxisUserIds.Add(performBy);
                }

                _praxisUsers = GetPraxisUsersByUserIds(userIds.ToList(), praxisUserIds.ToList(), userRoles.ToList());
                entityDictionary.PraxisUsers = _praxisUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception Occured in GetDependentArtifactResponseProperties -> {ErrorMessage} -> {StackTrace}", ex.Message, ex.StackTrace);
            }
            return entityDictionary;
        }

        private async Task<List<DocumentEditMappingRecord>> FetchDocumentMappingDataAsync(bool isFileArtifact, HashSet<string> artifactIds)
        {
            return await Task.Run(() => isFileArtifact
                ? _repository.GetItems<DocumentEditMappingRecord>(d => !d.IsMarkedToDelete &&
                    (artifactIds.Contains(d.ObjectArtifactId) ||
                     (artifactIds.Contains(d.ParentObjectArtifactId) && d.IsDraft && !string.IsNullOrEmpty(d.ObjectArtifactId))))?.ToList()
                : new List<DocumentEditMappingRecord>());
        }

        private async Task<List<DmsArtifactUsageReference>> FetchDmsArtifactUsageReferencesAsync(bool isFileArtifact, HashSet<string> artifactIds)
        {
            var dmsArtifactUsageTask = Task.Run(() => isFileArtifact ? GetDmsArtifactUsageReferencesOfDirectUsage(artifactIds.ToList()) : new List<DmsArtifactUsageReference>());
            var dmsArtifactUsageBypermissionTask = Task.Run(() => isFileArtifact ? GetDmsArtifactUsageReferencesOfDirectUsageByPermission(artifactIds.ToList()) : new List<DmsArtifactUsageReference>());
            
            var dmsArtifactUsageReferences = await dmsArtifactUsageTask;
            var dmsArtifactUsageByPermissionReferences = await dmsArtifactUsageBypermissionTask;

            return dmsArtifactUsageReferences.Concat(dmsArtifactUsageByPermissionReferences).ToList();

        }

        private void GetDependentIds
        (
            RiqsObjectArtifact artifact,
            HashSet<string> artifactIds,
            HashSet<string> parentIds,
            HashSet<string> userIds,
            HashSet<string> praxisUserIds,
            HashSet<string> userRoles,
            HashSet<string> orgIds,
            HashSet<string> clientIds)
        {
            artifactIds.Add(artifact.ItemId);

            if (!string.IsNullOrEmpty(artifact.CreatedBy)) userIds.Add(artifact.CreatedBy);
            if (!string.IsNullOrEmpty(artifact.ParentId)) parentIds.Add(artifact.ParentId);
            if (!string.IsNullOrEmpty(artifact.OrganizationId)) orgIds.Add(artifact.OrganizationId);

            var departmentId = (string)GetMetaValue(artifact, $"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID}");
            if (!string.IsNullOrEmpty(departmentId)) clientIds.Add(departmentId);
            
            if (artifact?.SharedOrganizationList?.Count > 0)
            {
                foreach (var sharedOrg in artifact?.SharedOrganizationList)
                {
                    if (sharedOrg?.SharedPersonList?.Count > 0)
                    {
                        praxisUserIds.UnionWith(sharedOrg.SharedPersonList);
                    }
                    if (sharedOrg?.Tags?.Count() > 0)
                    {
                        foreach (var tag in sharedOrg.Tags)
                        {
                            if (LibraryModuleConstants.StaticRoleDynamicRolePrefixMap.ContainsKey(tag))
                            {
                                userRoles.Add($"{LibraryModuleConstants.StaticRoleDynamicRolePrefixMap[tag]}_{sharedOrg.OrganizationId}");
                            }
                        }
                    }
                }
            }
        }

        private List<DmsArtifactUsageReference> GetDmsArtifactUsageReferencesOfDirectUsage(List<string> objectArtifactIds)
        {
            var relatedEntity = new List<string>
            {
                EntityName.PraxisOpenItem,
                EntityName.PraxisEquipmentMaintenance,
                EntityName.CirsGenericReport
            };

            var completionStatus = new List<string>() { "Completed", "Done", "completed", "done", "COMPLETED", "DONE" };
            var queryFilter = new BsonDocument
                {
                    { "IsMarkedToDelete", false },
                    { "ObjectArtifactId", new BsonDocument("$in", new BsonArray(objectArtifactIds ?? new List<string>()) ) },
                    { "RelatedEntityName", new BsonDocument("$in", new BsonArray(relatedEntity)) },
                    { "$or", new BsonArray
                        {
                            new BsonDocument { { "TaskCompletionInfo", BsonNull.Value } },
                            new BsonDocument
                            {
                                { "TaskCompletionInfo.CompletionStatus", new BsonDocument("$nin", new BsonArray(completionStatus)) },
                            },
                            new BsonDocument
                            {
                                { "TaskCompletionInfo.DueDate", BsonNull.Value }
                            },
                            new BsonDocument
                            {
                                { "TaskCompletionInfo.DueDate", new BsonDocument("$gte", DateTime.UtcNow) }
                            }
                        }
                    }
                };

            return GetDmsArifactAfterusingStagePipeline(queryFilter);
        }
        
        private List<DmsArtifactUsageReference> GetDmsArtifactUsageReferencesOfDirectUsageByPermission(List<string> objectArtifactIds)
        {
            var queryFilter = new BsonDocument
            {
                { "IsMarkedToDelete", false },
                { "ObjectArtifactId", new BsonDocument("$in", new BsonArray(objectArtifactIds ?? new List<string>())) }
            };

            var isDepartmentLevelUser = _securityHelperService.IsADepartmentLevelUser();

            if (isDepartmentLevelUser)
            {
                var clientId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                ArgumentNullException.ThrowIfNull(clientId, nameof(clientId));

                var orgId = _securityHelperService.ExtractOrganizationFromOrgLevelUser();
                var orFilters = new BsonArray
                {
                    new BsonDocument
                    {
                        { "ClientInfos", new BsonDocument("$exists", true) },
                        { "ClientInfos.ClientId", clientId }
                    },
                    new BsonDocument { { "OrganizationId", orgId } },
                    new BsonDocument { { "OrganizationIds", new BsonDocument { { "$in", new BsonArray(new List<string> { orgId }) } } } }
                };
                queryFilter.Add("$or", orFilters);
            }

            return GetDmsArifactAfterusingStagePipeline(queryFilter);
        }

        private List<DmsArtifactUsageReference> GetDmsArifactAfterusingStagePipeline(BsonDocument queryFilter)
        {
            PipelineStageDefinition<BsonDocument, BsonDocument> groupPipeline = new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$ObjectArtifactId" },
                { "firstDocument", new BsonDocument("$first", "$$ROOT") }
            });

            PipelineStageDefinition<BsonDocument, BsonDocument> replaceRootStage = new BsonDocument("$replaceRoot", new BsonDocument
            {
                { "newRoot", "$firstDocument" }
            });

            var collection = _ecapMongoDbDataContextProvider
                        .GetTenantDataContext()
                        .GetCollection<BsonDocument>($"{nameof(DmsArtifactUsageReference)}s")
                        .Aggregate()
                        .Match(queryFilter);

            PipelineStageDefinition<BsonDocument, BsonDocument> projectionStage = new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { nameof(DmsArtifactUsageReference.ObjectArtifactId), 1 },
                { nameof(DmsArtifactUsageReference.RelatedEntityName), 1 },
                { nameof(DmsArtifactUsageReference.TaskCompletionInfo), 1 },
                { nameof(DmsArtifactUsageReference.OrganizationId), 1 },
                { nameof(DmsArtifactUsageReference.ClientInfos), 1 }
            });

            collection = collection.AppendStage(groupPipeline);
            collection = collection.AppendStage(replaceRootStage);
            collection = collection.AppendStage(projectionStage);

            var usageReferences = collection?.ToEnumerable()?.Select(document => BsonSerializer.Deserialize<DmsArtifactUsageReference>(document))?.ToList();

            return usageReferences ?? new List<DmsArtifactUsageReference>();
        }

        private List<IDictionary<string, object>> PrepareAllViewArtifactResponse(List<BsonDocument> documents, ArtifactResponseEntityDictionary entityDictionary)
        {
            var artifacts = new ConcurrentBag<(IDictionary<string, object>, long index)>();

            Parallel.ForEach(documents, (document, _, index) =>
            {
                var foundArtifact = BsonSerializer.Deserialize<RiqsObjectArtifact>(document);
                var fileType = _objectArtifactUtilityService.GetMetaDataValueByKey(foundArtifact.MetaData,
                     LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"]);

                var commomArtifactinfo = new SearchInfo(foundArtifact, LibraryModuleConstants.LibraryViewCommonResponseFields).GetSearchInfo();

                var addiitonalArtifactinfo = new Dictionary<string, object>
                {
                    {
                        nameof(RiqsObjectArtifact.ApprovalStatus),
                        GetApprovalStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.Keywords),
                        GetKeywordsData(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.UploadDetail),
                        GetUploadDetailData(foundArtifact, entityDictionary?.PraxisUsers)
                    },
                    {
                        nameof(RiqsObjectArtifact.Status),
                        GetStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.AssigneeDetail),
                        _objectArtifactSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(
                            foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl)
                    },
                    {
                        nameof(RiqsObjectArtifact.SharedObjectArtifactDetail),
                        _objectArtifactSharedDataResponseGeneratorService.GetSharedObjectArtifactResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisClients, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl)
                    },
                    {
                        nameof(RiqsObjectArtifact.Permissions),
                        foundArtifact.ArtifactType == ArtifactTypeEnum.Folder? GetFolderUserPermission(foundArtifact, entityDictionary): GetFileFeatureUserPermission(foundArtifact, entityDictionary)
                    }
                };

                if (foundArtifact.ArtifactType == ArtifactTypeEnum.File)
                {
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.HavePermissionForUsageReference), HaveAnyPermissionForUsageReference(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.NextReapproveDate), GetNextReapproveDate(foundArtifact));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.IsReapproveProcessStarted), _objectArtifactAuthorizationCheckerService.IsReapproveProcessStarted(foundArtifact?.MetaData));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.ApprovalDetails), GetApprovalDetailData(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.ReapprovalDetails), GetReapprovalDetailData(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.Version), GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.VERSION}") ?? "1.00");
                    
                    if (fileType == ((int)LibraryFileTypeEnum.DOCUMENT).ToString())
                    {
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.EditDetail), GetEditDocumentDetailData(foundArtifact, entityDictionary));
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.IsDraft), _objectArtifactUtilityService.IsADocument(foundArtifact?.MetaData, true));
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.HaveDraftedChildArtifact), HaveDraftedChildArtifact(foundArtifact, entityDictionary));
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.DraftedDocumentFileId), GetDraftedDocumentFileId(foundArtifact, entityDictionary));
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.VersionDepartmentName), GetVersionDepartmentName(foundArtifact, entityDictionary));
                    }
                    else if (fileType == ((int)LibraryFileTypeEnum.FORM).ToString())
                    {
                        var (formFilledByDetails, formFillPendingByDetails) =
                        (_securityHelperService.IsAAdminOrTaskConrtroller() ||
                        _objectArtifactAuthorizationCheckerService.IsALibraryAuthorityMember(foundArtifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings)) ?
                        _objectArtifactSharedDataResponseGeneratorService.GetFormFillActionDetails(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.RiqsObjectArtifactMappings) :
                        (null, null);
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.FormFilledBy), new { AssignedDepartmentList = formFilledByDetails });
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.FormFillPendingBy), new { AssignedDepartmentList = formFillPendingByDetails });
                        addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.FormResponse), PrepareFilledFormResponse(document, entityDictionary));
                    }
                }

                var artifact = commomArtifactinfo.Concat(addiitonalArtifactinfo).ToDictionary(x => x.Key, x => x.Value);
                artifacts.Add((artifact, index));
            });

            return artifacts.OrderBy(o => o.index).Select(o => o.Item1).ToList();
        }

        private List<IDictionary<string, object>> PrepareApprovalViewArtifactResponse(List<BsonDocument> documents, ArtifactResponseEntityDictionary entityDictionary)
        {
            var artifacts = new ConcurrentBag<(IDictionary<string, object>, long index)>();

            Parallel.ForEach(documents, (document, _, index) =>
            {
                var foundArtifact = BsonSerializer.Deserialize<RiqsObjectArtifact>(document);
                var fileType = _objectArtifactUtilityService.GetMetaDataValueByKey(foundArtifact.MetaData,
                     LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"]);

                var commomArtifactinfo = new SearchInfo(foundArtifact, LibraryModuleConstants.LibraryViewCommonResponseFields).GetSearchInfo();
                var approvalStatus = (string)GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}");
                var isDraftedArtifact = (string)GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.IS_DRAFT}");

                var addiitonalArtifactinfo = new Dictionary<string, object>
                {
                    {
                        nameof(RiqsObjectArtifact.Keywords),
                        GetKeywordsData(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.UploadDetail),
                        GetUploadDetailData(foundArtifact, entityDictionary?.PraxisUsers)
                    },
                    {
                        nameof(RiqsObjectArtifact.ApprovalStatus),
                        GetApprovalStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.Status),
                        GetStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.SharedObjectArtifactDetail),
                        approvalStatus == ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString()?
                        _objectArtifactSharedDataResponseGeneratorService.GetSharedObjectArtifactResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisClients, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl):
                        null
                    },
                    {
                        nameof(RiqsObjectArtifact.Permissions),
                        _objectArtifactAuthorizationCheckerService.IsAReapprovedArtifact(foundArtifact?.MetaData, true) ?
                                GetApprovedFileUserPermission(foundArtifact, entityDictionary) :
                                GetPendingFileUserPermission(foundArtifact, entityDictionary)
                    }
                };

                var isDocumentType = false;
                var isApprovedStatus = false;

                if (approvalStatus != ((int)LibraryFileApprovalStatusEnum.PENDING).ToString())
                {   
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.ApprovalDetails), GetApprovalDetailData(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.ReapprovalDetails), GetReapprovalDetailData(foundArtifact, entityDictionary));
                }

                if (
                    _objectArtifactAuthorizationCheckerService.HaveNextReapproveDateKey(foundArtifact?.MetaData) ||
                    approvalStatus == ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString()
                )
                {
                    isApprovedStatus = true;
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.HavePermissionForUsageReference), HaveAnyPermissionForUsageReference(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.NextReapproveDate), GetNextReapproveDate(foundArtifact));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.IsReapproveProcessStarted), _objectArtifactAuthorizationCheckerService.IsReapproveProcessStarted(foundArtifact?.MetaData));

                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.AssigneeDetail),
                        _objectArtifactSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl));
                }

                addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.Version), GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.VERSION}") ?? "1.00");
                if (fileType == ((int)LibraryFileTypeEnum.DOCUMENT).ToString())
                {
                    isDocumentType = true;
                }

                if (isDocumentType && (isApprovedStatus || !string.IsNullOrEmpty(isDraftedArtifact)))
                {
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.EditDetail), GetEditDocumentDetailData(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.IsDraft), _objectArtifactUtilityService.IsADocument(foundArtifact?.MetaData, true));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.HaveDraftedChildArtifact), HaveDraftedChildArtifact(foundArtifact, entityDictionary));
                    addiitonalArtifactinfo.Add(nameof(RiqsObjectArtifact.DraftedDocumentFileId), GetDraftedDocumentFileId(foundArtifact, entityDictionary));
                }

                var artifact = commomArtifactinfo.Concat(addiitonalArtifactinfo).ToDictionary(x => x.Key, x => x.Value); 
                artifacts.Add((artifact, index));
            });

            return artifacts.OrderBy(o => o.index).Select(o => o.Item1).ToList();
        }

        private List<IDictionary<string, object>> PrepareDocumentArtifactResponse(List<BsonDocument> documents, ArtifactResponseEntityDictionary entityDictionary)
        {
            var artifacts = new ConcurrentBag<(IDictionary<string, object>, long index)>();

            Parallel.ForEach(documents, (document, _, index) =>
            {
                var foundArtifact = BsonSerializer.Deserialize<RiqsObjectArtifact>(document);

                var commomArtifactinfo = new SearchInfo(foundArtifact, LibraryModuleConstants.LibraryViewCommonResponseFields).GetSearchInfo();
                
                var addiitonalArtifactinfo = new Dictionary<string, object>
                {
                    {
                        nameof(RiqsObjectArtifact.HaveDraftedChildArtifact),
                        HaveDraftedChildArtifact(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.IsDraft),
                        _objectArtifactUtilityService.IsADocument(foundArtifact?.MetaData, true)
                    },
                    {
                        nameof(RiqsObjectArtifact.DraftedDocumentFileId),
                        GetDraftedDocumentFileId(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.VersionDepartmentName),
                        GetVersionDepartmentName(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.Version),
                        GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.VERSION}")
                    },
                    {
                        nameof(RiqsObjectArtifact.Status),
                        GetStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.UploadDetail),
                        GetUploadDetailData(foundArtifact, entityDictionary?.PraxisUsers)
                    },
                    {
                        nameof(RiqsObjectArtifact.EditDetail),
                        GetEditDocumentDetailData(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.ApprovalDetails),
                        GetApprovalDetailData(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.ReapprovalDetails),
                        GetReapprovalDetailData(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.AssigneeDetail),
                        _objectArtifactSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl)
                    },
                    {
                        nameof(RiqsObjectArtifact.SharedObjectArtifactDetail),
                        _objectArtifactSharedDataResponseGeneratorService.GetSharedObjectArtifactResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisClients, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl)
                    },
                    {
                        nameof(RiqsObjectArtifact.Permissions),
                        GetDocumentUserPermission(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.Keywords),
                        GetKeywordsData(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.NextReapproveDate),
                        GetNextReapproveDate(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.IsReapproveProcessStarted),
                        _objectArtifactAuthorizationCheckerService.IsReapproveProcessStarted(foundArtifact?.MetaData)
                    },
                    {
                        nameof(RiqsObjectArtifact.HavePermissionForUsageReference),
                        HaveAnyPermissionForUsageReference(foundArtifact, entityDictionary)
                    }
                };

                var artifact = commomArtifactinfo.Concat(addiitonalArtifactinfo).ToDictionary(x => x.Key, x => x.Value);
                artifacts.Add((artifact, index));
            });

            return artifacts.OrderBy(o => o.index).Select(o => o.Item1).ToList();
        }

        private List<IDictionary<string, object>> PrepareFormArtifactResponse(List<BsonDocument> documents, ArtifactResponseEntityDictionary entityDictionary)
        {
            var artifacts = new ConcurrentBag<(IDictionary<string, object>, long index)>();

            Parallel.ForEach(documents, (document, _, index) =>
            {
                var foundArtifact = BsonSerializer.Deserialize<RiqsObjectArtifact>(document);

                var commomArtifactinfo = new SearchInfo(foundArtifact, LibraryModuleConstants.LibraryViewCommonResponseFields).GetSearchInfo();

                var assigneeDetails =
                _objectArtifactSharedDataResponseGeneratorService.GetObjectArtifactAssigneeDetailResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl);
                var sharedObjectArtifactResponse =
                _objectArtifactSharedDataResponseGeneratorService.GetSharedObjectArtifactResponse(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.PraxisClients, entityDictionary?.PraxisOrganizations, entityDictionary?.RiqsViewControl);
                var (formFilledByDetails, formFillPendingByDetails) =
                (_securityHelperService.IsAAdminOrTaskConrtroller() ||
                _objectArtifactAuthorizationCheckerService.IsALibraryAuthorityMember(foundArtifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings)) ?
                _objectArtifactSharedDataResponseGeneratorService.GetFormFillActionDetails(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.RiqsObjectArtifactMappings) :
                (null, null);

                var addiitonalArtifactinfo = new Dictionary<string, object>
                {
                    {
                        nameof(RiqsObjectArtifact.Department),
                        GetDepartmentName(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.Status),
                        GetStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.UploadDetail),
                        GetUploadDetailData(foundArtifact, entityDictionary?.PraxisUsers)
                    },
                    {
                        nameof(RiqsObjectArtifact.Version),
                        GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.VERSION}") ?? "1.00"
                    },
                    {
                        nameof(RiqsObjectArtifact.ApprovalDetails),
                        GetApprovalDetailData(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.ReapprovalDetails),
                        GetReapprovalDetailData(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.AssigneeDetail), assigneeDetails
                    },
                    {
                        nameof(RiqsObjectArtifact.SharedObjectArtifactDetail),
                        sharedObjectArtifactResponse
                    },
                    {
                        nameof(RiqsObjectArtifact.Keywords),
                        GetKeywordsData(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.FormFilledBy), formFilledByDetails
                    },
                    {
                        nameof(RiqsObjectArtifact.FormFillPendingBy), formFillPendingByDetails
                    },
                    {
                        nameof(RiqsObjectArtifact.Permissions),
                        GetFormUserPermission(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.FormResponse),
                        PrepareFilledFormResponse(document, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.NextReapproveDate),
                        GetNextReapproveDate(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.IsReapproveProcessStarted),
                        _objectArtifactAuthorizationCheckerService.IsReapproveProcessStarted(foundArtifact?.MetaData)
                    },
                    {
                        nameof(RiqsObjectArtifact.HavePermissionForUsageReference),
                        HaveAnyPermissionForUsageReference(foundArtifact, entityDictionary)
                    }
                };

                var artifact = commomArtifactinfo.Concat(addiitonalArtifactinfo).ToDictionary(x => x.Key, x => x.Value);
                artifacts.Add((artifact, index));
            });

            return artifacts.OrderBy(o => o.index).Select(o => o.Item1).ToList();
        }

        private FormResponseResult PrepareFilledFormResponse(BsonDocument originalFormArtifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            originalFormArtifact.TryGetValue("FormResponses", out BsonValue value);
            if (value != null)
            {
                var foundformResponses =
                    value.AsBsonArray.ToList()
                    .Select(v => BsonSerializer.Deserialize<RiqsObjectArtifact>(v.AsBsonDocument))
                    .ToList();

                originalFormArtifact.TryGetValue("FormResponseTotalCount", out BsonValue bsonTotalCount);

                var totalCount = (bsonTotalCount != null && bsonTotalCount.IsInt32) ? bsonTotalCount.AsInt32 : (foundformResponses?.Count >= 0 ? foundformResponses.Count : 0); 

                return new FormResponseResult()
                {
                    Data = PrepareFormResponseArtifactResponse(foundformResponses, true, entityDictionary),
                    TotalCount = totalCount
                };
            }
            return null;
        }

        public List<IDictionary<string, object>> PrepareFormResponseArtifactResponse(
            List<RiqsObjectArtifact> foundArtifacts,
            bool isCountRestricted = false,
            ArtifactResponseEntityDictionary entityDictionary = null
        )
        {
            var artifacts = new ConcurrentBag<(IDictionary<string, object>, long index)>();

            if (foundArtifacts == null) return new List<IDictionary<string, object>>();

            Parallel.ForEach(foundArtifacts, (foundArtifact, _, index) =>
            {
                if (isCountRestricted && index >= 2) return;

                var commomArtifactinfo = new SearchInfo(foundArtifact, LibraryModuleConstants.LibraryViewCommonResponseFields).GetSearchInfo();

                var (formFilledByDetails, formFillPendingByDetails) =
                    _objectArtifactSharedDataResponseGeneratorService.GetFormFillActionDetailsForFilledForm(foundArtifact, entityDictionary?.PraxisUsers, entityDictionary?.RiqsObjectArtifactMappings);
                var addiitonalArtifactinfo = new Dictionary<string, object>
                {
                    {
                        nameof(RiqsObjectArtifact.Department),
                        GetDepartmentName(foundArtifact, entityDictionary)
                    },
                    {
                        nameof(RiqsObjectArtifact.Status),
                        GetStatusValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.STATUS}")
                    },
                    {
                        nameof(RiqsObjectArtifact.Version),
                        GetMetaValue(foundArtifact, $"{ObjectArtifactMetaDataKeyEnum.VERSION}")
                    },
                    {
                        nameof(RiqsObjectArtifact.UploadDetail),
                        GetUploadDetailData(foundArtifact, entityDictionary?.PraxisUsers)
                    },
                    {
                        nameof(RiqsObjectArtifact.Keywords),
                        GetKeywordsData(foundArtifact)
                    },
                    {
                        nameof(RiqsObjectArtifact.FormFilledBy), formFilledByDetails
                    },
                    {
                        nameof(RiqsObjectArtifact.FormFillPendingBy), formFillPendingByDetails
                    },
                    {
                        nameof(RiqsObjectArtifact.Permissions),
                        GetFormResponseUserPermission(foundArtifact, entityDictionary)
                    }
                };

                var artifact = commomArtifactinfo.Concat(addiitonalArtifactinfo).ToDictionary(x => x.Key, x => x.Value);
                artifacts.Add((artifact, index));
            });

            return artifacts.OrderBy(o => o.index).Select(o => o.Item1).ToList();
        }

        private object GetMetaValue(RiqsObjectArtifact artifact, string feature)
        {
            if (artifact?.MetaData != null && !string.IsNullOrWhiteSpace(feature))
            {
                var metadata = artifact.MetaData;
                var key = LibraryModuleConstants.ObjectArtifactMetaDataKeys[feature];
                metadata.TryGetValue(key, out MetaValuePair value);
                if (value != null)
                {
                    return GetParsedValue(value);
                }
            }
            return null;
        }

        private object GetParsedValue(MetaValuePair metaValue)
        {
            object value = null;
            if (metaValue != null)
            {
                if (metaValue.Type == LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{ObjectArtifactMetaDataKeyTypeEnum.STRING}"])
                {
                    value = metaValue.Value;
                }
                else if (metaValue.Type == LibraryModuleConstants.ObjectArtifactMetaDataKeyTypes[$"{ObjectArtifactMetaDataKeyTypeEnum.DATETIME}"])
                {
                    DateTime.TryParse(metaValue.Value, out DateTime dateValue);
                    if (dateValue != null)
                    {
                        value = dateValue.ToUniversalTime();
                    }
                }
            }
            return value;
        }

        private string GetStatusValue(RiqsObjectArtifact artifact, string feature)
        {
            var value = (string)GetMetaValue(artifact, feature);
            var response =
                !string.IsNullOrEmpty(value) ?
                LibraryModuleConstants.ObjectArtifactStatusValueLanguageMap[value] :
                null;
            return response;
        }

        private string GetApprovalStatusValue(RiqsObjectArtifact artifact, string feature)
        {
            var value = (string)GetMetaValue(artifact, feature);
            var response =
                !string.IsNullOrEmpty(value) ?
                LibraryModuleConstants.ObjectArtifactApprovalStatusValueLanguageMap[value] :
                null;
            return response;
        }

        private EntityDetail GetDepartmentName(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var departmentId = (string)GetMetaValue(artifact, $"{ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID}");

            var department = new EntityDetail()
            {
                Id = departmentId,
                Name = entityDictionary?.PraxisClients?.Find(c => c.ItemId == departmentId)?.ClientName 
                        ?? _objectArtifactUtilityService.GetDepartmentById(departmentId)?.ClientName,
            };

            return department;
        }

        private ActionDetail GetUploadDetailData(RiqsObjectArtifact artifact, List<PraxisUser> praxisUsers)
        {
            var data = new ActionDetail();
            if (artifact != null)
            {
                var praxisUser = praxisUsers?.Find(pu => pu.UserId == artifact.CreatedBy);
                data.DateTime = artifact.CreateDate;
                data.Name = praxisUser != null && praxisUser.IsMarkedToDelete != true ? praxisUser.DisplayName : artifact.OwnerName;
            }

            return data;
        }

        private ActionDetail GetEditDocumentDetailData(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var data = new ActionDetail();
            if (artifact != null)
            {
                var editHistory = entityDictionary?.DocumentEditMappingRecords.Find(d => d.ObjectArtifactId == artifact.ItemId)?.EditHistory;

                var lastEditUser = editHistory?.OrderByDescending(editItem => editItem.EditDate)?
                                                .FirstOrDefault();

                if (lastEditUser == null) return null;

                data.Name = entityDictionary?.PraxisUsers?.Find(pu => pu.UserId == lastEditUser.EditorUserId)?.DisplayName;
                data.DateTime = lastEditUser.EditDate;
                return data;
            }

            return null;
        }

        private bool HaveDraftedChildArtifact(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            Predicate<DocumentEditMappingRecord> match = d => d.ParentObjectArtifactId == artifact.ItemId && d.IsDraft 
                        && !string.IsNullOrEmpty(d.ObjectArtifactId);
            if (_securityHelperService.IsADepartmentLevelUser())
            {
                var departmentId = entityDictionary?.LoggedInDepartmentId ?? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                match = d => d.ParentObjectArtifactId == artifact.ItemId && d.IsDraft
                        && !string.IsNullOrEmpty(d.ObjectArtifactId) && d.DepartmentId == departmentId;
            }
            else if (_securityHelperService.IsAAdminBUser())
            {
                var orgId = artifact.OrganizationId;
                match = d => d.ParentObjectArtifactId == artifact.ItemId && d.IsDraft
                        && !string.IsNullOrEmpty(d.ObjectArtifactId) && d.OrganizationId == orgId && string.IsNullOrEmpty(d.DepartmentId);
            }
            var mappingData = entityDictionary?.DocumentEditMappingRecords?.Find(match);
            return mappingData != null;
        }

        private string GetDraftedDocumentFileId(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var isDraft = _objectArtifactUtilityService.IsADocument(artifact?.MetaData, true);
            var documentMap = entityDictionary?.DocumentEditMappingRecords?.Find(d => d.ObjectArtifactId == artifact.ItemId && d.IsDraft);
            var fileId = string.Empty;
            if (
                documentMap != null && !string.IsNullOrEmpty(documentMap.ObjectArtifactId) &&
                !string.IsNullOrEmpty(documentMap.CurrentDocFileId) && isDraft
            )
            {
                fileId = documentMap.CurrentDocFileId;
            };
            return fileId;
        }

        private string GetVersionDepartmentName(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var documentMap = entityDictionary?.DocumentEditMappingRecords?.Find(d => d.ObjectArtifactId == artifact.ItemId);
            if (documentMap != null && !string.IsNullOrEmpty(documentMap.DepartmentId))
            {
                return entityDictionary?.PraxisClients?
                    .Find(c => c.ItemId == documentMap.DepartmentId)?
                    .ClientName ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task<bool> IsVersionHistoryAvailable(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            try
            {
                if (entityDictionary?.DocumentEditMappingRecords != null)
                {
                    var mappingData = entityDictionary?.DocumentEditMappingRecords?.Find(d => d.ObjectArtifactId == artifact.ItemId && !d.IsDraft);
                    return mappingData != null;
                }
                return await _repository.ExistsAsync<DocumentEditMappingRecord>(m =>
                    !m.IsDraft && m.ObjectArtifactId == artifact.ItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in IsVersionHistoryAvaialble -> message: {ErrorMessage} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        private List<ActionDetail> GetApprovalDetailData(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var mappingData = RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(artifact?.ItemId, entityDictionary?.RiqsObjectArtifactMappings);
            var approverDetails = mappingData?.ApproverInfos?
                .Where(a => a.ReapprovalCount == 0)?
                .Select(a => new ActionDetail()
                {
                    DateTime = a.ApprovedDate,
                    Name = a.ApproverName
                })?.ToList() ?? new List<ActionDetail>();

            return approverDetails;
        }

        private List<ReapproveDetail> GetReapprovalDetailData(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var mappingData = RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(artifact.ItemId, entityDictionary?.RiqsObjectArtifactMappings);
            var approverDetails = mappingData?.ApproverInfos?
                .Where(a => a.ReapprovalCount != 0)?
                .Select(a => new ReapproveDetail()
                {
                    DateTime = a.ApprovedDate,
                    Name = a.ApproverName,
                    ReapprovalCount = a.ReapprovalCount
                })?.ToList() ?? new List<ReapproveDetail>();

            return approverDetails;
        }

        private bool HaveAnyPermissionForUsageReference(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDict)
        {
            var usageRefs = entityDict?.DmsArtifactUsageReferences;
            if (usageRefs == null) return false;
            if (_securityHelperService.IsADepartmentLevelUser())
            {
                var deptId = entityDict?.LoggedInDepartmentId ?? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                return usageRefs.Exists(u => u.ObjectArtifactId == artifact.ItemId && 
                    ((u.OrganizationIds != null && u.OrganizationIds.Contains(artifact.OrganizationId))) || u.OrganizationId == artifact.OrganizationId || (u.ClientInfos != null && u.ClientInfos.Any(c => c.ClientId == deptId)));
            }
            return usageRefs.Exists(u => u.ObjectArtifactId == artifact.ItemId);
        }

        private string GetNextReapproveDate(RiqsObjectArtifact artifact)
        {
            var nextApproveDateKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.NEXT_REAPPROVE_DATE.ToString()];
            if (artifact?.MetaData != null && artifact.MetaData.TryGetValue(nextApproveDateKey, out MetaValuePair value))
            {
                return value.Value;
            }
            return string.Empty;
        }

        private string[] GetKeywordsData(RiqsObjectArtifact artifact)
        {
            var keywords = new string[] { };
            if (artifact != null)
            {
                var value = (string)GetMetaValue(artifact, $"{ObjectArtifactMetaDataKeyEnum.KEYWORDS}");
                if (!string.IsNullOrEmpty(value)) keywords = JsonConvert.DeserializeObject<string[]>(value);
            }
            return keywords;
        }

        private List<PraxisUser> GetPraxisUsersByUserIds(List<string> userIds, List<string> praxisUserIds, List<string> userRoles)
        {
            return _repository.GetItems<PraxisUser>(pu => pu.Active && (userIds.Contains(pu.UserId) || 
                praxisUserIds.Contains(pu.ItemId) || (pu.Roles.Any(r => userRoles.Contains(r)) && !pu.Roles.Contains(RoleNames.GroupAdmin)))
            )?.Select(pu => new PraxisUser()
            {
                ItemId = pu.ItemId,
                UserId = pu.UserId,
                DisplayName = pu.DisplayName,
                Roles = pu.Roles,
                Image = pu.Image,
                IsMarkedToDelete = pu.IsMarkedToDelete
            })?.ToList();
        }

        private List<PermissionModel> GetFolderUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permissions = new List<PermissionModel>();
            var featureList = LibraryModuleConstants.FolderUserPermissions;

            foreach (var feature in featureList)
            {
                PermissionModel permissionModel = null;
                if (feature == $"{ObjectArtifactPermissions.RENAME}")
                {
                    permissionModel = GetRenameFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.MOVE}")
                {
                    permissionModel = GetMoveFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.CHANGE_COLOR}")
                {
                    permissionModel = GetChangeColorFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.SHARE}")
                {
                    permissionModel = GetShareFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.VIEW_ONLY_ACCESS_CONTROL}")
                {
                    permissionModel = GetViewOnlyAccessControlPermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.EDIT_ACCESS_CONTROL}")
                {
                    permissionModel = GetEditAccessControlPermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.ADD_CHILDREN}")
                {
                    permissionModel = GetAddChildrenPermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.REMOVE}")
                {
                    permissionModel = GetRemoveFeaturePermission(artifact, entityDictionary);
                }

                if (permissionModel != null)
                {
                    permissions.Add(permissionModel);
                }
            }

            return permissions;
        }

        private List<PermissionModel> GetFileFeatureUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary, List<string> featureList = null)
        {
            var permissions = new List<PermissionModel>();
            if (featureList == null) featureList = LibraryModuleConstants.FileUserPermissions;

            foreach (var feature in featureList)
            {
                PermissionModel permissionModel = null;
                if (feature == $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}")
                {
                    permissionModel = GetActiveInactiveToggleFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.VIEW}")
                {
                    permissionModel = GetViewFeaturePermission();
                }
                else if (feature == $"{ObjectArtifactPermissions.DOWNLOAD}")
                {
                    permissionModel = GetDownloadFeaturePermission();
                }
                else if (feature == $"{ObjectArtifactPermissions.RENAME}")
                {
                    permissionModel = GetRenameFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.EDIT}")
                {
                    permissionModel = GetEditFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.EDIT_DOC}")
                {
                    permissionModel = GetEditDocFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.FILL_FORM}")
                {
                    permissionModel = GetFillFormFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.VERSION_HISTORY}")
                {
                    permissionModel = GetVersionHistoryFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.SHARE}")
                {
                    permissionModel = GetShareFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.MOVE}")
                {
                    permissionModel = GetMoveFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.VIEW_ONLY_ACCESS_CONTROL}")
                {
                    permissionModel = GetViewOnlyAccessControlPermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.EDIT_ACCESS_CONTROL}")
                {
                    permissionModel = GetEditAccessControlPermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.APPROVE}")
                {
                    permissionModel = GetApproveFeaturePermission(artifact, entityDictionary);
                }
                else if (feature == $"{ObjectArtifactPermissions.REMOVE}")
                {
                    permissionModel = GetRemoveFeaturePermission(artifact, entityDictionary);
                }

                if (permissionModel != null)
                {
                    permissions.Add(permissionModel);
                }
            }

            return permissions;
        }

        private List<PermissionModel> GetApprovedFileUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var featureList = LibraryModuleConstants.ApprovedFileUserPermissions;
            var permissions = GetFileFeatureUserPermission(artifact, entityDictionary, featureList);

            return permissions;
        }

        private List<PermissionModel> GetPendingDraftedDocumentFileUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permissions = new List<PermissionModel>();
            var permissionModel = GetApproveFeaturePermission(artifact, entityDictionary);
            if (permissionModel != null) permissions.Add(permissionModel);
            permissions.AddRange(GetApprovedFileUserPermission(artifact, entityDictionary));
            return permissions;
        }

        private List<PermissionModel> GetPendingFileUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permissions = new List<PermissionModel>();
            if (_objectArtifactUtilityService.IsADocument(artifact?.MetaData, true) || _objectArtifactAuthorizationCheckerService.IsAReapprovedArtifact(artifact?.MetaData, false))
            {
                permissions = GetPendingDraftedDocumentFileUserPermission(artifact, entityDictionary);
            }
            else
            {
                var featureList = LibraryModuleConstants.PendingFileUserPermissions;

                permissions = GetFileFeatureUserPermission(artifact, entityDictionary, featureList);
            }

            return permissions;
        }

        private List<PermissionModel> GetDocumentUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var featureList = LibraryModuleConstants.DocumentUserPermissions;
            var permissions = GetFileFeatureUserPermission(artifact, entityDictionary, featureList);

            return permissions;
        }

        private List<PermissionModel> GetFormUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var featureList = LibraryModuleConstants.FormUserPermissions;
            var permissions = GetFileFeatureUserPermission(artifact, entityDictionary, featureList);

            return permissions;
        }

        private List<PermissionModel> GetFormResponseUserPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var featureList = LibraryModuleConstants.FormResponseUserPermissions;
            var permissions = GetFileFeatureUserPermission(artifact, entityDictionary, featureList);

            return permissions;
        }

        private PermissionModel GetViewFeaturePermission()
        {
            var permission = new PermissionModel()
            {
                FeatureName = $"{ObjectArtifactPermissions.VIEW}",
                IsEnable = true
            };
            return permission;
        }

        private PermissionModel GetDownloadFeaturePermission()
        {
            var permission = new PermissionModel()
            {
                FeatureName = $"{ObjectArtifactPermissions.DOWNLOAD}",
                IsEnable = true
            };
            return permission;
        }

        private PermissionModel GetRenameFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAEditAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnWriteAccess(artifact, $"{ObjectArtifactPermissions.RENAME}", entityDictionary?.RiqsViewControl) :
                null;
            return permission;
        }

        private PermissionModel GetChangeColorFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAEditAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnWriteAccess(artifact, $"{ObjectArtifactPermissions.CHANGE_COLOR}", entityDictionary?.RiqsViewControl) :
                null;
            return permission;
        }

        private PermissionModel GetEditFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAEditAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnWriteAccess(artifact, $"{ObjectArtifactPermissions.EDIT}", entityDictionary?.RiqsViewControl) :
                null;
            ;
            return permission;
        }

        private PermissionModel GetEditDocFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var fileType = (string)GetMetaValue(artifact, $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}");
            var permission =
                _objectArtifactAuthorizationCheckerService.IsAEditAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) &&
                fileType == ((int)LibraryFileTypeEnum.DOCUMENT).ToString() ?
                GetFeaturePermissionBasedOnUpdateAccess(artifact, $"{ObjectArtifactPermissions.EDIT_DOC}") :
                null;
            return permission;
        }

        private PermissionModel GetFillFormFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission =
                !_objectArtifactAuthorizationCheckerService.IsAFormFillRestrictedUser(artifact) &&
                (_objectArtifactUtilityService.IsAOriginalForm(artifact.MetaData) ||
                _objectArtifactUtilityService.IsADraftedFormResponse(artifact.MetaData)) ?
                GetFillFormFeaturePermissionModel(artifact, $"{ObjectArtifactPermissions.FILL_FORM}", entityDictionary) :
                null;
            return permission;
        }

        private PermissionModel GetFillFormFeaturePermissionModel(RiqsObjectArtifact artifact, string featureName, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = new PermissionModel()
            {
                FeatureName = featureName,
                IsEnable = CanFillForm(artifact, entityDictionary)
            };

            return permission;
        }

        private PermissionModel GetVersionHistoryFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = GetFeaturePermissionBasedOnReadAccess(artifact, $"{ObjectArtifactPermissions.VERSION_HISTORY}", entityDictionary?.RiqsViewControl);
            permission.IsEnable = permission.IsEnable && IsVersionHistoryAvailable(artifact, entityDictionary).Result;
            return permission;
        }

        private PermissionModel GetAddChildrenPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = !_objectArtifactAuthorizationCheckerService.IsAArtifactUploadRestrictedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms) ?
                GetFeaturePermissionBasedOnUpdateAccess(artifact, $"{ObjectArtifactPermissions.ADD_CHILDREN}") :
                null;
            return permission;
        }

        private PermissionModel GetShareFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAShareAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) &&
                !_objectArtifactUtilityService.IsAGeneralForm(artifact.MetaData) ?
                GetFeaturePermissionBasedOnReadAccess(artifact, $"{ObjectArtifactPermissions.SHARE}", entityDictionary?.RiqsViewControl) :
                null;
            return permission;
        }

        private PermissionModel GetMoveFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.CanMoveObjectArtifact(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnWriteAccess(artifact, $"{ObjectArtifactPermissions.MOVE}", entityDictionary?.RiqsViewControl) :
                null;
            return permission;
        }

        private PermissionModel GetViewOnlyAccessControlPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAShareAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnReadAccess(artifact, $"{ObjectArtifactPermissions.VIEW_ONLY_ACCESS_CONTROL}", entityDictionary?.RiqsViewControl) :
                null;
            return permission;
        }

        private PermissionModel GetEditAccessControlPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = _objectArtifactAuthorizationCheckerService.IsAShareAllowedUser(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.RiqsViewControl) ?
                GetFeaturePermissionBasedOnUpdateAccess(artifact, $"{ObjectArtifactPermissions.EDIT_ACCESS_CONTROL}") :
                null;
            return permission;
        }

        private PermissionModel GetRemoveFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = GetFeaturePermissionBasedOnDeleteAccess(artifact, $"{ObjectArtifactPermissions.REMOVE}", entityDictionary?.RiqsViewControl);
            return permission;
        }

        private PermissionModel GetFeaturePermissionBasedOnReadAccess(RiqsObjectArtifact artifact, string featureName, RiqsPediaViewControlResponse viewControl)
        {
            var permission = new PermissionModel()
            {
                FeatureName = featureName,
                IsEnable = true
            };

            return permission;
        }

        private PermissionModel GetFeaturePermissionBasedOnUpdateAccess(RiqsObjectArtifact artifact, string featureName)
        {
            var permission = new PermissionModel()
            {
                FeatureName = featureName,
                IsEnable = CanEditObjectArtifact(artifact)
            };

            return permission;
        }

        private PermissionModel GetFeaturePermissionBasedOnWriteAccess(RiqsObjectArtifact artifact, string featureName, RiqsPediaViewControlResponse viewControl)
        {
            var permission = new PermissionModel()
            {
                FeatureName = featureName,
                IsEnable = _objectArtifactAuthorizationCheckerService.CanWriteObjectArtifact(artifact) && HaveDefaultUserPermission(artifact, viewControl)
            };

            return permission;
        }

        private bool HaveDefaultUserPermission(RiqsObjectArtifact artifact, RiqsPediaViewControlResponse viewControl)
        {
            if (viewControl == null)
            {
                return true;
            }
            if (!viewControl.IsShowViewState) return true;
            if (!_objectArtifactUtilityService.IsAOrgLevelArtifact(artifact.MetaData, artifact.ArtifactType)) return true;

            return viewControl.IsAdminViewEnabled;
        }

        private bool CanEditObjectArtifact(RiqsObjectArtifact artifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return
                (artifact?.RolesAllowedToUpdate?.Count() > 0  && artifact.RolesAllowedToUpdate.Any(r => securityContext.Roles.Contains(r))) ||
                (artifact?.IdsAllowedToUpdate?.Count() > 0 && artifact.IdsAllowedToUpdate.Contains(securityContext.UserId)) ||
                (artifact?.RolesAllowedToWrite?.Count() > 0 && artifact.RolesAllowedToWrite.Any(r => securityContext.Roles.Contains(r))) ||
                (artifact?.IdsAllowedToWrite?.Count() > 0 && artifact.IdsAllowedToWrite.Contains(securityContext.UserId));
        }

        private bool CanFillForm(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            return CanEditObjectArtifact(artifact) || HasFillFormSharedPermission(artifact, entityDictionary);
        }

        private bool HasFillFormSharedPermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var canFillForm = false;
            if (_securityHelperService.IsADepartmentLevelUser())
            {
                var departmentId = entityDictionary?.LoggedInDepartmentId ?? _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                var organizationId = GetParentOrganizationIdByClientId(departmentId);
                const string featureName = "form_fill";
                var userId = _securityContextProvider.GetSecurityContext().UserId;
                var praxisUserId = entityDictionary?.PraxisUsers?.Find(p => p.UserId == userId)?.ItemId;
                var deptStaticRoles = _securityHelperService.GetAllDepartmentLevelStaticRolesFromCurrentUserRoles();
                var orgGeneralAccessRole = _securityHelperService.GetOrganizationGeneralAccessRolePrefixFromCurrentUserRoles();
                var staticRoles = deptStaticRoles.Union(new[] { orgGeneralAccessRole }).ToArray();

                canFillForm = artifact.SharedOrganizationList?.Any(s => 
                    s.FeatureName == featureName &&
                    (s.OrganizationId == organizationId || s.OrganizationId == departmentId) &&
                    (s.SharedPersonList.Contains(praxisUserId) || staticRoles.Any(r => s.Tags.Contains(r)))) == true;
            }

            return canFillForm;
        }

        private PermissionModel GetFeaturePermissionBasedOnDeleteAccess(RiqsObjectArtifact artifact, string featureName, RiqsPediaViewControlResponse viewControl)
        {
            var permission = new PermissionModel()
            {
                FeatureName = featureName,
                IsEnable = CanRemoveObjectArtifact(artifact) && HaveDefaultUserPermission(artifact, viewControl)
            };

            return permission;
        }

        private bool CanRemoveObjectArtifact(RiqsObjectArtifact artifact)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            return
                artifact.RolesAllowedToDelete.Any(r => securityContext.Roles.Contains(r)) ||
                artifact.IdsAllowedToDelete.Contains(securityContext.UserId);
        }

        private PermissionModel GetActiveInactiveToggleFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = new PermissionModel()
            {
                FeatureName = $"{ObjectArtifactPermissions.ACTIVE_INACTIVE_TOGGLE}",
                IsEnable = _objectArtifactAuthorizationCheckerService.CanActiveInactiveObjectArtifact(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings, entityDictionary?.DmsArtifactUsageReferences, entityDictionary?.RiqsViewControl)
            };
            return permission;
        }

        private PermissionModel GetApproveFeaturePermission(RiqsObjectArtifact artifact, ArtifactResponseEntityDictionary entityDictionary)
        {
            var permission = new PermissionModel()
            {
                FeatureName = $"{ObjectArtifactPermissions.APPROVE}",
                IsEnable = _objectArtifactAuthorizationCheckerService.CanApproveObjectArtifact(artifact, entityDictionary?.RiqsLibraryControlMechanisms, entityDictionary?.RiqsObjectArtifactMappings)
            };
            return permission;
        }

        private string GetParentOrganizationIdByClientId(string clientId)
        {
            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId);
            return client?.ParentOrganizationId ?? string.Empty;
        }
    }
}
