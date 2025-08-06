using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using SeliseBlocks.Genesis.Framework.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceEquipmentMigrationService : IRiqsInterfaceEquipmentMigrationService
    {
        private readonly IPraxisFileService _fileService;
        private readonly ILogger<RiqsInterfaceEquipmentMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IStorageDataService _storageDataService;
        private readonly INotificationService _notificationService;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IRiqsInterfaceEquipmentService _service;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new();

        public RiqsInterfaceEquipmentMigrationService(
         ILogger<RiqsInterfaceEquipmentMigrationService> logger,
         IRepository repository,
         IPraxisFileService fileService,
         IStorageDataService storageDataService,
         INotificationService notificationService,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ISecurityContextProvider securityContextProvider,
         IServiceClient serviceClient,
         IRiqsInterfaceEquipmentService service,
         IUilmResourceKeyService uilmResourceKeyService)
        {
            _logger = logger;
            _repository = repository;
            _fileService = fileService;
            _storageDataService = storageDataService;
            _notificationService = notificationService;
            _ecapRepository = ecapRepository;
            _securityContextProvider = securityContextProvider;
            _serviceClient = serviceClient;
            _service = service;
            _uilmResourceKeyService = uilmResourceKeyService;
        }

        public async Task<string> ProcessUploadEquipmentData(UplaodEquipemtInterfaceDataCommand command)
        {

            if (string.IsNullOrEmpty(command.FileId))
            {
                return string.Empty;
            }

            try
            {
                var fileStream = await DownloadFileStreamFromFileId(command.FileId);

                if (fileStream == null)
                {
                    _logger.LogError("Error occurred : FIle steam null in {ClassName} ",
                      nameof(DownloadFileStreamFromFileId));
                    return string.Empty;
                }

                using (fileStream)
                {
                    _logger.LogInformation("Enter {ClassName} with payload: {Payload}",
                 nameof(ProcessUploadEquipmentData), JsonConvert.SerializeObject(command));

                    var interfaceSummary = await PrepareEquipmentInterfaceSummery(fileStream, command);

                    if (interfaceSummary != null)
                    {
                        interfaceSummary.ClientId = command.ClientId;
                        interfaceSummary.OrganizationId = command.OrganizationId;
                        _repository.Save(interfaceSummary);

                        await PublishProcessMigrationCompletedEvent(command);
                        return interfaceSummary.ItemId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} ",
                    nameof(ProcessUploadEquipmentData));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message,
              ex.StackTrace);
            }

            return string.Empty;
        }


        private async Task PublishProcessMigrationCompletedEvent(
         UplaodEquipemtInterfaceDataCommand command
         )
        {

            await _notificationService.GetCommonSubscriptionNotification(
                        true,
                        command.NotificationSubscriptionId,
                        command.ActionName,
                        command.Context
                    );
        }

        private async Task<RiqsEquipmentInterfaceMigrationSummary> PrepareEquipmentInterfaceSummery(Stream fileStream, UplaodEquipemtInterfaceDataCommand command)
        {
            if (fileStream == null)
            {
                _logger.LogError("Error occurred: File stream is null in {MethodName}", nameof(PrepareEquipmentInterfaceSummery));
                return null;
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets.Count() >= 2
                        ? package.Workbook.Worksheets[1]
                        : package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet.Dimension == null)
                    {
                        return null;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    var equipmentList = new List<TempEquipmentInterfacePaseData>();
                    var maintenanceList = new List<TempEquipmentMaintenancesInterfacePastData>();

                    command.IsUpdate = worksheet.Cells[1, 1].Text.Trim() == "Equipment ID";

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var equipment = MapRowToEquipment(worksheet, row, command);
                            var eqMaintenance = GetMaintenanceRows(worksheet, row, command, equipment);

                            if (equipment == null) continue;
                            equipmentList.Add(equipment);
                            maintenanceList.AddRange(eqMaintenance);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error occurred in {ClassName}.",
                             nameof(PrepareEquipmentInterfaceSummery));
                            _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message,
                          ex.StackTrace);
                        }
                    }

                    if (equipmentList != null && equipmentList.Any())
                    {
                        if (command.IsUpdate)
                        {
                            equipmentList = PrepareUpdateEquipmentData(equipmentList);
                        }

                        var collection = _ecapRepository
                           .GetTenantDataContext()
                           .GetCollection<BsonDocument>("TempEquipmentInterfacePaseDatas");

                        var bulkOperations = equipmentList.Select(equipment =>
                        {
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", equipment.ItemId);
                            var update = equipment.ToBsonDocument();
                            return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                        }).ToList();

                        var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);
                    }

                    if (maintenanceList != null && maintenanceList.Any())
                    {
                        var collection = _ecapRepository
                           .GetTenantDataContext()
                           .GetCollection<BsonDocument>("TempEquipmentMaintenancesInterfacePastDatas");

                        var bulkOperations = maintenanceList.Select(equipmentMaintenance =>
                        {
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", equipmentMaintenance.ItemId);
                            var update = equipmentMaintenance.ToBsonDocument();
                            return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                        }).ToList();

                        var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);
                    }

                    var summary = new RiqsEquipmentInterfaceMigrationSummary
                    {
                        ItemId = command.MigrationSummaryId,
                        IsMarkedToDelete = false,
                        IsDraft = false,
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        IsUpdate = command.IsUpdate
                    };

                    return summary;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} ",
                    nameof(PrepareEquipmentInterfaceSummery));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message,
              ex.StackTrace);
            }
            return null;
        }

        private List<TempEquipmentInterfacePaseData> PrepareUpdateEquipmentData(List<TempEquipmentInterfacePaseData> tempEquipmentList)
        {
            var equipmentIds = tempEquipmentList.Select(e => e.ItemId).ToList();
            var existingEquipments = _repository.GetItems<PraxisEquipment>(e => equipmentIds.Contains(e.ItemId))?.ToList();

            if (existingEquipments != null && existingEquipments.Count > 0)
            {
                foreach (var tempEquipment in tempEquipmentList)
                {
                    var equipment = existingEquipments.FirstOrDefault(e => e.ItemId == tempEquipment.ItemId);
                    if (equipment != null)
                    {
                        tempEquipment.Files = equipment.Files;
                        tempEquipment.SupplierId = equipment.SupplierId;
                        tempEquipment.SupplierName = equipment.SupplierName;
                        tempEquipment.RoomId = equipment.RoomId;
                        tempEquipment.RoomName = equipment.RoomName;
                        tempEquipment.ManufacturerId = equipment.ManufacturerId;
                        tempEquipment.Manufacturer = equipment.Manufacturer;
                    }
                }
            }

            return tempEquipmentList;
        }

        private TempEquipmentInterfacePaseData MapRowToEquipment(ExcelWorksheet worksheet, int row, UplaodEquipemtInterfaceDataCommand command)
        {
            string equipmentId = command.IsUpdate ? worksheet.Cells[row, 1].Text.Trim() : string.Empty;
            string name = worksheet.Cells[row, command.IsUpdate ? 2 : 1].Text.Trim();
            if (string.IsNullOrEmpty(name)) return null;

            var equipment = new TempEquipmentInterfacePaseData
            {
                MigrationSummeryId = command.MigrationSummaryId,
                ItemId = !string.IsNullOrEmpty(equipmentId) ? equipmentId : Guid.NewGuid().ToString(),
                Name = name,
                Remarks = worksheet.Cells[row, command.IsUpdate ? 12 : 11].Text.Trim(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                ClientId = command.ClientId,
                AdditionalInfos = new List<PraxisEquipmentAdditionalInfo>()
            };
            string exactLocation = worksheet.Cells[row, command.IsUpdate ? 3 : 2].Text.Trim();
            var metaValue = new PraxisKeyValue
            {
                Key = "ExactLocation",
                Value = exactLocation
            };
            var metaValues = new List<PraxisKeyValue>() { metaValue };

            var metaDataList = new List<MetaDataKeyPairValue>();

            var serialNumber = worksheet.Cells[row, command.IsUpdate ? 4 : 3].Text.Trim();
            var uDINumber = worksheet.Cells[row, command.IsUpdate ? 5 : 4].Text.Trim();
            var internalNumber = worksheet.Cells[row, command.IsUpdate ? 6 : 5].Text.Trim();
            var installationNumber = worksheet.Cells[row, command.IsUpdate ? 7 : 6].Text.Trim();

            AddMetaData(metaDataList, EquipmentMetaDataKeys.SerialNumber, serialNumber);
            AddMetaData(metaDataList, EquipmentMetaDataKeys.UDINumber, uDINumber);
            AddMetaData(metaDataList, EquipmentMetaDataKeys.InternalNumber, internalNumber);
            AddMetaData(metaDataList, EquipmentMetaDataKeys.InstallationNumber, installationNumber);

            if (command.IsUpdate && !string.IsNullOrEmpty(equipmentId))
            {
                var existingEquipment = _repository.GetItem<PraxisEquipment>(e => e.ItemId == equipmentId);
                if (existingEquipment != null)
                {
                    PrepareExistingEquipment(equipment, existingEquipment);
                    if (existingEquipment.MetaValues != null && existingEquipment.MetaValues.Count() > 0)
                    {
                        metaValues = existingEquipment.MetaValues.ToList();

                        var exactLocationMeta = metaValues
                            .FirstOrDefault(mv => mv.Key == "ExactLocation");

                        if (exactLocationMeta != null)
                        {
                            exactLocationMeta.Value = exactLocation;
                        }
                        else
                        {
                            metaValues.Add(new PraxisKeyValue
                            {
                                Key = "ExactLocation",
                                Value = exactLocation
                            });
                        }
                    }

                    if (existingEquipment.MetaDataList != null && existingEquipment.MetaDataList.Count() > 0)
                    {
                        metaDataList = existingEquipment.MetaDataList;

                        var exactSerialNumberMeta = metaDataList
                            .FirstOrDefault(mv => mv.Key == EquipmentMetaDataKeys.SerialNumber);

                        if (exactSerialNumberMeta != null)
                        {
                            exactSerialNumberMeta.MetaData.Value = serialNumber;
                        }
                        else
                        {
                            AddMetaData(metaDataList, EquipmentMetaDataKeys.SerialNumber, serialNumber);
                        }

                        var exactUDINumberMeta = metaDataList
                            .FirstOrDefault(mv => mv.Key == EquipmentMetaDataKeys.UDINumber);

                        if (exactUDINumberMeta != null)
                        {
                            exactSerialNumberMeta.MetaData.Value = uDINumber;
                        }
                        else
                        {
                            AddMetaData(metaDataList, EquipmentMetaDataKeys.UDINumber, uDINumber);
                        }

                        var exactInternalNumberMeta = metaDataList
                           .FirstOrDefault(mv => mv.Key == EquipmentMetaDataKeys.InternalNumber);

                        if (exactInternalNumberMeta != null)
                        {
                            exactSerialNumberMeta.MetaData.Value = internalNumber;
                        }
                        else
                        {
                            AddMetaData(metaDataList, EquipmentMetaDataKeys.InternalNumber, internalNumber);
                        }

                        var exactInstallationNumberMeta = metaDataList
                           .FirstOrDefault(mv => mv.Key == EquipmentMetaDataKeys.InstallationNumber);

                        if (exactInstallationNumberMeta != null)
                        {
                            exactSerialNumberMeta.MetaData.Value = installationNumber;
                        }
                        else
                        {
                            AddMetaData(metaDataList, EquipmentMetaDataKeys.InstallationNumber, installationNumber);
                        }

                    }
                }
            }

            equipment.MetaValues = metaValues;

            equipment.MetaDataList = metaDataList;

            if (DateTime.TryParseExact(worksheet.Cells[row, command.IsUpdate ? 8 : 7].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime purchaseDate))
            {
                equipment.DateOfPurchase = purchaseDate;
            }

            if (DateTime.TryParseExact(worksheet.Cells[row, command.IsUpdate ? 9 : 8].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime serviceDate))
            {
                equipment.DateOfPlacingInService = serviceDate;
            }

            string addonTitle = worksheet.Cells[row, command.IsUpdate ? 10 : 9].Text.Trim();
            string addonDescription = worksheet.Cells[row, command.IsUpdate ? 11 : 10].Text.Trim();

            CreateAdditionalInfo(equipment, addonTitle, addonDescription);

            if(!command.IsUpdate && string.IsNullOrEmpty(equipmentId))
            {
                PrepareRolePermission(equipment);
            }

            return equipment;
        }

        private List<TempEquipmentMaintenancesInterfacePastData> GetMaintenanceRows(
            ExcelWorksheet worksheet,
            int row,
            UplaodEquipemtInterfaceDataCommand command,
            TempEquipmentInterfacePaseData equipment)
        {
            var allMaintenances = new List<TempEquipmentMaintenancesInterfacePastData>();

            if(command.IsUpdate) { return allMaintenances; }

            var maintenanceCols1 = new List<int> { 12, 13, 14 };
            var maintenanceCols2 = new List<int> { 15, 16, 17 };

            var maint1 = PrepareEquipmentMaintenances(worksheet, row, equipment, maintenanceCols1);
            var maint2 = PrepareEquipmentMaintenances(worksheet, row, equipment, maintenanceCols2);

            if (maint1 != null) allMaintenances.Add(maint1);
            if (maint2 != null) allMaintenances.Add(maint2);

            return allMaintenances;
        }

        bool RowsHaveData(ExcelWorksheet worksheet, int row, List<int> columns)
        {
            foreach (int column in columns)
            {
                var value = worksheet.Cells[row, column].Text;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return true; // At least one row has value
                }
            }
            return false; // All rows are empty
        }

        private TempEquipmentMaintenancesInterfacePastData PrepareEquipmentMaintenances(
           ExcelWorksheet worksheet,
           int row,
           TempEquipmentInterfacePaseData equipment,
           List<int> columns)
        {
            if (!RowsHaveData(worksheet, row, columns))
            {
                return null;
            }

            var equipmentMaintenances = new TempEquipmentMaintenancesInterfacePastData
            {
                MigrationSummeryId = equipment.MigrationSummeryId,
                ItemId = Guid.NewGuid().ToString(),
                PraxisEquipmentId = equipment.ItemId,
                Title = equipment.Name,
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                ClientId = equipment.ClientId,
            };

            string maintenanceValidation = worksheet.Cells[row, columns[0]].Text.Trim();

            if (!string.IsNullOrEmpty(maintenanceValidation))
            {
                char firstChar = maintenanceValidation[0];
                switch (char.ToUpper(firstChar))
                {
                    case 'M':
                        equipmentMaintenances.ScheduleType = "MAINTENANCE";
                        break;
                    case 'V':
                        equipmentMaintenances.ScheduleType = "VALIDATION";
                        break;
                    default:
                        break;
                }
            }

            var metaDataList = new List<MetaDataKeyPairValue>();

            metaDataList.Add(new MetaDataKeyPairValue
            {
                Key = "IsPastMaintenance",
                MetaData = new MetaValuePair
                {
                    Type = "bool",
                    Value = "true"
                }
            });

            equipmentMaintenances.MetaDataList = metaDataList;

            if (DateTime.TryParseExact(worksheet.Cells[row, columns[1]].Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime maintenanceValidationDate))
            {
                equipmentMaintenances.MaintenanceDate = maintenanceValidationDate;
                equipmentMaintenances.MaintenanceEndDate = maintenanceValidationDate;

                var equipmentMaintenanceDate = new MaintenanceDateProp
                {
                    ItemId = Guid.NewGuid().ToString(),
                    Date = maintenanceValidationDate,
                    CompletionStatus = new PraxisKeyValue { Key = "done", Value = "DONE" }
                };
                if(equipment.MaintenanceDates != null && equipment.MaintenanceDates.Any())
                {
                    var maintenanceDates = new List<MaintenanceDateProp>();
                    maintenanceDates = equipment.MaintenanceDates.ToList();
                    maintenanceDates.Add(equipmentMaintenanceDate);
                    equipment.MaintenanceDates = maintenanceDates;
                }
                else
                {
                    var maintenanceDates = new List<MaintenanceDateProp>() { equipmentMaintenanceDate };
                    equipment.MaintenanceDates = maintenanceDates;
                }

                var key = "MaintenanceDates";

                var index = equipment.MetaDataList.FindIndex(d => d.Key == key);

                var equipmentMaintenanceDates = new List<MaintenanceValidationDateProp>();
                if (index >= 0)
                {
                    var value = equipment.MetaDataList[index].MetaData.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        equipmentMaintenanceDates = JsonConvert.DeserializeObject<List<MaintenanceValidationDateProp>>(value);
                    }
                }

                var newEquipmentMaintenanceDate = new MaintenanceValidationDateProp
                {
                    ItemId = equipmentMaintenanceDate.ItemId,
                    ScheduleType = equipmentMaintenances.ScheduleType,
                    Date = equipmentMaintenanceDate.Date,
                    CompletionStatus = equipmentMaintenanceDate.CompletionStatus
                };

                equipmentMaintenanceDates.Add(newEquipmentMaintenanceDate);


                var metaValuePairType = new MetaValuePair
                {
                    Type = "Array",
                    Value = JsonConvert.SerializeObject(equipmentMaintenanceDates)
                };

                var maintenanceDatesEntry = new MetaDataKeyPairValue
                {
                    Key = key,
                    MetaData = metaValuePairType
                };

                
                if (index >= 0)
                {
                    equipment.MetaDataList[index] = maintenanceDatesEntry;
                }
                else
                {
                    equipment.MetaDataList.Add(maintenanceDatesEntry);
                }
            }

            string responsiveUser = worksheet.Cells[row, columns[2]].Text.Trim();

            if (!string.IsNullOrEmpty(responsiveUser))
            {
                var usersArray = responsiveUser
                    .Split(',')
                    .Select(u => $"\"{u.Trim()}\"");

                string responsiveUsers = "[" + string.Join(",", usersArray) + "]";

                equipmentMaintenances.MetaDataList.Add(new MetaDataKeyPairValue
                {
                    Key = "OldExecutiveMembers",
                    MetaData = new MetaValuePair
                    {
                        Type = "string[]",
                        Value = responsiveUsers
                    }
                });
            }

            equipmentMaintenances.CompletionStatus = new PraxisKeyValue
            {
                Key = "done",
                Value = "DONE"
            };

            equipmentMaintenances.ExternalUserInfos = new List<PraxisEquipmentMaintenanceByExternalUser>();

            return equipmentMaintenances;
        }

        void PrepareExistingEquipment(TempEquipmentInterfacePaseData equipment, PraxisEquipment praxisEquipment)
        {
            equipment.ItemId = praxisEquipment.ItemId;
            equipment.CreateDate = praxisEquipment.CreateDate;
            equipment.CreatedBy = praxisEquipment.CreatedBy;
            equipment.LastUpdateDate = praxisEquipment.LastUpdateDate;
            equipment.LastUpdatedBy = praxisEquipment.LastUpdatedBy;
            equipment.Language = praxisEquipment.Language;
            equipment.Tags = praxisEquipment.Tags;
            equipment.TenantId = praxisEquipment.TenantId;
            equipment.IsMarkedToDelete = praxisEquipment.IsMarkedToDelete;
            equipment.RolesAllowedToRead = praxisEquipment.RolesAllowedToRead;
            equipment.IdsAllowedToRead = praxisEquipment.IdsAllowedToRead;
            equipment.RolesAllowedToWrite = praxisEquipment.RolesAllowedToWrite;
            equipment.IdsAllowedToWrite = praxisEquipment.IdsAllowedToWrite;
            equipment.RolesAllowedToUpdate = praxisEquipment.RolesAllowedToUpdate;
            equipment.IdsAllowedToUpdate = praxisEquipment.IdsAllowedToUpdate;
            equipment.RolesAllowedToDelete = praxisEquipment.RolesAllowedToDelete;
            equipment.IdsAllowedToDelete = praxisEquipment.IdsAllowedToDelete;
            equipment.ClientId = praxisEquipment.ClientId;
            equipment.ClientName = praxisEquipment.ClientName;
            equipment.Name = praxisEquipment.Name;
            equipment.RoomId = praxisEquipment.RoomId;
            equipment.RoomName = praxisEquipment.RoomName;
            equipment.CategoryId = praxisEquipment.CategoryId;
            equipment.CategoryName = praxisEquipment.CategoryName;
            equipment.SubCategoryId = praxisEquipment.SubCategoryId;
            equipment.SubCategoryName = praxisEquipment.SubCategoryName;
            equipment.Topic = praxisEquipment.Topic;
            equipment.Manufacturer = praxisEquipment.Manufacturer;
            equipment.AdditionalInfos = praxisEquipment.AdditionalInfos;
            equipment.InstallationDate = praxisEquipment.InstallationDate;
            equipment.LastMaintenanceDate = praxisEquipment.LastMaintenanceDate;
            equipment.NextMaintenanceDate = praxisEquipment.NextMaintenanceDate;
            equipment.Email = praxisEquipment.Email;
            equipment.PhoneNumber = praxisEquipment.PhoneNumber;
            equipment.Remarks = praxisEquipment.Remarks;
            equipment.SupplierId = praxisEquipment.SupplierId;
            equipment.SupplierName = praxisEquipment.SupplierName;
            equipment.SerialNumber = praxisEquipment.SerialNumber;
            equipment.DateOfPurchase = praxisEquipment.DateOfPurchase;
            equipment.MaintenanceMode = praxisEquipment.MaintenanceMode;
            equipment.Company = praxisEquipment.Company;
            equipment.ContactPerson = praxisEquipment.ContactPerson;
            equipment.Photos = praxisEquipment.Photos;
            equipment.MaintenanceDates = praxisEquipment.MaintenanceDates;
            equipment.DateOfPlacingInService = praxisEquipment.DateOfPlacingInService;
            equipment.EquipmentQrFileId = praxisEquipment.EquipmentQrFileId;
            equipment.LocationImages = praxisEquipment.LocationImages;
            equipment.Files = praxisEquipment.Files;
            equipment.PraxisUserAdditionalInformationTitles = praxisEquipment.PraxisUserAdditionalInformationTitles;
            equipment.CompanyId = praxisEquipment.CompanyId;
            equipment.ManufacturerId = praxisEquipment.ManufacturerId;
            equipment.ContactPersons = praxisEquipment.ContactPersons;
            equipment.MetaValues = praxisEquipment.MetaValues;
            equipment.EquipmentContactsInformation = praxisEquipment.EquipmentContactsInformation;
            equipment.MetaDataList = praxisEquipment.MetaDataList;
        }

        void PrepareRolePermissionForEquipmentMaintenances(PraxisEquipmentMaintenance equipmentMaintenance)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var authorizedRoles = new[] { RoleNames.Admin, RoleNames.TaskController };
            var authorizedIds = new[] { userId };
            equipmentMaintenance.IdsAllowedToRead = authorizedIds;
            equipmentMaintenance.IdsAllowedToUpdate = authorizedIds;
            equipmentMaintenance.RolesAllowedToRead = authorizedRoles;
            equipmentMaintenance.RolesAllowedToUpdate = authorizedRoles;
            equipmentMaintenance.RolesAllowedToDelete = authorizedRoles;
            equipmentMaintenance.Tags = new[] { "Is-Valid-PraxisEquipmentMaintenance" };
            equipmentMaintenance.CreatedBy = userId;
            equipmentMaintenance.LastUpdatedBy = userId;
            equipmentMaintenance.TenantId = _securityContextProvider.GetSecurityContext().TenantId;

        }

        private void CreateAdditionalInfo(
            PraxisEquipment equipment,
            string title,
            string description)
        {

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(description))
            {

                var additionalInfos = new List<PraxisEquipmentAdditionalInfo>()
                {
                    new PraxisEquipmentAdditionalInfo()
                    {
                        ItemId= Guid.NewGuid().ToString(),
                        Title=title,
                        Description=description
                    }
                };

                if(equipment.AdditionalInfos != null && equipment.AdditionalInfos.Count() > 0)
                {
                    if(!equipment.AdditionalInfos.Any(x => x.Title == title && x.Description == description))
                    {
                        additionalInfos.AddRange(equipment.AdditionalInfos.ToList());
                    }
                    else
                    {
                        additionalInfos = equipment.AdditionalInfos.ToList();
                    }
                }
                equipment.AdditionalInfos = additionalInfos.AsEnumerable();
            }

        }
        void PrepareRolePermission(PraxisEquipment equipment)
        {
            var userId = _securityContextProvider.GetSecurityContext().UserId;
            var authorizedRoles = new[] { RoleNames.Admin, RoleNames.TaskController };
            var authorizedIds = new[] { userId };
            equipment.IdsAllowedToRead = authorizedIds;
            equipment.IdsAllowedToUpdate = authorizedIds;
            equipment.RolesAllowedToRead = authorizedRoles;
            equipment.RolesAllowedToUpdate = authorizedRoles;
            equipment.RolesAllowedToDelete = authorizedRoles;
            equipment.Tags = new[] { "Is-Valid-PraxisEquipment" };
            equipment.CreatedBy = userId;
            equipment.LastUpdatedBy = userId;
            equipment.TenantId = _securityContextProvider.GetSecurityContext().TenantId;

        }
        private void AddMetaData(List<MetaDataKeyPairValue> metaDataList, string key, string cellValue)
        {
            if (!string.IsNullOrEmpty(cellValue))
            {
                metaDataList.Add(new MetaDataKeyPairValue
                {
                    Key = key,
                    MetaData = new MetaValuePair
                    {
                        Type = "String",
                        Value = cellValue
                    }
                });
            }

        }

        private async Task<Stream> DownloadFileStreamFromFileId(string fileId)
        {
            var file = await _fileService.GetFileInfoFromStorage(fileId);
            return await Task.Run(() => _storageDataService.GetFileContentStream(file?.Url));
        }

        public async Task<RiqsEquipmentInterfaceMigrationSummary> GetEquipmentMigrationSummery(GetEquipmentInterfaceSummeryQuery query)
        {
            if (string.IsNullOrEmpty(query.MigrationSummaryId))
            {
                return null;
            }

            try
            {
                var migrationSummery = await _repository.GetItemAsync<RiqsEquipmentInterfaceMigrationSummary>(x => x.ItemId == query.MigrationSummaryId);

                if (migrationSummery == null) return null;
                int skipCount = (query.PageNumber - 1) * query.PageSize;
                long totalRecord = 0;
                var queryFilter = Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", query.MigrationSummaryId);

                var collections = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<BsonDocument>("TempEquipmentInterfacePaseDatas")
                .Aggregate()
                .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();
                collections = collections.Skip(skipCount).Limit(query.PageSize);


                if (totalRecord > 0)
                {


                    var results = collections.ToEnumerable()
                  .Select(document => BsonSerializer.Deserialize<TempEquipmentInterfacePaseData>(document));
                    var equipments = results.ToList();
                    foreach (var equipment in equipments)
                    {
                        equipment.IdsAllowedToRead = null;
                        equipment.IdsAllowedToUpdate = null;
                        equipment.IdsAllowedToDelete = null;
                        equipment.RolesAllowedToRead = null;
                        equipment.RolesAllowedToUpdate = null;
                        equipment.RolesAllowedToDelete = null;
                    }

                    var praxisEquipmentMaintenances = GetPraxisEquipmentMaintenances(query);

                    return new RiqsEquipmentInterfaceMigrationSummary
                    {
                        ItemId = query.MigrationSummaryId,
                        IsDraft = migrationSummery.IsDraft,
                        ClientId = migrationSummery.ClientId,
                        OrganizationId = migrationSummery.OrganizationId,
                        PraxisEquipments = equipments,
                        PraxisEquipmentMaintenances = praxisEquipmentMaintenances,
                        TotalRecord = totalRecord,
                        IsUpdate = migrationSummery.IsUpdate
                    };
                }

                // Return an empty response if no results found
                return new RiqsEquipmentInterfaceMigrationSummary
                {
                    ItemId = query.MigrationSummaryId,
                    IsDraft = false,
                    PraxisEquipments = new List<TempEquipmentInterfacePaseData>(),
                    PraxisEquipmentMaintenances = new List<TempEquipmentMaintenancesInterfacePastData>(),
                    TotalRecord = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(GetEquipmentMigrationSummery), ex.Message, ex.StackTrace);

            }

            return null;

        }

        private List<TempEquipmentMaintenancesInterfacePastData> GetPraxisEquipmentMaintenances(GetEquipmentInterfaceSummeryQuery query)
        {
            var equipmentMaintenances = new List<TempEquipmentMaintenancesInterfacePastData>();
            int skipCount = (query.PageNumber - 1) * query.PageSize;
            long totalRecord = 0;
            var queryFilter = Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", query.MigrationSummaryId);

            var collections = _ecapRepository
            .GetTenantDataContext()
            .GetCollection<BsonDocument>("TempEquipmentMaintenancesInterfacePastDatas")
            .Aggregate()
            .Match(queryFilter);

            totalRecord = collections.ToEnumerable().Count();
            collections = collections.Skip(skipCount).Limit(query.PageSize);

            if (totalRecord > 0)
            {
                var results = collections.ToEnumerable().Select(document => BsonSerializer.Deserialize<TempEquipmentMaintenancesInterfacePastData>(document));
                equipmentMaintenances = results.ToList();
                foreach (var equipment in equipmentMaintenances)
                {
                    equipment.IdsAllowedToRead = null;
                    equipment.IdsAllowedToUpdate = null;
                    equipment.IdsAllowedToDelete = null;
                    equipment.RolesAllowedToRead = null;
                    equipment.RolesAllowedToUpdate = null;
                    equipment.RolesAllowedToDelete = null;
                }
            }

            return equipmentMaintenances;
        }

        public async Task ProcessUploadEquipmentAdditionalData(UpdateEquipemtInterfaceAdditioanalDataCommand command)
        {
            if (string.IsNullOrEmpty(command.MigrationSummaryId) || !command.EquipmentIds.Any()) return;

            try
            {
                var filter = Builders<BsonDocument>.Filter.And(
                  Builders<BsonDocument>.Filter.In("_id", command.EquipmentIds),
                  Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", command.MigrationSummaryId)
              );


                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempEquipmentInterfacePaseDatas");
                var documents = await collection.Find(filter).ToListAsync();

                var updates = new List<UpdateOneModel<BsonDocument>>();
                var createEquipmentIds = new List<string>();
                foreach (var document in documents)
                {
                    var metaValuesArray = document.GetValue("MetaValues", new BsonArray()).AsBsonArray;
                    var mergedMetaValues = MergeMetaValues(metaValuesArray, command.MetaValues);

                    var update = Builders<BsonDocument>.Update
                      .Set("Files", command.Attachments)
                      .Set("MetaValues", mergedMetaValues)
                      .Set("SupplierId", command.Supplier.SupplierId)
                      .Set("SupplierName", command.Supplier.SupplierName)
                      .Set("RoomId", command.Location.RoomId)
                      .Set("RoomName", command.Location.RoomName)
                      .Set("Manufacturer", command.Manufacturer.Manufacturer)
                      .Set("ManufacturerId", command.Manufacturer.ManufacturerId);

                    updates.Add(new UpdateOneModel<BsonDocument>(
                        Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                        update
                    ));
                    createEquipmentIds.Add(document["_id"].AsString);
                }


                if (updates.Count > 0 && createEquipmentIds.Count > 0)
                {
                    await collection.BulkWriteAsync(updates);

                    var createEquimentFromRiqsInterfaceMigrationCommand = new CreateEquimentFromRiqsInterfaceMigrationCommand
                    {
                        EquipmentIds = createEquipmentIds,
                        MigrationSummaryId = command.MigrationSummaryId
                    };
                    _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), createEquimentFromRiqsInterfaceMigrationCommand);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(ProcessUploadEquipmentAdditionalData), ex.Message, ex.StackTrace);
            }
        }

        public static BsonArray MergeMetaValues(BsonArray existingArray, IEnumerable<PraxisKeyValue> newValues)
        {
            existingArray ??= new BsonArray();

            if (newValues == null)
                return existingArray;

            var existingList = existingArray
                .Where(x => x != null && x.IsBsonDocument && x.AsBsonDocument.Contains("Key") && x.AsBsonDocument.Contains("Value"))
                .Select(x => new PraxisKeyValue
                {
                    Key = x["Key"].AsString,
                    Value = x["Value"].AsString
                })
                .ToList();

            foreach (var newItem in newValues)
            {
                if (string.IsNullOrWhiteSpace(newItem?.Key)) continue;

                var existingItem = existingList.FirstOrDefault(x => x.Key == newItem.Key);

                if (existingItem?.Key == "CompanyContactInfo") continue;

                if (existingItem != null)
                {
                    existingItem.Value = newItem.Value;
                }
                else
                {
                    existingList.Add(new PraxisKeyValue { Key = newItem.Key, Value = newItem.Value });
                }
            }

            var mergedArray = new BsonArray(
                existingList.Select(x =>
                    new BsonDocument
                    {
                { "Key", x.Key },
                { "Value", x.Value ?? string.Empty }
                    })
            );

            return mergedArray;
        }

        public async Task<string> ProcessDownloadEquipmentData(DownloadEquipemtInterfaceDataCommand command)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("ClientId", command.ClientId);

                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisEquipments");

                var documents = await collection.Find(filter).ToListAsync();

                var equipmentList = documents.Select(doc => BsonSerializer.Deserialize<PraxisEquipment>(doc)).ToList();

                _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(GetDownloadEquipmentDataLanguageKeys(), command.Language);

                if (equipmentList != null && equipmentList.Count > 0)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Equipment-Update-List");

                    // Add Header
                    worksheet.Cells[1, 1].Value = _translatedStringsAsDictionary["Equipment ID"];
                    worksheet.Cells[1, 2].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.NAME"];
                    worksheet.Cells[1, 3].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.EXACT_LOCATION"];
                    worksheet.Cells[1, 4].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SERIAL_NUMBER"];
                    worksheet.Cells[1, 5].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.UDI_NUMBER"];
                    worksheet.Cells[1, 6].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.INTERNAL_NUMBER"];
                    worksheet.Cells[1, 7].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.INSTALLATION_NUMBER"];
                    worksheet.Cells[1, 8].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.PURCHASE_DATE"];
                    worksheet.Cells[1, 9].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.PLACING_IN_SERVICE"];
                    worksheet.Cells[1, 10].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.ADD_ONS_TITLE"];
                    worksheet.Cells[1, 11].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.ADD_ONS_DESCRIPTION"];
                    worksheet.Cells[1, 12].Value = _translatedStringsAsDictionary["Remarks"];

                    // Set column widths
                    worksheet.Column(1).Width = 40;  // Equipment ID
                    worksheet.Column(2).Width = 25;  // Equipment-Name
                    worksheet.Column(3).Width = 46;  // Exact Location
                    worksheet.Column(4).Width = 20;  // Serial-Number
                    worksheet.Column(5).Width = 20;  // UDI Number
                    worksheet.Column(6).Width = 20;  // Internal ID-Number
                    worksheet.Column(7).Width = 22;  // Installation Number
                    worksheet.Column(8).Width = 18;  // Date of manufacture
                    worksheet.Column(9).Width = 25;  // Date of placing in service
                    worksheet.Column(10).Width = 30;  // Add ons Title
                    worksheet.Column(11).Width = 30; // Add ons Description
                    worksheet.Column(12).Width = 50; // Remarks

                    worksheet.Row(1).Height = 20;

                    using (var range = worksheet.Cells[1, 1, 1, 12])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.General;

                        // Fill background color
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#DDEBF7"));
                    }

                    int row = 2;
                    foreach (var equipment in equipmentList)
                    {
                        worksheet.Cells[row, 1].Value = equipment.ItemId;
                        worksheet.Cells[row, 2].Value = equipment.Name;
                        var exactLocation = equipment?.MetaValues?.FirstOrDefault(x => x.Key == "ExactLocation")?.Value;
                        worksheet.Cells[row, 3].Value = exactLocation ?? string.Empty;
                        var serialNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.SerialNumber)?.MetaData?.Value;
                        worksheet.Cells[row, 4].Value = serialNumber ?? string.Empty;
                        var udiNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.UDINumber)?.MetaData?.Value;
                        worksheet.Cells[row, 5].Value = udiNumber ?? string.Empty;
                        var internalNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InternalNumber)?.MetaData?.Value;
                        worksheet.Cells[row, 6].Value = internalNumber ?? string.Empty;
                        var installationNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InstallationNumber)?.MetaData?.Value;
                        worksheet.Cells[row, 7].Value = installationNumber ?? string.Empty;
                        var dateOfPurchase = equipment.DateOfPurchase == null
                            || (equipment.DateOfPurchase == DateTime.MinValue)
                            || (equipment.DateOfPurchase.Value.Date.Year < 2000)
                            ? string.Empty
                            : equipment.DateOfPurchase.Value.ToString("dd.MM.yyyy");
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "@";
                        worksheet.Cells[row, 8].Value = dateOfPurchase ?? string.Empty;
                        var dateOfPlacingInService = (equipment.DateOfPlacingInService == DateTime.MinValue) || (equipment.DateOfPlacingInService.Date.Year < 2000) ? string.Empty : equipment.DateOfPlacingInService.ToString("dd.MM.yyyy");
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "@";
                        worksheet.Cells[row, 9].Value = dateOfPlacingInService ?? string.Empty;
                        var adonTitle = equipment?.AdditionalInfos?.FirstOrDefault()?.Title ?? string.Empty;
                        var adonDes = equipment?.AdditionalInfos?.FirstOrDefault()?.Description ?? string.Empty;
                        worksheet.Cells[row, 10].Value = adonTitle;
                        worksheet.Cells[row, 11].Value = adonDes;
                        worksheet.Cells[row, 12].Value = !string.IsNullOrEmpty(equipment?.Remarks) ? Regex.Replace(equipment?.Remarks, "<.*?>", "") : string.Empty;
                        row++;
                    }

                    var fileId = Guid.NewGuid().ToString();
                    string fileName = $"Equipment-Update-List.xlsx";
                    var success = await _storageDataService.UploadFileAsync(fileId, fileName, package.GetAsByteArray());
                    if (success) return fileId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} ", nameof(ProcessDownloadEquipmentData));
                _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", ex.Message,
              ex.StackTrace);
            }

            return string.Empty;
        }
    }


}
