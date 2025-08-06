using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public record ProjectedEquipmentResponse(
        string ClientId,
        string ClientName,
        string Name,
        string RoomId,
        string RoomName,
        string CategoryId,
        string CategoryName,
        string SubCategoryId,
        string SubCategoryName,
        PraxisKeyValue Topic,
        string Manufacturer,
        IEnumerable<PraxisEquipmentAdditionalInfo> AdditionalInfos,
        DateTime InstallationDate,
        DateTime? LastMaintenanceDate,
        DateTime? NextMaintenanceDate,
        string Email,
        string PhoneNumber,
        string Remarks,
        string SupplierId,
        string SupplierName,
        string SerialNumber,
        DateTime? DateOfPurchase,
        bool MaintenanceMode,
        string Company,
        string ContactPerson,
        IEnumerable<PraxisImage> Photos,
        IEnumerable<MaintenanceDateProp> MaintenanceDates,
        DateTime DateOfPlacingInService,
        string EquipmentQrFileId,
        IEnumerable<PraxisImage> LocationImages,
        IEnumerable<PraxisDocument> Files,
        IEnumerable<ItemIdAndTitle> PraxisUserAdditionalInformationTitles,
        string CompanyId,
        string ManufacturerId,
        string CreatedBy,
        DateTime CreateDate,
        string ItemId,
        DateTime LastUpdateDate,
        string Language,
        IEnumerable<PraxisSupplierContactPerson> EquipmentContactsInformation,
        IEnumerable<PraxisKeyValue> MetaValues,
        List<MetaDataKeyPairValue> MetaDataList
        );
}
