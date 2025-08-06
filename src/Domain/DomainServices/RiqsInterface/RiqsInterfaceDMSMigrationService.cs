using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using MassTransit;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceDMSMigrationService : IRiqsInterfaceDMSMigrationService
    {
        private readonly IRepository _repository;
        private readonly ILogger<RiqsInterfaceDMSMigrationService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IDmsService _dmsService;
        private readonly ISharePointFileAndFolderInfoService _sharePointFileAndFolderInfoService;
        private readonly IStorageDataService _storageDataService;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        private readonly INotificationService _notificationService;
        private readonly ITokenService _tokenService;
        private readonly IGoogleDriveFileAndFolderInfoService _googleDriveFileAndFolderInfoService;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private readonly IDmsFileUploadedEventHandlerHandlerService _dmsFileUploadedEventHandlerHandlerService;

        public RiqsInterfaceDMSMigrationService(
            IRepository repository,
            ILogger<RiqsInterfaceDMSMigrationService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IDmsService dmsService,
            ISharePointFileAndFolderInfoService sharePointFileAndFolderInfoService,
            IStorageDataService storageDataService,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            INotificationService notificationService,
            ITokenService tokenService,
            IGoogleDriveFileAndFolderInfoService googleDriveFileAndFolderInfoService,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IDmsFileUploadedEventHandlerHandlerService dmsFileUploadedEventHandlerHandlerService)
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _dmsService = dmsService;
            _sharePointFileAndFolderInfoService = sharePointFileAndFolderInfoService;
            _storageDataService = storageDataService;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _notificationService = notificationService;
            _tokenService = tokenService;
            _googleDriveFileAndFolderInfoService = googleDriveFileAndFolderInfoService;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
            _dmsFileUploadedEventHandlerHandlerService = dmsFileUploadedEventHandlerHandlerService;
        }

        public async Task InitiateInterfaceMigration(InterfaceMigrationFolderAndFileCommand command)
        {
            var clock = new Stopwatch();
            clock.Start();

            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                var migrationSummery = await GetRiqsInterfaceMigrationSummeryAsync(command.InterfaceMigrationSummeryId);
                var workspaces = await GetWorkSpaceForCurrentUserId(securityContext.UserId);

                if (migrationSummery?.InterfaceFolders != null && migrationSummery.InterfaceFolders.Count() <= 1000)
                {
                    await CreateInterfaceMigrationFolder(migrationSummery, workspaces);
                }

                if (migrationSummery?.InterfaceFiles != null && migrationSummery.InterfaceFiles.Count() <= 1000)
                {
                    await UploadInterfaceMigrationFile(migrationSummery, workspaces);
                }

                _logger.LogInformation("Migration Summary: Total InterfaceFolders = {InterfaceFoldersCount}, Total InterfaceFiles = {InterfaceFilesCount}",
                   migrationSummery?.InterfaceFolders?.Count ?? 0,
                   migrationSummery?.InterfaceFiles?.Count ?? 0);

                _logger.LogInformation("Migration Summary: Total InterfaceCreatedFolders = {InterfaceFoldersCount}, Total InterfaceCreatedFiles = {InterfaceFilesCount}",
                   migrationSummery?.InterfaceFolders?.Where(i => !string.IsNullOrEmpty(i.ArtifactId))?.Count() ?? 0,
                   migrationSummery?.InterfaceFiles?.Where(i => !string.IsNullOrEmpty(i.ArtifactId)).Count() ?? 0);


                _logger.LogInformation("InterfaceMigrationFolderAndFileCommand Execute time: {st}ms", clock.Elapsed.TotalMilliseconds);

                await _notificationService.GetCommonSubscriptionNotification(
                           true,
                           command.NotificationSubscriptionId,
                           command.ActionName,
                           command.Context
                       );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            clock.Stop();
        }

        public async Task CreateInterfaceMigrationFolder(RiqsInterfaceMigrationSummary migrationSummery, Workspace workspace)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                migrationSummery.InterfaceFolders = PrepareInterfaceFolderSummaryHierarchy(migrationSummery.InterfaceFolders);
                var interfacrFolders = migrationSummery.InterfaceFolders;

                if (migrationSummery?.InterfaceFolders != null)
                {
                    var metaData = new Dictionary<string, MetaValuePair>();

                    var departmentKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];
                    var orgLevelKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL.ToString()];
                    var interfaceMigrationSummeryIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID.ToString()];

                    if (!string.IsNullOrEmpty(migrationSummery.ClientId))
                    {
                        metaData.Add(departmentKey, new MetaValuePair() { Type = "string", Value = migrationSummery.ClientId });
                    }
                    else if (!_securityHelperService.IsADepartmentLevelUser())
                    {
                        metaData.Add(orgLevelKey, new MetaValuePair() { Type = "string", Value = "1" });
                    }
                    metaData.Add(interfaceMigrationSummeryIdKey, new MetaValuePair() { Type = "string", Value = migrationSummery.ItemId });

                    var folderPayloads = new List<ObjectArtifactFolderCreateCommand>();
                    var folderDict  = new Dictionary<string, InterfaceFolder>();
                    var artifactDict = new Dictionary<string, ObjectArtifactFolderCreateCommand>();

                    interfacrFolders.ForEach(folder =>
                    {
                        if (!string.IsNullOrEmpty(folder.ArtifactId)) return;
                        var artifact = new ObjectArtifactFolderCreateCommand
                        {
                            ObjectArtifactId = Guid.NewGuid().ToString(),
                            ParentId = artifactDict.ContainsKey(folder.ParentId) ? artifactDict[folder.ParentId]?.ObjectArtifactId : null,
                            OrganizationId = migrationSummery.OrganizationId,
                            Description = string.Empty,
                            Name = folder.Name,
                            UserId = securityContext.UserId,
                            WorkspaceId = workspace.ItemId,
                            Secured = false,
                            Tags = new[] { "create_folder" },
                            IsPreventShareWithParentSharedUsers = true,
                            MetaData = metaData
                        };
                        folderPayloads.Add(artifact);

                        artifactDict[folder.FolderId] = artifact;
                        folderDict[artifact.ObjectArtifactId] = folder;
                    });

                    var successfulPayloads = await _dmsService.CreateFolders(folderPayloads);

                    foreach (var payload in successfulPayloads)
                    {
                        var folder = folderDict.ContainsKey(payload.ObjectArtifactId) ? folderDict[payload.ObjectArtifactId] : null;
                        if (folder != null)
                        {
                            folder.ArtifactId = payload.ObjectArtifactId;
                        }
                    }

                    await _repository.UpdateAsync(summery => summery.ItemId == migrationSummery.ItemId, migrationSummery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        public async Task UploadInterfaceMigrationFile(RiqsInterfaceMigrationSummary migrationSummary, Workspace workspace)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var metaData = PrepareMetaData(migrationSummary);

                if (migrationSummary?.InterfaceFiles == null || !migrationSummary.InterfaceFiles.Any())
                    return;

                var interfaceFolders = migrationSummary.InterfaceFolders ?? new List<InterfaceFolder>();
                var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();

                if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token))
                {
                    return;
                }

                var batches = BuildBatches(migrationSummary.InterfaceFiles);
                foreach (var batch in batches)
                {
                    var uploadTasks = batch.Select(file =>
                        ProcessAndUploadInterfacrFiles(file, interfaceFolders, migrationSummary, tokenInfo, workspace, securityContext)
                    ).ToList();

                    await Task.WhenAll(uploadTasks);
                    await Task.Delay(100);
                }

                await _repository.UpdateAsync(sum => sum.ItemId == migrationSummary.ItemId, migrationSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {Name}: {Message}, Trace: {StackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        private List<List<InterfaceFile>> BuildBatches(
            IEnumerable<InterfaceFile> files,
            long maxBatchBytes = 100 * 1024 * 1024,   // 100 MB
            int maxBatchFiles = 20)
        {
            var pq = new PriorityQueue<(long size, List<InterfaceFile> files), long>();

            foreach (var f in files)
                pq.Enqueue((f.FileSize, new List<InterfaceFile> { f }), f.FileSize);

            var completed = new List<List<InterfaceFile>>();

            while (pq.Count > 1)
            {
                var b1 = pq.Dequeue();          // smallest
                var b2 = pq.Dequeue();          // next‑smallest

                bool fitsBySize = b1.size + b2.size <= maxBatchBytes;
                bool fitsByCount = b1.files.Count + b2.files.Count <= maxBatchFiles;

                if (fitsBySize && fitsByCount)
                {
                    // merge
                    b1.files.AddRange(b2.files);
                    b1.size += b2.size;
                    pq.Enqueue(b1, b1.size);
                }
                else
                {
                    if (b1.files.Count >= b2.files.Count)
                    {
                        completed.Add(b1.files);
                        pq.Enqueue(b2, b2.size);
                    }
                    else
                    {
                        completed.Add(b2.files);
                        pq.Enqueue(b1, b1.size);
                    }
                }
            }

            if (pq.TryDequeue(out var last, out _))
                completed.Add(last.files);

            return completed;
        }

        private async Task ProcessAndUploadInterfacrFiles(InterfaceFile file,
            List<InterfaceFolder> interfaceFolders,
            RiqsInterfaceMigrationSummary migrationSummary,
            ExternalUserTokenResponse tokenInfo,
            Workspace workspace,
            SecurityContext securityContext)
        {
            if (!string.IsNullOrEmpty(file?.ArtifactId))
                return;

            var fileStream = await GetFileContentBytesAsync(migrationSummary.Provider, file, tokenInfo.access_token);
            if (fileStream == null)
                return;

            var parent = interfaceFolders.FirstOrDefault(folder => folder.FolderId == file.ParentId);
            if (parent != null && string.IsNullOrEmpty(parent.ArtifactId))
            {
                _logger.LogError("Invalid Parent ID for file {id}", file.FileId);
                return;
            }

            var payload = PrepareUploadPayload(migrationSummary, workspace, securityContext, parent, file);
            var uploadUrl = await _dmsService.UploadFile(payload, securityContext.OauthBearerToken);

            if (!string.IsNullOrEmpty(uploadUrl))
            {
                await _dmsFileUploadedEventHandlerHandlerService.HandleDmsFileUploadedEvent(payload);
            }

            var isUploaded = await _storageDataService.UploadFileToStorageByUrlAsync(uploadUrl, fileStream);

            if (!string.IsNullOrEmpty(uploadUrl) && isUploaded)
            {
                file.ArtifactId = payload.ObjectArtifactId;
                await _repository.UpdateAsync(sum => sum.ItemId == migrationSummary.ItemId, migrationSummary);
            }
        }

        private List<InterfaceFolder> PrepareInterfaceFolderSummaryHierarchy(List<InterfaceFolder> interfaceFolders)
        {
            var finalFolders = new List<InterfaceFolder>();
            interfaceFolders ??= new List<InterfaceFolder>();

            var folderIds = new HashSet<string>(interfaceFolders.Select(f => f.FolderId));

            var childLookup = interfaceFolders
                .Where(x => !string.IsNullOrEmpty(x.ParentId) && folderIds.Contains(x.ParentId))
                .GroupBy(folder => folder.ParentId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var folders = interfaceFolders.Where(folder => string.IsNullOrEmpty(folder.ParentId) || !childLookup.ContainsKey(folder.ParentId)).ToList();
            finalFolders.AddRange(folders);

            while (folders.Any())
            {
                folders = folders
                        .Where(folder => childLookup.ContainsKey(folder.FolderId))
                        .SelectMany(folder => childLookup[folder.FolderId])
                        .ToList();

                finalFolders.AddRange(folders);
            }

            return finalFolders;
        }

        private async Task<RiqsInterfaceMigrationSummary> GetRiqsInterfaceMigrationSummeryAsync(string interfaceMigrationSummeryId)
        {
            var result = await _repository.GetItemAsync<RiqsInterfaceMigrationSummary>(x => x.ItemId == interfaceMigrationSummeryId);
            return result;
        }

        private async Task<Workspace> GetWorkSpaceForCurrentUserId(string userId)
        {
            var workspace = await _repository.GetItemAsync<Workspace>(w => w.OwnerId == userId && w.Name == "My Workspace");

            try
            {
                if (workspace == null)
                {
                    var existingWorkspace = _repository.GetItems<Workspace>().FirstOrDefault();

                    var newWorkspace = new Workspace
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Name = "My Workspace",
                        Description = "My Workspace",
                        OwnerId = userId,
                        CreatedBy = userId,
                        IdsAllowedToRead = new[] { userId },
                        IdsAllowedToWrite = new[] { userId },
                        IsDefault = true,
                        IsShared = false,
                        StorageAreaId = existingWorkspace?.StorageAreaId,
                        TotalStorageSpace = 10000,
                        UsedStorageSpace = 0
                    };

                    await _repository.SaveAsync(newWorkspace);

                    return newWorkspace;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in GetWorkSpaceForCurrentUserId {ex.Message}");
            }

            return workspace;
        }

        private Dictionary<string, MetaValuePair> PrepareMetaData(RiqsInterfaceMigrationSummary migrationSummary)
        {
            var metaData = new Dictionary<string, MetaValuePair>
            {
                { LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS.ToString()],
                  new MetaValuePair { Type = "string", Value = ((int)LibraryFileApprovalStatusEnum.PENDING).ToString() } },

                { LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.STATUS.ToString()],
                  new MetaValuePair { Type = "string", Value = ((int)LibraryFileStatusEnum.INACTIVE).ToString() } },

                { LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB.ToString()],
                  new MetaValuePair { Type = "string", Value = "1" } },

                { LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID.ToString()],
                  new MetaValuePair { Type = "string", Value = migrationSummary.ItemId } },

                { LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.VERSION.ToString()],
                  new MetaValuePair { Type = "string", Value = _securityHelperService.IsADepartmentLevelUser() ? "0.01" : "1.00" } }
            };

            if (!string.IsNullOrEmpty(migrationSummary.ClientId))
            {
                metaData.Add(
                    LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()],
                    new MetaValuePair { Type = "string", Value = migrationSummary.ClientId }
                );
            }
            if (!_securityHelperService.IsADepartmentLevelUser())
            {
                var orgLevelKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL.ToString()];
                metaData.Add(orgLevelKey, new MetaValuePair() { Type = "string", Value = "1" });
            }

            return metaData;
        }

        private async Task<byte[]> GetFileContentBytesAsync(string provider, InterfaceFile file, string accessToken)
        {
            if (provider == "Google")
            {
                return await _googleDriveFileAndFolderInfoService.GetFileContentBytesAsync(file.FileId, Path.GetExtension(file.Name), accessToken);
            }

            if (provider == "Microsoft")
            {
                return await _sharePointFileAndFolderInfoService.GetFileContentBytesAsync(file.DownloadUrl);
            }

            return null;
        }

        private ObjectArtifactFileUploadCommand PrepareUploadPayload(
            RiqsInterfaceMigrationSummary migrationSummary,
            Workspace workspace,
            SecurityContext securityContext,
            InterfaceFolder parent,
            InterfaceFile file)
        {
            return new ObjectArtifactFileUploadCommand
            {
                FileStorageId = Guid.NewGuid().ToString(),
                Description = null,
                Tags = new[] { "upload" },
                ParentId = parent?.ArtifactId,
                FileName = file.Name,
                StorageAreaId = workspace.StorageAreaId,
                ObjectArtifactId = Guid.NewGuid().ToString(),
                WorkspaceId = workspace.ItemId,
                UserId = securityContext.UserId,
                OrganizationId = migrationSummary.OrganizationId,
                UseLicensing = true,
                FileSizeInBytes = Convert.ToInt32(file.FileSize),
                FeatureId = "praxis-license",
                IsPreventShareWithParentSharedUsers = true,
                MetaData = PrepareMetaData(migrationSummary),
                IsUploadFromInterface = true
            };
        }

    }
}
