using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.ESignature.Service.Events;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.Entities;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class LibraryFormService : ILibraryFormService
    {
        private readonly IRepository _repository;
        private readonly ILogger<LibraryFormService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly INotificationService _notificationService;
        private readonly IPraxisUserService _praxisUserService;
        private readonly IDmsService _dmsService;
        private readonly ISignatureService _signatureService;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ITokenService _tokenService;
        private readonly IObjectArtifactFilePermissionService _objectArtifactFilePermissionService;
        private readonly IServiceClient _serviceClient;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IObjectArtifactMappingService _objectArtifactMappingService;
        private readonly IGenericEventPublishService _genericEventPublishService;
        private readonly ILibraryFormAdoptFactoryService _libraryFormAdoptFactoryService;
        private readonly ICockpitFormDocumentActivityMetricsGenerationService _cockpitFormDocumentActivityMetricsGenerationService;
        private readonly ICockpitDocumentActivityMetricsGenerationService _cockpitDocumentActivityMetricsGenerationService;


        public LibraryFormService(
            IRepository repository,
            ILogger<LibraryFormService> logger,
            ISecurityContextProvider securityContextProvider,
            INotificationService notificationService,
            IDmsService dmsService,
            ISignatureService signatureService,
            IPraxisUserService praxisUserService,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ITokenService tokenService,
            IObjectArtifactFilePermissionService objectArtifactFilePermissionService,
            IServiceClient serviceClient,
            ISecurityHelperService securityHelperService,
            IObjectArtifactMappingService objectArtifactMappingService,
            IGenericEventPublishService genericEventPublishService,
            ILibraryFormAdoptFactoryService libraryFormAdoptFactoryService,
            ICockpitFormDocumentActivityMetricsGenerationService cockpitFormDocumentActivityMetricsGenerationService,
            ICockpitDocumentActivityMetricsGenerationService cockpitDocumentActivityMetricsGenerationService
        )
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _notificationService = notificationService;
            _dmsService = dmsService;
            _signatureService = signatureService;
            _praxisUserService = praxisUserService;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _tokenService = tokenService;
            _objectArtifactFilePermissionService = objectArtifactFilePermissionService;
            _serviceClient = serviceClient;
            _securityHelperService = securityHelperService;
            _objectArtifactMappingService = objectArtifactMappingService;
            _genericEventPublishService = genericEventPublishService;
            _libraryFormAdoptFactoryService = libraryFormAdoptFactoryService;
            _cockpitFormDocumentActivityMetricsGenerationService = cockpitFormDocumentActivityMetricsGenerationService;
            _cockpitDocumentActivityMetricsGenerationService = cockpitDocumentActivityMetricsGenerationService;
        }

        public async Task LibraryFormClone(LibraryFormCloneCommand command)
        {
            try
            {
                var objectArtifact = await GetFormObjectArtifactById(command.ParentObjectArtifactId);

                var isStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE.ToString()];
                var isStandardFile = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifact.MetaData, isStandardFileKey) == ((int)LibraryBooleanEnum.TRUE).ToString();

                await CreateNewObjectArtifact(command, isStandardFile);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in CloneFile: {ExMessage} -> {ExStackTrace}", ex.Message, ex.StackTrace);
            }
        }

        public async Task<LibraryFormMappingRecord> GetFormCloneMappingRecord(string objectArtifactId)
        {
            try
            {
                var objectArtifactData = await GetCloneFromObjectArtifact(objectArtifactId);
                if (objectArtifactData == null) return null;

                return new LibraryFormMappingRecord()
                {
                    ItemId = objectArtifactData.ItemId,
                    Name = objectArtifactData.Name,
                    FileStorageId = objectArtifactData.FileStorageId,
                    MetaData = objectArtifactData.MetaData,
                };
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in GetFormCloneMappingRecord: ObjectArtifactId  {objectArtifactId} {ExMessage} -> {ExStackTrace}", objectArtifactId, e.Message, e.StackTrace);
                return null;
            }
        }

        public async Task UpdateForm(LibraryFormUpdateCommand command)
        {
            var objectArtifact = await GetFormObjectArtifactById(command.ObjectArtifactId);
            var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE.ToString()];
            var isChildStandardFile = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifact.MetaData, isChildStandardFileKey) == ((int)LibraryBooleanEnum.TRUE).ToString();

            var objectArtifactData = await GetUserCloneFromObjectArtifact(command.ObjectArtifactId);

            if (objectArtifactData == null)
            {
                throw new FileNotFoundException("ObjectArtifact not found for id {Id}", command.ObjectArtifactId);
            }

            var statusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];

            /*
            var esignatureRequireStatusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.IS_ESIGN_REQUIRED.ToString()];
            var twfaEnableStatusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.IS_2FA_ENABLED.ToString()];
            */

            try
            {
                var status = GetFormStatus(objectArtifactData, command.IsDraft);
                var securityContext = _securityContextProvider.GetSecurityContext();
                objectArtifactData.MetaData[statusKey].Value = status;
                objectArtifactData.LastUpdatedBy = securityContext.UserId;

                await UpdateArtifact(objectArtifactData);

                // generate signing url before going to singing window only 2fa form
                /*
                 var twfaEnableStatus = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData.MetaData, twfaEnableStatusKey);
                 var esignatureRequireStatus = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData.MetaData, esignatureRequireStatusKey);
                 if (
                     command.IsDraft == false && 
                     twfaEnableStatus == ((int)LibraryBooleanEnum.TRUE).ToString() &&
                     esignatureRequireStatus == ((int)LibraryBooleanEnum.TRUE).ToString()
                 )
                 {
                     _ = GenerateSignatureUrl(objectArtifactData.ItemId);
                 }*/

                PublishLibraryFormUpdateEvent(objectArtifactData);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in UpdateForm: {ExMessage} -> {ExStackTrace}", e.Message,
                    e.StackTrace);
            }
        }

        public async Task<bool> UpdateFormSignatureUrl(ExternalContractProcessedEvent @event)
        {
            var objectArtifactData = await GetFormObjectArtifactById(@event.Response.TrackingId);
            if (objectArtifactData == null)
            {
                _logger.LogError("ObjectArtifact not found for id {Id}", @event.Response.TrackingId);
                return false;
            }

            // create CreateImpersonateContext
            var praxisUser = await GetPraxisUserQuery(objectArtifactData.OwnerId);

            _tokenService.CreateImpersonateContext(
                _securityContextProvider.GetSecurityContext(),
                praxisUser.Email,
                praxisUser.UserId,
                praxisUser.Roles.ToList()
            );
            var denormalizePayload = JsonConvert.SerializeObject(new
            {
                Url = @event.Response.AccessLink
            });

            await _notificationService.LibraryFromUpdateNotification
                (true, @event.Response.TrackingId, "LibraryFormSignUrl", denormalizePayload);

            await UpdateArtifact(objectArtifactData);

            var expiredDate = GetUrlExpiredTime();
            await CreateFormSignatureMapping(new FormSignatureMapping()
            {
                ItemId = Guid.NewGuid().ToString(),
                ObjectArtifactId = objectArtifactData.ItemId,
                DocumentId = @event.Response.DocumentId,
                Url = @event.Response.AccessLink,
                Expired = expiredDate
            });

            _logger.LogInformation("Signature url updated with : {Payload}",
                @event.Response.AccessLink);

            return true;
        }

        private async Task<PraxisUser> GetPraxisUserQuery(string userId)
        {
            Expression<Func<PraxisUser, bool>> filter = pu => pu.UserId == userId;
            return await _repository.GetItemAsync(PraxisConstants.PraxisTenant, filter);
        }

        public async Task<bool> CompleteFormSignature(ExternalContractSentAndSignedEvent @event)
        {
            //get signature mapping with documentId
            var mapping = await GetFormSignatureMappingByDocumentId(@event.DocumentId);

            if (mapping == null)
            {
                _logger.LogError("Signature mapping not found for document id {Id}", @event.DocumentId);
                return false;
            }

            var objectArtifactData = await GetFormObjectArtifactById(mapping.ObjectArtifactId);
            if (objectArtifactData == null)
            {
                _logger.LogError("ObjectArtifact not found for id {Id}", mapping.ObjectArtifactId);
                return false;
            }

            // create CreateImpersonateContext
            var praxisUser = await GetPraxisUserQuery(objectArtifactData.OwnerId);
            _tokenService.CreateImpersonateContext(
                _securityContextProvider.GetSecurityContext(),
                praxisUser.Email,
                praxisUser.UserId,
                praxisUser.Roles.ToList()
            );

            var denormalizePayload = JsonConvert.SerializeObject(new
            {
                FileId = objectArtifactData.ItemId
            });

            await _notificationService.LibraryFromUpdateNotification
                (true, objectArtifactData.ItemId, "LibraryFormSignComplete", denormalizePayload);

            _logger.LogInformation("Signature sign Event : {Payload}",
                JsonConvert.SerializeObject(@event));

            //get signed file and store it to local tenant with current fileId
            var success = await _signatureService.StoreSignedFileToLocal(objectArtifactData.FileStorageId,
                @event.FileMaps?[0].SignedFileId);
            if (!success)
            {
                return false;
            }

            try
            {
                var statusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    $"{ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS}"];
                objectArtifactData.MetaData[statusKey].Value = ((int)FormFillStatus.COMPLETE).ToString();
                await UpdateArtifactWithEvent(objectArtifactData);

                //delete the signature mapping object
                await DeleteFormSignatureMapping(mapping.DocumentId);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in CompleteFormSignature: {Message} -> {StackTrace}", e.Message,
                    e.StackTrace);
            }

            return false;
        }

        public async Task<FormSignatureMapping> GetFormSignatureMapping(string objectArtifactId)
        {
            try
            {
                var mapping = await GetFormSignatureMappingByArtifactId(objectArtifactId);
                if (mapping == null) return null;

                //check signature is expired or not
                var utcCurrentDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
                if (utcCurrentDate <= mapping.Expired) return mapping;

                await DeleteFormSignatureMapping(mapping.DocumentId);

                return new FormSignatureMapping()
                {
                    IsLinkedExpired = true
                };
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in GetFormSignatureMapping: ObjectArtifactId {objectArtifactId}, {m} -> {s}", objectArtifactId, e.Message, e.StackTrace);
                return null;
            }
        }

        public async Task GenerateSignatureUrl(string objectArtifactId)
        {
            try
            {
                var objectArtifactData = await GetUserCloneFromObjectArtifact(objectArtifactId);

                var statusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];
                var eSignatureRequireStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_ESIGN_REQUIRED.ToString()];

                var eSignatureRequireStatus = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData?.MetaData, eSignatureRequireStatusKey);
                if (objectArtifactData == null || objectArtifactData.MetaData[statusKey].Value.Equals(((int)FormFillStatus.COMPLETE).ToString()) ||
                      eSignatureRequireStatus != ((int)LibraryBooleanEnum.TRUE).ToString()) return;


                if (await IsSignatureUrlAlreadyExist(objectArtifactId)) return;

                await CreateSignatureRequest(objectArtifactData);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in GenerateSignatureUrl:  ObjectArtifactId {objectArtifactId}, {m} -> {s}", objectArtifactId, e.Message, e.StackTrace);
            }
        }

        private async Task<bool> IsSignatureUrlAlreadyExist(string objectArtifactId)
        {
            Expression<Func<FormSignatureMapping, bool>> filter =
                o => o.ObjectArtifactId.Equals(objectArtifactId);

            var signatureMapping = await _repository.GetItemAsync(PraxisConstants.PraxisTenant, filter);
            if (signatureMapping == null) return false;

            var utcCurrentDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            if (utcCurrentDate <= signatureMapping.Expired) return true;

            await DeleteFormSignatureMapping(signatureMapping.DocumentId);
            return false;
        }

        public Task<ObjectArtifact> GetFormObjectArtifactById(string objectArtifactId)
        {
            Expression<Func<ObjectArtifact, bool>> filter =
                o => o.ItemId == objectArtifactId;

            return _repository.GetItemAsync(PraxisConstants.PraxisTenant, filter);
        }

        private async Task<ObjectArtifact> GetCloneFromObjectArtifact(string parentObjectArtifactId)
        {
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"];
            var securityContext = _securityContextProvider.GetSecurityContext();

            var formTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            var formFillStatusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];
            var isAOriginalArtifactKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                nameof(ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT)];

            Expression<Func<ObjectArtifact, bool>> filter =
                o =>
                    o.CreatedBy == securityContext.UserId
                    && o.MetaData[originalArtifactIdKey].Value == parentObjectArtifactId
                    && (!o.MetaData.ContainsKey(isAOriginalArtifactKey) ||
                        o.MetaData[isAOriginalArtifactKey] == null ||
                        o.MetaData[isAOriginalArtifactKey].Value != ((int)LibraryBooleanEnum.TRUE).ToString())
                    && o.MetaData[formTypeKey].Value == ((int)LibraryFileTypeEnum.FORM).ToString()
                    && (o.MetaData[formFillStatusKey].Value == ((int)FormFillStatus.DRAFT).ToString() ||
                        o.MetaData[formFillStatusKey].Value == ((int)FormFillStatus.PENDING_SIGNATURE).ToString())
                    && !o.IsMarkedToDelete;

            return await _repository
                .GetItemAsync(filter);
        }

        private async Task<ObjectArtifact> GetUserCloneFromObjectArtifact(string objectArtifactId, bool isChildStandardFile = false)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            var formTypeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.FILE_TYPE}"];
            var originalArtifactKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT}"];
            var formFillStatusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];
            var formFillStatusValues = new List<string>
            {
                ((int)FormFillStatus.DRAFT).ToString(),
                ((int)FormFillStatus.PENDING_SIGNATURE).ToString()
            };
            if (isChildStandardFile)
            {
                formFillStatusValues.Add(((int)FormFillStatus.COMPLETE).ToString());
            }

            Expression<Func<ObjectArtifact, bool>> filter =
                o =>
                    o.ItemId == objectArtifactId
                    && o.CreatedBy == securityContext.UserId
                    && o.MetaData != null
                    && o.MetaData[formTypeKey].Value == ((int)LibraryFileTypeEnum.FORM).ToString()
                    && (!o.MetaData.ContainsKey(originalArtifactKey) ||
                        o.MetaData[originalArtifactKey] == null ||
                        o.MetaData[originalArtifactKey].Value != ((int)LibraryBooleanEnum.TRUE).ToString())
                    && formFillStatusValues.Contains(o.MetaData[formFillStatusKey].Value)
                    && !o.IsMarkedToDelete;

            return await _repository
                .GetItemAsync(filter);
        }

        private async Task UpdateArtifact(ObjectArtifact objectArtifactData)
        {
            objectArtifactData.LastUpdateDate = DateTime.Now.ToLocalTime();

            await _repository.UpdateAsync(o => o.ItemId.Equals(objectArtifactData.ItemId), PraxisConstants.PraxisTenant,
                objectArtifactData);
        }

        public async Task UpdateArtifactWithEvent(ObjectArtifact objectArtifactData)
        {
            objectArtifactData.LastUpdateDate = DateTime.Now.ToLocalTime();

            var statusKey =
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];

            await AdoptLibraryFormForOtherModule(objectArtifactData);

            //when status complete add activity summary
            if (objectArtifactData.MetaData[statusKey].Value.Equals(((int)FormFillStatus.COMPLETE).ToString()))
            {
                objectArtifactData.ActivitySummary = PrepareActivitySummary(objectArtifactData.ActivitySummary);
                var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"];
                await AddParentActivitySummary(objectArtifactData.MetaData[originalArtifactIdKey].Value, objectArtifactData.OwnerId);
                await SaveFormCompletionSummaryInRiqsObjectArtifactMapping(objectArtifactData);
                await SaveFormCompletionSummaryInRiqsObjectArtifactMapping(objectArtifactData, objectArtifactData.MetaData[originalArtifactIdKey].Value);
            }

            await _repository.UpdateAsync(o => o.ItemId.Equals(objectArtifactData.ItemId), PraxisConstants.PraxisTenant,
                objectArtifactData);

            //update file permission
            if (objectArtifactData.MetaData[statusKey].Value.Equals(((int)FormFillStatus.COMPLETE).ToString()))
            {
                await _objectArtifactFilePermissionService.SetObjectArtifactFilePermissions(
                    objectArtifactData,
                    ObjectArtifactEvent.FORM_RESPONSE_SAVED
                );
            }

            await OnFormFillInitiateCockpitActivityMetricsGeneration(objectArtifactData);
        }

        private async Task CreateSignatureRequest(ObjectArtifact objectArtifactData)
        {
            var currentUser = _praxisUserService
                .GetPraxisUserByUserId(_securityContextProvider.GetSecurityContext().UserId);

            if (currentUser == null)
                return;

            var payload = new SignatureRequestCommand()
            {
                TrackingId = objectArtifactData.ItemId,
                Title = objectArtifactData.Name,
                ReceiveRolloutEmail = false,
                SignatureClass = 0,
                FileIds = new List<string>
                {
                    objectArtifactData.FileStorageId
                },
                AddSignatoryCommands = new List<AddSignatoryCommand>
                {
                    new AddSignatoryCommand
                    {
                        Email = currentUser.Email,
                        FirstName = currentUser.FirstName,
                        LastName = currentUser.LastName,
                        ContractRole = 0,
                    }
                }
            };
            var success = await _signatureService.CreateSignatureRequest(payload);
            if (!success)
            {
                await _signatureService.CreateSignatureRequest(payload);
            }
        }

        private static string GetFormStatus(ObjectArtifact objectArtifactData, bool isDraft)
        {
            var status = ((int)FormFillStatus.DRAFT).ToString();

            if (isDraft)
            {
                return status;
            }

            status = ((int)FormFillStatus.COMPLETE).ToString();

            //check if the signature required
            var signKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                $"{ObjectArtifactMetaDataKeyEnum.IS_ESIGN_REQUIRED}"];

            var signValue = objectArtifactData.MetaData[signKey].Value;
            //signature not required return the stats
            if (!int.TryParse(signValue, out var i)
                || LibraryBooleanEnum.TRUE != (LibraryBooleanEnum)i
               )
            {
                return status;
            }

            //signature required set the status to Pending Signature
            status = ((int)FormFillStatus.PENDING_SIGNATURE).ToString();

            return status;
        }

        private async Task CreateNewObjectArtifact(LibraryFormCloneCommand command, bool isStandardFile = false)
        {
            var objectArtifactData = await GetFormObjectArtifactById(command.ParentObjectArtifactId);
            var securityContext = _securityContextProvider.GetSecurityContext();
            var metaData = objectArtifactData.MetaData;

            var entityName = _objectArtifactUtilityService.GetMetaDataValueByKey(command.MetaData, "EntityName");

            _logger.LogInformation("Method: {CreateNewObjectArtifact}, EntityName: {EntityName}", nameof(CreateNewObjectArtifact), entityName);

            if (isStandardFile)
            {
                isStandardFile = entityName != null && entityName != nameof(ObjectArtifact) ? false : true;
            }

            var assignedOnKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON.ToString()];
            var originalArtifactIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID.ToString()];
            var isUploadedFromWebKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_UPLOADED_FROM_WEB.ToString()];
            var isUsedInAnotherEntityKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_USED_IN_ANOTHER_ENTITY.ToString()];
            var artifactUsageReferenceCounterKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.ARTIFACT_USAGE_REFERENCE_COUNTER.ToString()];
            var formFillStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS.ToString()];
            var isAOriginalArtifactKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT.ToString()];
            var isStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_STANDARD_FILE.ToString()];
            var isChildStandardFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_CHILD_STANDARD_FILE.ToString()];
            var fileStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.STATUS.ToString()];
            var approvalStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS.ToString()];
            var departmentIdKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];
            var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.VERSION.ToString()];
            var isNotifiedToCockpitKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_NOTIFIED_TO_COCKPIT.ToString()];
            var isOrgLevelKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                ObjectArtifactMetaDataKeyEnum.IS_ORG_LEVEL.ToString()];

            var removeKeys = new List<string>
            {
                assignedOnKey,
                originalArtifactIdKey,
                isUploadedFromWebKey,
                isUsedInAnotherEntityKey,
                artifactUsageReferenceCounterKey,
                isStandardFileKey,
                isChildStandardFileKey,
                isNotifiedToCockpitKey,
                isOrgLevelKey,
                LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.INTERFACE_MIGRATION_SUMMARY_ID.ToString()]
            };

            foreach (var removeKey in removeKeys.Where(removeKey => metaData.ContainsKey(removeKey)))
            {
                metaData.Remove(removeKey);
            }

            var originalArtifactMetaData = new MetaValuePair
            { Type = "string", Value = command.ParentObjectArtifactId };

            var statusMetaData = new MetaValuePair
            { Type = "string", Value = ((int)FormFillStatus.DRAFT).ToString() };

            var isAOriginalArtifactMetaData = new MetaValuePair
            {
                Type = "string",
                Value = isStandardFile ? ((int)LibraryBooleanEnum.TRUE).ToString() : ((int)LibraryBooleanEnum.FALSE).ToString()
            };
            var addKeys = new List<(string, MetaValuePair)>()
            {
                (formFillStatusKey, statusMetaData),
                (isAOriginalArtifactKey, isAOriginalArtifactMetaData),
                (originalArtifactIdKey, originalArtifactMetaData)
            };

            if (entityName != null && entityName != nameof(ObjectArtifact))
            {
                var isUsedInAnotherEntityMetaData = new MetaValuePair
                {
                    Type = "string",
                    Value = ((int)LibraryBooleanEnum.TRUE).ToString()
                };
                var artifactUsageReferenceCounterMetaData = new MetaValuePair
                { Type = "string", Value = "1" };
                addKeys.Add((isUsedInAnotherEntityKey, isUsedInAnotherEntityMetaData));
                addKeys.Add((artifactUsageReferenceCounterKey, artifactUsageReferenceCounterMetaData));
            }

            foreach (var addKey in addKeys)
            {
                if (metaData.TryGetValue(addKey.Item1, out var value1))
                {
                    value1.Value = addKey.Item2.Value;
                }
                else
                {
                    metaData.Add(addKey.Item1, addKey.Item2);
                }
            }

            //add additional metadata provided for process guid
            if (command.MetaData != null)
            {
                foreach (var (key, value) in command.MetaData)
                {
                    if (metaData.TryGetValue(key, out var value1))
                    {
                        value1.Value = value.Value;
                    }
                    else
                    {
                        metaData.Add(new KeyValuePair<string, MetaValuePair>(key, value));
                    }
                }
            }

            metaData[fileStatusKey].Value = ((int)LibraryFileStatusEnum.ACTIVE).ToString();
            metaData[approvalStatusKey].Value = ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString();
            bool isOrgLevel = false;
            if (isStandardFile)
            {
                var isChildStandardFileValue = new MetaValuePair
                { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() };
                metaData[isChildStandardFileKey] = isChildStandardFileValue;
                var parentVersion = _objectArtifactUtilityService.GetMetaDataValueByKey(objectArtifactData?.MetaData, versionKey);
                metaData[versionKey] = new MetaValuePair
                { Type = "string", Value = _dmsService.GenerateVersionFromParentObjectArtifact(parentVersion) };
                
                if (_objectArtifactUtilityService.IsAOrgLevelArtifact(metaData, ArtifactTypeEnum.File))
                {
                    metaData[isOrgLevelKey] = new MetaValuePair { Type = "string", Value = ((int)LibraryBooleanEnum.TRUE).ToString() };
                    isOrgLevel = true;
                }
            }

            //add department id for non admin b user
            if (_securityHelperService.IsADepartmentLevelUser() && !isOrgLevel)
            {
                var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

                if (metaData.TryGetValue(departmentIdKey, out var value1))
                {
                    value1.Value = departmentId;
                }
                else
                {
                    metaData.Add(new KeyValuePair<string, MetaValuePair>(departmentIdKey, new MetaValuePair
                    {
                        Type = "string",
                        Value = departmentId
                    }));
                }
            }

            //create new object artifact
            var dmsUploadPayload = new ObjectArtifactFileUploadCommand
            {
                FileStorageId = command.FileStorageId,
                Description = objectArtifactData.Description,
                FileName = isStandardFile ? objectArtifactData.Name : GetChildFormName(objectArtifactData.Name, securityContext.DisplayName),
                Tags = objectArtifactData.Tags,
                ParentId = objectArtifactData.ParentId,
                StorageAreaId = objectArtifactData.StorageAreaId,
                ObjectArtifactId = command.NewObjectArtifactId,
                CorrelationId = command.SubscriptionId,
                WorkspaceId = command.WorkspaceId,
                UserId = securityContext.UserId,
                OrganizationId = objectArtifactData.OrganizationId,
                UseLicensing = command.UseLicensing,
                FileSizeInBytes = (int)objectArtifactData.FileSizeInByte,
                IsPreventShareWithParentSharedUsers = true,
                FeatureId = "praxis-license",
                MetaData = metaData,
            };

            await _dmsService.UploadFile(dmsUploadPayload, securityContext.OauthBearerToken);
            if (entityName != null && entityName != nameof(ObjectArtifact))
            {
                PublishEventForArtifactUsageReference(dmsUploadPayload);
            }
        }

        private async Task<FormSignatureMapping> GetFormSignatureMappingByArtifactId(string artifactId)
        {
            Expression<Func<FormSignatureMapping, bool>> filter =
                o => o.ObjectArtifactId == artifactId;

            return await _repository
                .GetItemAsync(PraxisConstants.PraxisTenant, filter);
        }

        private async Task<FormSignatureMapping> GetFormSignatureMappingByDocumentId(string documentId)
        {
            Expression<Func<FormSignatureMapping, bool>> filter =
                o => o.DocumentId == documentId;

            return await _repository
                .GetItemAsync(PraxisConstants.PraxisTenant, filter);
        }

        private async Task DeleteFormSignatureMapping(string documentId)
        {
            var collection = _ecapMongoDbDataContextProvider
                .GetTenantDataContext(PraxisConstants.PraxisTenant)
                .GetCollection<FormSignatureMapping>($"{nameof(FormSignatureMapping)}s");
            await collection.DeleteOneAsync(x => x.DocumentId == documentId);
        }

        private async Task CreateFormSignatureMapping(FormSignatureMapping signatureMapping)
        {
            //check if exist
            var exist =
                await _repository.GetItemAsync<FormSignatureMapping>(
                    PraxisConstants.PraxisTenant,
                    o => o.DocumentId.Equals(signatureMapping.DocumentId)
                );

            if (exist == null)
            {
                await _repository.SaveAsync(PraxisConstants.PraxisTenant,
                    signatureMapping);
            }
        }

        private static DateTime GetUrlExpiredTime()
        {
            //add 7 Days as expiry time
            var resultTime = DateTime.Now.ToLocalTime().AddDays(7);

            return resultTime;
        }

        private static string GetChildFormName(string fileName, string userName)
        {
            const string fileType = ".pdf";
            var lastDotIndex = fileName.LastIndexOf(fileType, StringComparison.Ordinal);

            if (lastDotIndex == -1) return fileName;

            var newName = fileName[..lastDotIndex];

            return newName + "_" + userName + fileType;
        }

        private async Task AdoptLibraryFormForOtherModule(ObjectArtifact objectArtifactData)
        {
            await _libraryFormAdoptFactoryService.AdoptLibraryFormResponse(objectArtifactData);
        }

        private List<ActivitySummaryModel> PrepareActivitySummary(List<ActivitySummaryModel> currentActivitySummary)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var praxisUserId = _objectArtifactUtilityService.GetPraxisUsersByUserIds(new[] { userId })?.FirstOrDefault()?.ItemId;
            var activitySummary = !string.IsNullOrWhiteSpace(praxisUserId) ?
                new List<ActivitySummaryModel>
                {
                    new ActivitySummaryModel
                    {
                        ActivityName = ((int)ArtifactActivityName.FORM_RESPONSE_COMPLETED).ToString(),
                        ActivityPerformerModel = new List<ActivityPerformerModel>
                        {
                            new ActivityPerformerModel { PerformedBy = praxisUserId, PerformedOn = DateTime.Now.ToLocalTime()}
                        }
                    }
                } : currentActivitySummary;
            return activitySummary;
        }

        private async Task AddParentActivitySummary(string parentArtifactId, string ownerId)
        {
            //get parent artifact
            var artifact = await GetFormObjectArtifactById(parentArtifactId);

            var activitySummary = artifact.ActivitySummary;
            activitySummary ??= new List<ActivitySummaryModel>();

            var currentUser = await GetPraxisUserQuery(ownerId);

            if (currentUser == null)
                return;

            //check activity exist for form
            var formSummary = activitySummary
                .Find(s => s.ActivityName.Equals(((int)ArtifactActivityName.FORM_RESPONSE_COMPLETED).ToString()));

            if (formSummary == null)
            {
                formSummary = new ActivitySummaryModel
                {
                    ActivityName = ((int)ArtifactActivityName.FORM_RESPONSE_COMPLETED).ToString(),
                    ActivityPerformerModel = new List<ActivityPerformerModel>()
                };
                activitySummary.Add(formSummary);
            }

            var performerModel = formSummary.ActivityPerformerModel
                .Find(m => m.PerformedBy.Equals(currentUser.ItemId));

            if (performerModel == null)
            {
                formSummary.ActivityPerformerModel.Add(new ActivityPerformerModel
                {
                    PerformedBy = currentUser.ItemId,
                    PerformedOn = DateTime.Now.ToLocalTime()
                });
            }

            artifact.ActivitySummary = activitySummary;

            await UpdateArtifact(artifact);
        }

        private async Task SaveFormCompletionSummaryInRiqsObjectArtifactMapping(ObjectArtifact artifact, string parentArtifactId = null)
        {
            var activitySummary = GetRiqsActivitySummary(artifact);
            if (activitySummary != null)
            {
                var objectArtifact = _objectArtifactUtilityService.GetObjectArtifactById(
                    !string.IsNullOrWhiteSpace(parentArtifactId) ? parentArtifactId : artifact.ItemId);
                var riqsObjectArtifact = RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(
                    !string.IsNullOrWhiteSpace(parentArtifactId) ? parentArtifactId : artifact.ItemId);

                var isUpdate = !string.IsNullOrEmpty(riqsObjectArtifact?.ItemId);
                if (!isUpdate)
                {
                    riqsObjectArtifact = CreateRiqsObjectArtifactMappingPayload(objectArtifact, new List<RiqsActivitySummaryModel> { activitySummary });
                }
                else
                {
                    riqsObjectArtifact.FormCompletionSummary ??= new List<RiqsActivitySummaryModel>();
                    if (riqsObjectArtifact.FormCompletionSummary.FirstOrDefault(a => a.FilledFormId == artifact.ItemId) == null)
                    {
                        riqsObjectArtifact.FormCompletionSummary.Add(activitySummary);
                    }
                }

                await _objectArtifactMappingService.CreateOrUpdateRiqsObjectArtifactMapping(riqsObjectArtifact, isUpdate);
                RiqsObjectArtifactMappingConstant.ResetRiqsArtifactMappingData(riqsObjectArtifact);
            }
        }

        private RiqsActivitySummaryModel GetRiqsActivitySummary(ObjectArtifact artifact)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var currentUser = _objectArtifactUtilityService.GetPraxisUsersByUserIds(new[] { userId })?.FirstOrDefault();
            var activitySummary = currentUser != null ?
                new RiqsActivitySummaryModel
                {
                    OrganizationId = _securityHelperService.IsADepartmentLevelUser() ?
                        _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() :
                        artifact.OrganizationId,
                    PerformedBy = currentUser.ItemId,
                    PerformedOn = DateTime.Now.ToLocalTime(),
                    FilledFormId = artifact.ItemId
                } : null;
            return activitySummary;
        }

        private RiqsObjectArtifactMapping CreateRiqsObjectArtifactMappingPayload(ObjectArtifact artifact, List<RiqsActivitySummaryModel> activitySummary)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var mappingData = new RiqsObjectArtifactMapping()
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext.UserId,
                ObjectArtifactId = artifact.ItemId,
                OrganizationId = artifact.OrganizationId,
                FormCompletionSummary = activitySummary
            };
            return mappingData;
        }

        private void PublishLibraryFormUpdateEvent(ObjectArtifact artifact)
        {
            // Thread.Sleep(TimeSpan.FromSeconds(15));

            var libraryFormUpdateEvent = new GenericEvent
            {
                EventType = PraxisEventType.LibraryFormUpdateEvent,
                JsonPayload = JsonConvert.SerializeObject(artifact.ItemId)
            };

            _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), libraryFormUpdateEvent);
        }

        private void PublishEventForArtifactUsageReference(DmsFileUploadPayload dmsUploadPayload)
        {
            var entityName = _objectArtifactUtilityService.GetMetaDataValueByKey(dmsUploadPayload.MetaData, "EntityName");
            var entityId = _objectArtifactUtilityService.GetMetaDataValueByKey(dmsUploadPayload.MetaData, "EntityId");

            var artifactUsageReferenceEvent = new DmsArtifactUsageReferenceEventModel
            {
                Title = dmsUploadPayload.FileName,
                ObjectArtifactIds = new List<string> { dmsUploadPayload.ObjectArtifactId },
                RelatedEntityName = entityName,
                RelatedEntityId = entityId,
                PurposeEntityName = entityName,
                OrganizationId = dmsUploadPayload.OrganizationId,
                OrganizationIds = new List<string> { dmsUploadPayload.OrganizationId }
            };
            _genericEventPublishService.SendDmsArtifactUsageReferenceCreateEventToQueue(artifactUsageReferenceEvent);
        }

        public async Task CreateStandardLibraryForm(CreateStandardLibraryFormCommand command)
        {
            try
            {
                var objectArtifactData = await GetFormObjectArtifactById(command.ParentObjectArtifactId);
                var securityContext = _securityContextProvider.GetSecurityContext();
                var metaData = objectArtifactData.MetaData;

                var removeKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.ASSIGNED_ON.ToString()];
                if (metaData.ContainsKey(removeKey))
                {
                    metaData.Remove(removeKey);
                }

                var originalArtifactMetaData = new MetaValuePair
                { Type = "string", Value = command.ParentObjectArtifactId };
                metaData.Add(LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    $"{ObjectArtifactMetaDataKeyEnum.ORIGINAL_ARTIFACT_ID}"], originalArtifactMetaData);

                var statusMetaData = new MetaValuePair
                { Type = "string", Value = ((int)FormFillStatus.DRAFT).ToString() };
                metaData.Add(LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                        $"{ObjectArtifactMetaDataKeyEnum.FORM_FILL_STATUS}"],
                    statusMetaData);

                var originalFileKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    $"{ObjectArtifactMetaDataKeyEnum.IS_A_ORIGINAL_ARTIFACT}"];
                metaData[originalFileKey].Value =
                    ((int)LibraryBooleanEnum.FALSE).ToString();

                //add additional metadata provided for process guid
                if (command.MetaData != null)
                {
                    foreach (var (key, value) in command.MetaData)
                    {
                        if (metaData.TryGetValue(key, out var value1))
                        {
                            value1.Value = value.Value;
                        }
                        else
                        {
                            metaData.Add(new KeyValuePair<string, MetaValuePair>(key, value));
                        }
                    }
                }

                //add approval and active metadata for standard form
                var fileStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    $"{ObjectArtifactMetaDataKeyEnum.STATUS}"];
                var approvalStatusKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[
                    ObjectArtifactMetaDataKeyEnum.APPROVAL_STATUS.ToString()];

                metaData[fileStatusKey].Value = ((int)LibraryFileStatusEnum.ACTIVE).ToString();
                metaData[approvalStatusKey].Value = ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString();


                if (_securityHelperService.IsADepartmentLevelUser())
                {
                    var departmentKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.DEPARTMENT_ID.ToString()];
                    var departmentId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();

                    if (metaData.TryGetValue(departmentKey, out var value1))
                    {
                        value1.Value = departmentId;
                    }
                    else
                    {
                        metaData.Add(new KeyValuePair<string, MetaValuePair>(departmentKey, new MetaValuePair()
                        {
                            Type = "string",
                            Value = departmentId
                        }));
                    }
                }

                //create new object artifact
                var dmsUploadPayload = new ObjectArtifactFileUploadCommand()
                {
                    FileStorageId = command.FileStorageId,
                    Description = objectArtifactData.Description,
                    FileName = command.FileName ?? objectArtifactData.Name,
                    Tags = objectArtifactData.Tags,
                    ParentId = null,
                    StorageAreaId = objectArtifactData.StorageAreaId,
                    ObjectArtifactId = command.NewObjectArtifactId,
                    CorrelationId = command.SubscriptionId,
                    WorkspaceId = command.WorkspaceId,
                    UserId = securityContext.UserId,
                    OrganizationId = objectArtifactData.OrganizationId,
                    UseLicensing = command.UseLicensing,
                    FileSizeInBytes = (int)objectArtifactData.FileSizeInByte,
                    FeatureId = "praxis-license",
                    MetaData = metaData,
                };

                await _dmsService.UploadFile(dmsUploadPayload, securityContext.OauthBearerToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in CloneFile: {ExMessage} -> {ExStackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private LibraryFormUpdatedEventPayload GetFormUpdatedEventPayload(ObjectArtifact artifact)
        {
            _logger.LogInformation("Entered into Method: {MethodName} with Artifact: {Artifact}", nameof(GetFormUpdatedEventPayload), JsonConvert.SerializeObject(artifact));

            var entityName = _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, "EntityName");
            var entityId = _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, "EntityId");
            var clientId = _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, "DepartmentId") 
                           ?? _objectArtifactUtilityService.GetMetaDataValueByKey(artifact.MetaData, "ClientId");

            _logger.LogInformation("EntityName: {EntityName}, EntityId: {EntityId}, ClientId: {ClientId}", entityName, entityId, clientId);

            var eventPayload = new LibraryFormUpdatedEventPayload
            {
                ArtifactId = artifact.ItemId,
                RelatedEntityName = entityName,
                RelatedEntityId = entityId
            };
            switch (entityName)
            {
                case nameof(PraxisOpenItem):
                    var openItem = _repository.GetItem<PraxisOpenItem>(p => p.ItemId == entityId);
                    eventPayload.ClientId = openItem.ClientId;
                    eventPayload.OrganizationId = GetOrganizationByClientId(openItem.ClientId);
                    break;
                case nameof(PraxisEquipmentMaintenance):
                    var maintenance = _repository.GetItem<PraxisEquipmentMaintenance>(p => p.ItemId == entityId);
                    eventPayload.ClientId = maintenance.ClientId;
                    eventPayload.OrganizationId = GetOrganizationByClientId(maintenance.ClientId);
                    break;
                case nameof(PraxisProcessGuide):
                    var processGuide = _repository.GetItem<PraxisProcessGuide>(p => p.ItemId == entityId);
                    eventPayload.ClientId = clientId;
                    eventPayload.OrganizationId = GetOrganizationByClientId(clientId);
                    break;
            }

            _logger.LogInformation("Method: {MethodName}, LibraryFormUpdatedEventPayload: {Payload}", 
                nameof(GetFormUpdatedEventPayload), JsonConvert.SerializeObject(eventPayload));

            return eventPayload;
        }

        private string GetOrganizationByClientId(string clientId)
        {
            var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId);
            return client?.ParentOrganizationId;
        }
        private async Task OnFormFillInitiateCockpitActivityMetricsGeneration(ObjectArtifact objectArtifactData)
        {
            var isFormCompleted = _objectArtifactUtilityService.IsACompletedFormResponse(objectArtifactData.MetaData);
            var originalFormId = _objectArtifactUtilityService.GetOriginalArtifactId(objectArtifactData.MetaData);

            var eventPayload = GetFormUpdatedEventPayload(objectArtifactData);

            _logger.LogInformation("Method: {MethodName}  ObjectArtifactData: {artifactData}  IsFormCompleted: {IsFormCompleted}" +
                "  OriginalFormId: {originalFormId}", nameof(OnFormFillInitiateCockpitActivityMetricsGeneration),
                JsonConvert.SerializeObject(objectArtifactData), isFormCompleted, originalFormId);
            _logger.LogInformation("EventPayload: {eventPayload}", JsonConvert.SerializeObject(eventPayload));

            var allowedEntities = new List<string>
            {
                nameof(PraxisOpenItem),
                nameof(PraxisProcessGuide),
                nameof(PraxisEquipmentMaintenance)
            };
            if (eventPayload.RelatedEntityName != null && allowedEntities.Contains(eventPayload.RelatedEntityName))
            {
                await _cockpitFormDocumentActivityMetricsGenerationService.OnFormFillGenerateActivityMetrics(new string[] { originalFormId }, eventPayload.OrganizationId, eventPayload.ClientId, GetEntityBase(eventPayload.RelatedEntityName, eventPayload.RelatedEntityId));
            }
            else
            {
                await _cockpitDocumentActivityMetricsGenerationService.OnFormFillGenerateActivityMetrics(new[] { originalFormId }, eventPayload.RelatedEntityId, isFormCompleted);
            }
        }

        private EntityBase GetEntityBase(string entityName, string entityId)
        {
            return entityName switch
            {
                nameof(PraxisOpenItem) => _repository.GetItem<PraxisOpenItem>(p => p.ItemId == entityId),
                nameof(PraxisProcessGuide) => _repository.GetItem<PraxisProcessGuide>(p => p.ItemId == entityId),
                nameof(PraxisEquipmentMaintenance) => _repository.GetItem<PraxisEquipmentMaintenance>(p => p.ItemId == entityId),
                _ => null
            };
        }
    }
}
