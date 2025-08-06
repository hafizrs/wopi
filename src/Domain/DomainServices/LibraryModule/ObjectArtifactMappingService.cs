using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class ObjectArtifactMappingService : IObjectArtifactMappingService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IRepository _repository;
        private readonly IObjectArtifactUtilityService _objectArtifactUtilityService;
        private readonly ILogger<ObjectArtifactMappingService> _logger;

        public ObjectArtifactMappingService(
            ISecurityContextProvider securityContextProvider,
            IRepository repository,
            IObjectArtifactUtilityService objectArtifactUtilityService,
            ILogger<ObjectArtifactMappingService> logger
        )
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _objectArtifactUtilityService = objectArtifactUtilityService;
            _logger = logger;
        }

        public async Task CreateOrUpdateRiqsObjectArtifactMapping(RiqsObjectArtifactMapping mappingData, bool isUpdate)
        {
            try
            {
                if (!isUpdate) await _repository.SaveAsync(mappingData);
                else
                {
                    await _repository.UpdateAsync(m => m.ItemId == mappingData.ItemId, mappingData);
                }
                RiqsObjectArtifactMappingConstant.ResetRiqsArtifactMappingData(mappingData);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in CreateRiqsObjectArtifactMapping {Message} -> {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        public RiqsObjectArtifactMapping CreateRiqsObjectArtifactMappingPayload(ObjectArtifact artifact)
        {
            var mappingData = new RiqsObjectArtifactMapping()
            {
                ItemId = artifact.ItemId,
                CreatedBy = _securityContextProvider.GetSecurityContext().UserId,
                CreateDate = DateTime.UtcNow,
                ObjectArtifactId = artifact.ItemId,
                OrganizationId = artifact.OrganizationId,
                ApproverInfos = new List<ObjectArifactApproverInfo>(),
                UploadAdmins = new List<UserPraxisUserIdPair>()
            };

            if (artifact.MetaData == null ||
                !artifact.MetaData.TryGetValue(RiqsObjectArtifactMappingConstant.ApprovalAdminsKey, out var value))
                return mappingData;

            var data = value?.Value;
            if (string.IsNullOrEmpty(data)) return mappingData;

            var userIdPairs = JsonConvert.DeserializeObject<List<UserPraxisUserIdPair>>(data);
            mappingData.UploadAdmins = userIdPairs;

            return mappingData;
        }

        public async Task<string> UpdateAndGetApprovalStatusFromMapping(ObjectArtifact artifact,
            string controlMechanismName, DateTime currentUtcTime)
        {
            var approvalStatusValue = ((int)LibraryFileApprovalStatusEnum.APPROVED).ToString();
            var partiallyApprovedStatusValue = ((int)LibraryFileApprovalStatusEnum.PARTIALLY_APPROVED).ToString();
            var securityContext = _securityContextProvider.GetSecurityContext();
            var previousApproverIds = new List<string>();
            RiqsObjectArtifactMappingConstant.ResetRiqsArtifactMappingData(null);
            var mappingData =
                RiqsObjectArtifactMappingConstant.GetRiqsObjectArtifactMappingByArtifactId(artifact.ItemId);
            var approverInfo = new ObjectArifactApproverInfo()
            {
                ApproverId = securityContext.UserId,
                ApproverName = securityContext.DisplayName,
                ApprovedDate = currentUtcTime,
                ReapprovalCount = 0
            };

            if (string.IsNullOrEmpty(mappingData?.ItemId))
            {
                mappingData = CreateRiqsObjectArtifactMappingPayload(artifact);
                mappingData.ApproverInfos.Add(approverInfo);
                await CreateOrUpdateRiqsObjectArtifactMapping(mappingData, false);
            }
            else
            {
                previousApproverIds =
                    _objectArtifactUtilityService.GetPreviousApproverIdsByInterval(artifact, mappingData);
                if (mappingData.ApproverInfos == null)
                    mappingData.ApproverInfos = new List<ObjectArifactApproverInfo>();

                var last = mappingData.ApproverInfos.FindLast(a => !previousApproverIds.Contains(a.ApproverId));
                approverInfo.ReapprovalCount = last != null ? last.ReapprovalCount + 1 : 0;

                mappingData.ApproverInfos.Add(approverInfo);
                await CreateOrUpdateRiqsObjectArtifactMapping(mappingData, true);
            }

            RiqsObjectArtifactMappingConstant.ResetRiqsArtifactMappingData(mappingData);

            var isAInterfaceMigrationArtifact = _objectArtifactUtilityService.IsAInterfaceMigrationArtifact(artifact?.MetaData);

            if (controlMechanismName == LibraryControlMechanismConstant.SixEyePrinciple &&
                previousApproverIds.Count == 0 && !isAInterfaceMigrationArtifact)
            {
                return partiallyApprovedStatusValue;
            }

            return approvalStatusValue;
        }
    }
}
