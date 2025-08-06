using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Common;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public record ProjectedEquipmentMaintenanceResponse(
    string ClientId,
    string PraxisEquipmentId,
    string Title,
    string EquipmentTitle,
    string Description,
    DateTime MaintenanceDate,
    DateTime MaintenanceEndDate,
    int MaintenancePeriod,
    IEnumerable<string> ResponsiblePersonIds,
    PraxisKeyValue CompletionStatus,
    RiqsActivityDetail CompletionStatusDetail,
    string Remarks,
    IEnumerable<EquipmentMaintenanceAnswer> Answers,
    List<PraxisLibraryEntityDetail> LibraryForms,
    List<PraxisLibraryFormResponse> LibraryFormResponses,
    IEnumerable<string> ExecutivePersonIds,
    IEnumerable<string> ApprovedPersonIds,
    string ScheduleType,
    string ProcessGuideId,
    List<PraxisEquipmentMaintenanceByExternalUser> ExternalUserInfos,
    bool ApprovalRequired,
    PraxisFormEntityDetail PraxisFormInfo,
    string CreatedBy,
    DateTime CreateDate,
    string ItemId,
    DateTime LastUpdateDate,
    List<MetaDataKeyPairValue> MetaDataList
);
}
