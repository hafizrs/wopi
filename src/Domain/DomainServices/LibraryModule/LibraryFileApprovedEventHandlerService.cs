using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFileApprovedEventHandlerService : ILibraryFileApprovedEventHandlerService
    {
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IObjectArtifactSyncService _objectArtifactSyncService;
        private readonly IObjectArtifactActivationDeactivationService _objectArtifactActivationDeactivationService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;
        private readonly IDocumentEditHistoryService _documentEditHistoryService;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;
        private readonly ILogger<LibraryFileApprovedEventHandlerService> _logger;
        private readonly IVectorDBFileService _vectorDBFileService;

        public LibraryFileApprovedEventHandlerService(
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IObjectArtifactSyncService objectArtifactSyncService,
            IObjectArtifactActivationDeactivationService objectArtifactActivationDeactivationService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService,
            IDocumentEditHistoryService documentEditHistoryService,
            IRepository repository,
            IChangeLogService changeLogService,
            ILogger<LibraryFileApprovedEventHandlerService> logger,
            IVectorDBFileService vectorDBFileService
        )
        {
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _objectArtifactSyncService = objectArtifactSyncService;
            _objectArtifactActivationDeactivationService = objectArtifactActivationDeactivationService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
            _documentEditHistoryService = documentEditHistoryService;
            _repository = repository;
            _changeLogService = changeLogService;
            _logger = logger;
            _vectorDBFileService = vectorDBFileService;
        }

        public async Task<bool> InitiateLibraryFileApprovedAfterEffects(string artifactId)
        {
            var response = false;
            var objectArtifactData = _objectArtifactUtilityService.GetObjectArtifactById(artifactId);

            if (objectArtifactData != null)
            {
                await _vectorDBFileService.HandleManualFileUpload(objectArtifactData);
                await ProcessDependentEnititiesWithNewVersion(objectArtifactData);
                await UpdateDependencies(objectArtifactData);
                return true;
            }
            return response;
        }

        private async Task ProcessDependentEnititiesWithNewVersion(ObjectArtifact artifact)
        {
            if (!_objectArtifactUtilityService.IsAApprovedObjectArtifact(artifact.MetaData)) return;
            if (_objectArtifactUtilityService.IsADocument(artifact?.MetaData, true)) return;

            var originalArtifactId = _objectArtifactUtilityService.GetOriginalArtifactId(artifact?.MetaData, true);
            if (!string.IsNullOrEmpty(originalArtifactId))
            {
                var parentArtifact = _objectArtifactUtilityService.GetObjectArtifactById(originalArtifactId);
                if (parentArtifact != null)
                {
                    var artifactVersionLevel = IsSameLevelVersion(parentArtifact, artifact);
                    if (artifactVersionLevel.IsSameLevelVersion)
                    {
                        await UpdateDependentEnititiesWithNewVersion(new List<string>() { parentArtifact.ItemId }, artifact);
                    } 
                    else if (!artifactVersionLevel.IsParentFloating && artifactVersionLevel.IsChildFloating && artifactVersionLevel.ParentVersion >= 2) 
                    {
                        var allAncestors = await _documentEditHistoryService.GeneratePreviousHistoryByArtifactId(artifact.ItemId);
                        var ancestorIds = allAncestors.Select(a => a.ObjectArtifactId).ToList();
                        ancestorIds.AddRange(allAncestors.Select(a => a.ParentObjectArtifactId).Where(o => !string.IsNullOrEmpty(o)));
                        ancestorIds = ancestorIds.Distinct().ToList();

                        var allLinkedArtifacts = _documentEditHistoryService.GenerateAllLinkedArtifactsByArtifactIds(ancestorIds);
                        allLinkedArtifacts.AddRange(allAncestors);
                        allLinkedArtifacts = allLinkedArtifacts.DistinctBy(a => a.ObjectArtifactId).ToList();
                        var artifactIds = new List<string>();

                        foreach (var linkedArtifact in allLinkedArtifacts)
                        {
                            if (string.IsNullOrEmpty(linkedArtifact?.ObjectArtifactId) || artifact.ItemId == linkedArtifact.ObjectArtifactId) continue;

                            var isParsed = double.TryParse(linkedArtifact.Version, out double versionInDouble);
                            if (isParsed && versionInDouble < artifactVersionLevel.ChildVersion && versionInDouble % 1 != 0)
                            {
                                artifactIds.Add(linkedArtifact.ObjectArtifactId);
                            }
                        }

                        if (artifactIds.Count > 0)
                        {
                            await UpdateDependentEnititiesWithNewVersion(artifactIds, artifact);
                        }
                    }
                }
            }

        }

        private async Task UpdateDependentEnititiesWithNewVersion(List<string> previousArtifactIds, ObjectArtifact artifact)
        {
            await UpdateChildArtifactPermissions(previousArtifactIds, artifact);
            await _objectArtifactSyncService.UpdateEntityDependencyAsync(previousArtifactIds, artifact);

            previousArtifactIds.ForEach(async previousArtifactId =>
            {
                var inactiveCommand = new ObjectArtifactActivationDeactivationCommand()
                {
                    ObjectArtifactId = previousArtifactId,
                    Activate = false
                };
                await _objectArtifactActivationDeactivationService.InitiateObjectArtifactActivationDeactivationProcess(inactiveCommand);
            });
        }

        private async Task UpdateChildArtifactPermissions(List<string> previousArtifactIds, ObjectArtifact artifact)
        {
            try
            {
                var artifacts = _repository.GetItems<ObjectArtifact>(o => !o.IsMarkedToDelete && previousArtifactIds.Contains(o.ItemId))?.ToList()
                ?? new List<ObjectArtifact>();

                var sharedOrgList = artifacts
                    .Where(a => a.SharedOrganizationList?.Count > 0)
                    .SelectMany(a => a.SharedOrganizationList)
                    .Where(s => !string.IsNullOrEmpty(s.OrganizationId))
                    .GroupBy(s => s.OrganizationId)
                    .Select(g => new SharedOrganizationInfo() { 
                        OrganizationId = g.Key,
                        FeatureName = _objectArtifactUtilityService.IsAForm(artifact.MetaData) ? "form_fill" : "update",
                        SharedPersonList = g.Where(a => a.SharedPersonList?.Count > 0).SelectMany(a => a.SharedPersonList).Distinct().ToList(),
                        Tags = g?.Where(a => a.Tags != null).SelectMany(a => a.Tags).Distinct()?.ToArray()
                    })
                    .ToList();


                var updateArtifact = new ObjectArtifact
                {
                    SharedOrganizationList = sharedOrgList ?? artifact.SharedOrganizationList,
                    SharedPersonIdList = MergeLists(artifact.SharedPersonIdList, artifacts, a => a.SharedPersonIdList),
                    SharedRoleList = MergeLists(artifact.SharedRoleList, artifacts, a => a.SharedRoleList),
                    SharedUserIdList = MergeLists(artifact.SharedUserIdList, artifacts, a => a.SharedUserIdList),
                    RolesAllowedToRead = MergeArrays(artifact.RolesAllowedToRead, artifacts, a => a.RolesAllowedToRead),
                    IdsAllowedToRead = MergeArrays(artifact.IdsAllowedToRead, artifacts, a => a.IdsAllowedToRead),
                    RolesAllowedToUpdate = MergeArrays(artifact.RolesAllowedToUpdate, artifacts, a => a.RolesAllowedToUpdate),
                    IdsAllowedToUpdate = MergeArrays(artifact.IdsAllowedToUpdate, artifacts, a => a.IdsAllowedToUpdate)
                };

                var updates = new Dictionary<string, object>
                {
                    { nameof(ObjectArtifact.SharedOrganizationList), updateArtifact.SharedOrganizationList },
                    { nameof(ObjectArtifact.SharedPersonIdList), updateArtifact.SharedPersonIdList },
                    { nameof(ObjectArtifact.SharedRoleList), updateArtifact.SharedRoleList },
                    { nameof(ObjectArtifact.SharedUserIdList), updateArtifact.SharedUserIdList },
                    { nameof(ObjectArtifact.RolesAllowedToRead), updateArtifact.RolesAllowedToRead },
                    { nameof(ObjectArtifact.IdsAllowedToRead), updateArtifact.IdsAllowedToRead },
                    { nameof(ObjectArtifact.RolesAllowedToUpdate), updateArtifact.RolesAllowedToUpdate },
                    { nameof(ObjectArtifact.IdsAllowedToUpdate), updateArtifact.IdsAllowedToUpdate }
                };

                var builder = Builders<BsonDocument>.Filter;
                var updateFilters = builder.Eq("_id", artifact.ItemId);
                await _changeLogService.UpdateChange(nameof(ObjectArtifact), updateFilters, updates);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in UpdateChildArtifactPermissions.    error: {message}", ex.Message);
            }
        }

        List<T> MergeLists<T>(IEnumerable<T> baseList, IEnumerable<ObjectArtifact> artifacts, Func<ObjectArtifact, IEnumerable<T>> selector)
        {
            return (baseList ?? Enumerable.Empty<T>())
                .Concat(artifacts.Where(a => selector(a)?.Any() == true).SelectMany(selector))
                .Distinct()
                .ToList();
        }

        T[] MergeArrays<T>(IEnumerable<T> baseArray, IEnumerable<ObjectArtifact> artifacts, Func<ObjectArtifact, IEnumerable<T>> selector)
        {
            return (baseArray ?? Enumerable.Empty<T>())
                .Concat(artifacts.Where(a => selector(a) != null).SelectMany(selector))
                .Distinct()
                .ToArray();
        }

        private ArtifactParentChildVersionLevel IsSameLevelVersion(ObjectArtifact parentArtifact, ObjectArtifact childArtifact)
        {
            try
            {
                var parentVersion = _objectArtifactUtilityService.GetMetaDataValueByKey(parentArtifact.MetaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.VERSION}"]) ?? "1.00";
                var childVersion = _objectArtifactUtilityService.GetMetaDataValueByKey(childArtifact.MetaData, LibraryModuleConstants.ObjectArtifactMetaDataKeys[$"{ObjectArtifactMetaDataKeyEnum.VERSION}"]) ?? "1.00";

                double parentDouble = double.Parse(parentVersion);
                double childDouble = double.Parse(childVersion);

                bool isParentFloating = parentDouble % 1 != 0;
                bool isChildFloating = childDouble % 1 != 0;

                return new ArtifactParentChildVersionLevel() 
                { 
                    IsSameLevelVersion = parentVersion != childVersion && isParentFloating == isChildFloating,
                    IsParentFloating = isParentFloating,
                    IsChildFloating = isChildFloating,
                    ChildVersion = childDouble,
                    ParentVersion = parentDouble
                };
            }
            catch (Exception)
            {
                return new ArtifactParentChildVersionLevel();
            }
        }

        private async Task UpdateDependencies(ObjectArtifact objectArtifact)
        {
            if (_objectArtifactUtilityService.IsNotifiedToCockpit(objectArtifact.MetaData))
            {
                await _cockpitDocumentActivityMetricsGenerationService.OnDocumentShareGenerateActivityMetrics(
                    new[] { objectArtifact.ItemId }, $"{CockpitDocumentActivityEnum.DOCUMENTS_ASSIGNED}");
            }
            await _cockpitDocumentActivityMetricsGenerationService.OnDocumentApproveGenerateActivityMetrics(
                objectArtifact.ItemId);
        }

    }
}