using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using OfficeOpenXml;
using Selise.Ecap.Entities.BaseEntity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceSupplierMigrationService : IRiqsInterfaceSupplierMigrationService
    {
        private readonly IPraxisFileService _fileService;
        private readonly ILogger<RiqsInterfaceSupplierMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IStorageDataService _storageDataService;
        private readonly INotificationService _notificationService;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IProcessUserData _processUserDataService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new();

        public RiqsInterfaceSupplierMigrationService(
         ILogger<RiqsInterfaceSupplierMigrationService> logger,
         IRepository repository,
         IPraxisFileService fileService,
         IStorageDataService storageDataService,
         INotificationService notificationService,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ISecurityContextProvider securityContextProvider,
         IServiceClient serviceClient,
         IProcessUserData processUserDataService,
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
            _processUserDataService = processUserDataService;
            _uilmResourceKeyService = uilmResourceKeyService;
        }

        public async Task<string> ProcessUploadSupplierData(UplaodSupplierInterfaceDataCommand command)
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
                    _logger.LogError("Error occurred : FIle steam null in {ClassName} ", nameof(DownloadFileStreamFromFileId));
                    return string.Empty;
                }

                using (fileStream)
                {
                    _logger.LogInformation("Enter {ClassName} with payload: {Payload}", nameof(ProcessUploadSupplierData), JsonConvert.SerializeObject(command));

                    var interfaceSummary = await PrepareSupplierInterfaceSummery(fileStream, command);

                    if (interfaceSummary != null)
                    {
                        interfaceSummary.ClientId = command.ClientId;
                        _repository.Save(interfaceSummary);

                        await PublishProcessMigrationCompletedEvent(command);
                        return interfaceSummary.ItemId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} ", nameof(ProcessUploadSupplierData));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        private async Task<Stream> DownloadFileStreamFromFileId(string fileId)
        {
            var file = await _fileService.GetFileInfoFromStorage(fileId);
            return await Task.Run(() => _storageDataService.GetFileContentStream(file?.Url));
        }

        private async Task<RiqsSupplierInterfaceMigrationSummary> PrepareSupplierInterfaceSummery(Stream fileStream, UplaodSupplierInterfaceDataCommand command)
        {
            if (fileStream == null)
            {
                _logger.LogError("Error occurred: File stream is null in {MethodName}", nameof(PrepareSupplierInterfaceSummery));
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
                    var supplierList = new List<TempSupplierInterfacePastData>();

                    command.IsUpdate = worksheet.Cells[1, 1].Text.Trim() == "Supplier ID";
                    int rowIndex = command.IsUpdate ? 2 : 3;

                    for (int row = rowIndex; row <= rowCount; row++)
                    {
                        try
                        {
                            var supplier = MapRowToSupplier(worksheet, row, command);
                            if (supplier == null) continue;

                            supplierList.Add(supplier);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error occurred in {ClassName}.", nameof(PrepareSupplierInterfaceSummery));
                            _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                        }
                    }

                    if (supplierList != null && supplierList.Any())
                    {
                        var collection = _ecapRepository
                           .GetTenantDataContext()
                           .GetCollection<BsonDocument>("TempSupplierInterfacePastDatas");

                        var bulkOperations = supplierList.Select(supplier =>
                        {
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", supplier.ItemId);
                            var update = supplier.ToBsonDocument();
                            return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                        }).ToList();

                        var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);
                    }

                    var summary = new RiqsSupplierInterfaceMigrationSummary
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
                _logger.LogError("Error occurred in {ClassName}.", nameof(PrepareSupplierInterfaceSummery));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
            return null;
        }

        private TempSupplierInterfacePastData MapRowToSupplier(ExcelWorksheet worksheet, int row, UplaodSupplierInterfaceDataCommand command)
        {
            var newRowList = new List<string>()
            {
                "name", "lastName", "streetNumber_Address","zipNumber_Address","country_Address", 
                "customerNumber", "valueaddedTaxNumber", 
                "streetNumber_billingAddress","zipNumber_billingAddress","country_billingAddress",
                "name_CP1", "email_CP1", "phoneNumberCode_CP1", "phoneNumber_CP1", "position_CP1",
                "name_CP2", "email_CP2", "phoneNumberCode_CP2", "phoneNumber_CP2", "position_CP2",
                "name_CP3", "email_CP3", "phoneNumberCode_CP3", "phoneNumber_CP3", "position_CP3"
            };

            var updateRowList = new List<string>()
            {
                "SupplierId", "name", "address", "customerNumber", "valueaddedTaxNumber", "billingAddress",
                "name_CP1", "email_CP1", "phoneNumber_CP1", "position_CP1",
                "name_CP2", "email_CP2", "phoneNumber_CP2", "position_CP2",
                "name_CP3", "email_CP3", "phoneNumber_CP3", "position_CP3"
            };

            var rowData = new Dictionary<string, string>();

            // Choose the correct list based on update mode
            var selectedList = command.IsUpdate ? updateRowList : newRowList;

            if (!command.IsUpdate)
            {
                rowData["SupplierId"] = Guid.NewGuid().ToString();
            }

            // Loop through and collect data
            for (int i = 0; i < selectedList.Count; i++)
            {
                string fieldName = selectedList[i];
                string cellValue = worksheet.Cells[row, i + 1].Text.Trim(); // Excel is 1-based indexing
                rowData[fieldName] = cellValue;
            }

            string SupplierId = GetSafeValue(rowData, "SupplierId");
            string name = GetSafeValue(rowData, "name");
            string lastName = GetSafeValue(rowData, "lastName");

            if (!command.IsUpdate && !string.IsNullOrEmpty(lastName))
            {
                name = name + " " + lastName;
            }

            string address = GetSafeValue(rowData, "address");

            string streetNumber_Address = GetSafeValue(rowData, "streetNumber_Address");
            string zipNumber_Address = GetSafeValue(rowData, "zipNumber_Address");
            string country_Address = GetSafeValue(rowData, "country_Address");

            if (!command.IsUpdate)
            {
                address = streetNumber_Address + " " + zipNumber_Address + " " + " " + country_Address;
            }


            string customerNumber = GetSafeValue(rowData, "customerNumber");
            string valueaddedTaxNumber = GetSafeValue(rowData, "valueaddedTaxNumber");
            string billingAddress = GetSafeValue(rowData, "billingAddress");

            string streetNumber_billingAddress = GetSafeValue(rowData, "streetNumber_billingAddress");
            string zipNumber_billingAddress = GetSafeValue(rowData, "zipNumber_billingAddress");
            string country_billingAddress = GetSafeValue(rowData, "country_billingAddress");

            if (!command.IsUpdate)
            {
                billingAddress = streetNumber_billingAddress + " " + zipNumber_billingAddress + " " + " " + country_billingAddress;
            }

            string name_CP1 = GetSafeValue(rowData, "name_CP1");
            string email_CP1 = GetSafeValue(rowData, "email_CP1");
            string phoneNumberCode_CP1 = GetSafeValue(rowData, "phoneNumberCode_CP1");
            string phoneNumber_CP1 = GetSafeValue(rowData, "phoneNumber_CP1");
            phoneNumber_CP1 = !string.IsNullOrEmpty(phoneNumberCode_CP1) ? phoneNumberCode_CP1 + phoneNumber_CP1 : phoneNumber_CP1;
            string position_CP1 = GetSafeValue(rowData, "position_CP1");

            string name_CP2 = GetSafeValue(rowData, "name_CP2");
            string email_CP2 = GetSafeValue(rowData, "email_CP2");
            string phoneNumberCode_CP2 = GetSafeValue(rowData, "phoneNumberCode_CP2");
            string phoneNumber_CP2 = GetSafeValue(rowData, "phoneNumber_CP2");
            phoneNumber_CP2 = !string.IsNullOrEmpty(phoneNumberCode_CP2) ? phoneNumberCode_CP2 + phoneNumber_CP2 : phoneNumber_CP2;
            string position_CP2 = GetSafeValue(rowData, "position_CP2");

            string name_CP3 = GetSafeValue(rowData, "name_CP3");
            string email_CP3 = GetSafeValue(rowData, "email_CP3");
            string phoneNumberCode_CP3 = GetSafeValue(rowData, "phoneNumberCode_CP3");
            string phoneNumber_CP3 = GetSafeValue(rowData, "phoneNumber_CP3");
            phoneNumber_CP3 = !string.IsNullOrEmpty(phoneNumberCode_CP3) ? phoneNumberCode_CP3 + phoneNumber_CP3 : phoneNumber_CP3;
            string position_CP3 = GetSafeValue(rowData, "position_CP3");

            if (string.IsNullOrEmpty(name)) return null;

            var supplier = new TempSupplierInterfacePastData
            {
                MigrationSummeryId = command.MigrationSummaryId,
                ItemId = SupplierId,
                ClientId = command.ClientId,
                Name = name,
                Email = email_CP1,
                ContactPerson = name_CP1,
                PhoneNumber = phoneNumber_CP1,
                Address = new PraxisAddress { AddressLine1 = address },
                BillingAddress = new PraxisAddress { AddressLine1 = billingAddress },
                CustomerNumber = customerNumber,
                VatNumber = valueaddedTaxNumber,
                IsBillingAddressDifferent = !string.IsNullOrEmpty(billingAddress) ? true : false,
                SupplierContactPersons = new List<PraxisSupplierContactPerson>
                {
                    new PraxisSupplierContactPerson
                    {
                        ContactId = Guid.NewGuid().ToString(),
                        Name = name_CP1,
                        Email = email_CP1,
                        PhoneNumber = phoneNumber_CP1,
                        Position = position_CP1,
                        IsPrimaryContact = true
                    },
                    new PraxisSupplierContactPerson
                    {
                        ContactId = Guid.NewGuid().ToString(),
                        Name = name_CP2,
                        Email = email_CP2,
                        PhoneNumber = phoneNumber_CP2,
                        Position = position_CP2,
                        IsPrimaryContact = false
                    },
                    new PraxisSupplierContactPerson
                    {
                        ContactId = Guid.NewGuid().ToString(),
                        Name = name_CP3,
                        Email = email_CP3,
                        PhoneNumber = phoneNumber_CP3,
                        Position = position_CP3,
                        IsPrimaryContact = false
                    }
                },
            };

            var praxisClient = _repository.GetItemAsync<PraxisClient>(p => p.ItemId == command.ClientId).GetAwaiter().GetResult();
            var additionalInfos = praxisClient?.AdditionalInfos?.ToList() ?? new List<ClientAdditionalInfo>();
            var existingInfo = additionalInfos.FirstOrDefault(info => info.ItemId == SupplierId);

            if (command.IsUpdate && existingInfo != null)
            {
                supplier.CategoryKey = existingInfo.CategoryKey;
                supplier.CategoryName = existingInfo.CategoryName;
                supplier.IsExist = true;
            }

            return supplier;
        }

        private static string GetSafeValue(Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private async Task PublishProcessMigrationCompletedEvent(
         UplaodSupplierInterfaceDataCommand command
         )
        {

            await _notificationService.GetCommonSubscriptionNotification(
                        true,
                        command.NotificationSubscriptionId,
                        command.ActionName,
                        command.Context
                    );
        }

        public async Task<RiqsSupplierInterfaceMigrationSummary> GetSupplierInterfaceSummery(GetSupplierInterfaceSummeryQuery query)
        {
            if (string.IsNullOrEmpty(query.MigrationSummaryId))
            {
                return null;
            }

            try
            {
                var migrationSummery = await _repository.GetItemAsync<RiqsSupplierInterfaceMigrationSummary>(x => x.ItemId == query.MigrationSummaryId);

                if (migrationSummery == null) return null;
                int skipCount = (query.PageNumber - 1) * query.PageSize;
                long totalRecord = 0;
                var queryFilter = Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", query.MigrationSummaryId);

                var collections = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<BsonDocument>("TempSupplierInterfacePastDatas")
                .Aggregate()
                .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();
                collections = collections.Skip(skipCount).Limit(query.PageSize);


                if (totalRecord > 0)
                {
                    var results = collections.ToEnumerable().Select(document => BsonSerializer.Deserialize<TempSupplierInterfacePastData>(document));
                    var additionalInfos = results.ToList();

                    return new RiqsSupplierInterfaceMigrationSummary
                    {
                        ItemId = query.MigrationSummaryId,
                        IsDraft = migrationSummery.IsDraft,
                        ClientId = migrationSummery.ClientId,
                        OrganizationId = migrationSummery.OrganizationId,
                        AdditionalInfos = additionalInfos,
                        IsUpdate = migrationSummery.IsUpdate,
                        TotalRecord = totalRecord
                    };
                }

                // Return an empty response if no results found
                return new RiqsSupplierInterfaceMigrationSummary
                {
                    ItemId = query.MigrationSummaryId,
                    IsDraft = false,
                    AdditionalInfos = new List<TempSupplierInterfacePastData>(),
                    TotalRecord = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(GetSupplierInterfaceSummery), ex.Message, ex.StackTrace);

            }

            return null;

        }

        public async Task ProcessUploadSupplierAdditionalData(UpdateSupplierInterfaceAdditioanalDataCommand command)
        {
            if (string.IsNullOrEmpty(command.MigrationSummaryId) || !command.SupplierIds.Any()) return;

            try
            {
                var filter = Builders<BsonDocument>.Filter.And(
                  Builders<BsonDocument>.Filter.In("_id", command.SupplierIds),
                  Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", command.MigrationSummaryId)
              );


                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempSupplierInterfacePastDatas");
                var documents = await collection.Find(filter).ToListAsync();

                var updates = new List<UpdateOneModel<BsonDocument>>();
                var createSupplierIds = new List<string>();
                foreach (var document in documents)
                {
                    var update = Builders<BsonDocument>.Update
                      .Set("CategoryKey", command.CategoryKey)
                      .Set("CategoryName", command.CategoryName)
                      .Set("IsExist", true);

                    updates.Add(new UpdateOneModel<BsonDocument>(
                        Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                        update
                    ));
                    createSupplierIds.Add(document["_id"].AsString);
                }

                if (updates.Count > 0 && createSupplierIds.Count > 0)
                {
                    await collection.BulkWriteAsync(updates);

                    var updateCollection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempSupplierInterfacePastDatas");
                    var createDocuments = await updateCollection.Find(filter).ToListAsync();

                    var supplierToSave = createDocuments
                        .Select(doc => BsonSerializer.Deserialize<SupplierInfo>(doc))
                        .Where(supplier => command.SupplierIds.Contains(supplier.ItemId))
                        .ToList();

                    var praxisClient = await _repository.GetItemAsync<PraxisClient>(p => p.ItemId == command.ClientId);

                    foreach (var supplier in supplierToSave)
                    {
                        var additionalInfos = praxisClient?.AdditionalInfos?.ToList() ?? new List<ClientAdditionalInfo>();

                        var existingInfo = additionalInfos.FirstOrDefault(info => info.ItemId == supplier.ItemId);

                        if (existingInfo != null)
                        {
                            MapSupplierToClientInfo(existingInfo, supplier);
                        }
                        else
                        {
                            var newInfo = new ClientAdditionalInfo();
                            MapSupplierToClientInfo(newInfo, supplier);
                            additionalInfos.Add(newInfo);
                        }

                        praxisClient.AdditionalInfos = additionalInfos;

                        await _repository.UpdateAsync(c => c.ItemId == praxisClient.ItemId, praxisClient);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(ProcessUploadSupplierAdditionalData), ex.Message, ex.StackTrace);
            }
        }

        private void MapSupplierToClientInfo(ClientAdditionalInfo clientInfo, SupplierInfo supplier)
        {
            clientInfo.ItemId = supplier.ItemId;
            clientInfo.CategoryKey = supplier.CategoryKey;
            clientInfo.CategoryName = supplier.CategoryName;
            clientInfo.Name = supplier.Name;
            clientInfo.Address = supplier.Address;
            clientInfo.ContactPerson = supplier.ContactPerson;
            clientInfo.PhoneNumber = supplier.PhoneNumber;
            clientInfo.Email = supplier.Email;
            clientInfo.VatNumber = supplier.VatNumber;
            clientInfo.CustomerNumber = supplier.CustomerNumber;
            clientInfo.IsBillingAddressDifferent = supplier.IsBillingAddressDifferent;
            clientInfo.BillingAddress = supplier.BillingAddress;
            clientInfo.ContactPersons = supplier.ContactPersons;
            clientInfo.FileAttachments = supplier.FileAttachments;
            clientInfo.SupplierContactPersons = supplier.SupplierContactPersons;
        }

        public async Task<string> ProcessDownloadSupplierData(DownloadSupplierInterfaceDataCommand command)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", command.ClientId);

                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisClients");

                var document = await collection.Find(filter).FirstOrDefaultAsync();

                if (document != null)
                {
                    var praxisClient = BsonSerializer.Deserialize<PraxisClient>(document);

                    _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(GetDownloadSupplierDataLanguageKeys(), command.Language);

                    if (praxisClient != null && praxisClient?.AdditionalInfos != null && praxisClient.AdditionalInfos.Any())
                    {
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                        using var package = new ExcelPackage();
                        var worksheet = package.Workbook.Worksheets.Add("Supplier-Update-List");

                        // Add Header
                        worksheet.Cells[1, 1].Value = _translatedStringsAsDictionary["Supplier ID"];
                        worksheet.Cells[1, 2].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_NAME"];
                        worksheet.Cells[1, 3].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_EMAIL"];
                        worksheet.Cells[1, 4].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"];
                        worksheet.Cells[1, 5].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_PHONENUMBER"];
                        worksheet.Cells[1, 6].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_ADDRESS"];
                        worksheet.Cells[1, 7].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_BILLINGADDRESS"];
                        worksheet.Cells[1, 8].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CUSTOMERNUMBER"];
                        worksheet.Cells[1, 9].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_VALUEADDEDTAXNUMBER"];
                        worksheet.Cells[1, 10].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_POSITION"];

                        worksheet.Cells[1, 1].Value = _translatedStringsAsDictionary["Supplier ID"];
                        worksheet.Cells[1, 2].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_NAME"];
                        worksheet.Cells[1, 3].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_ADDRESS"];
                        worksheet.Cells[1, 4].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CUSTOMERNUMBER"];
                        worksheet.Cells[1, 5].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_VALUEADDEDTAXNUMBER"];
                        worksheet.Cells[1, 6].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_BILLINGADDRESS"];

                        // cp-1
                        worksheet.Cells[1, 7].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] + 
                            " 1" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_NAME"];

                        worksheet.Cells[1, 8].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 1" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_EMAIL"];

                        worksheet.Cells[1, 9].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 1" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_PHONENUMBER"];

                        worksheet.Cells[1, 10].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 1" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_POSITION"];

                        // cp-2
                        worksheet.Cells[1, 11].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 2" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_NAME"];

                        worksheet.Cells[1, 12].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 2" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_EMAIL"];

                        worksheet.Cells[1, 13].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 2" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_PHONENUMBER"];

                        worksheet.Cells[1, 14].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 2" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_POSITION"];

                        // cp-3
                        worksheet.Cells[1, 15].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 3" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_NAME"];

                        worksheet.Cells[1, 16].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 3" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_EMAIL"];

                        worksheet.Cells[1, 17].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 3" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_PHONENUMBER"];

                        worksheet.Cells[1, 18].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_CONTACTPERSON"] +
                            " 3" + Environment.NewLine + _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.SUPPLIER_POSITION"];

                        // Set column widths
                        worksheet.Column(1).Width = 40;  // Supplier ID
                        worksheet.Column(2).Width = 40;  // Name
                        worksheet.Column(3).Width = 46;  // Address
                        worksheet.Column(4).Width = 20;  // Customer Number
                        worksheet.Column(5).Width = 20;  // Value Added Tax Number
                        worksheet.Column(6).Width = 46;  // Billing Address

                        worksheet.Column(7).Width = 40;  // Name_CP-1
                        worksheet.Column(8).Width = 30;  // Email_CP-1
                        worksheet.Column(9).Width = 20; // Phone_Number_CP-1
                        worksheet.Column(10).Width = 30; // Position_CP-1

                        worksheet.Column(11).Width = 40;  // Name_CP-2
                        worksheet.Column(12).Width = 30;  // Email_CP-2
                        worksheet.Column(13).Width = 20; // Phone_Number_CP-2
                        worksheet.Column(14).Width = 30; // Position_CP-2

                        worksheet.Column(15).Width = 40;  // Name_CP-3
                        worksheet.Column(16).Width = 30;  // Email_CP-3
                        worksheet.Column(17).Width = 20; // Phone_Number_CP-3
                        worksheet.Column(18).Width = 30; // Position_CP-3

                        worksheet.Row(1).Height = 30;

                        using (var range = worksheet.Cells[1, 1, 1, 18])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                            // Fill background color
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#DDEBF7"));

                            range.Style.WrapText = true;

                            var border = range.Style.Border;
                            border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }

                        int row = 2;
                        foreach (var clientInfo in praxisClient.AdditionalInfos)
                        {
                            worksheet.Cells[row, 1].Value = clientInfo.ItemId;
                            worksheet.Cells[row, 2].Value = clientInfo?.Name;
                            worksheet.Cells[row, 3].Value = clientInfo?.Address?.AddressLine1 ?? string.Empty;
                            worksheet.Cells[row, 4].Value = clientInfo?.CustomerNumber ?? string.Empty;
                            worksheet.Cells[row, 5].Value = clientInfo?.VatNumber ?? string.Empty;
                            worksheet.Cells[row, 6].Value = clientInfo?.BillingAddress?.AddressLine1 ?? string.Empty;

                            worksheet.Cells[row, 7].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(0)?.Name ?? string.Empty;
                            worksheet.Cells[row, 8].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(0)?.Email ?? string.Empty;
                            worksheet.Cells[row, 9].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(0)?.PhoneNumber ?? string.Empty;
                            worksheet.Cells[row, 10].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(0)?.Position ?? string.Empty;

                            worksheet.Cells[row, 11].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(1)?.Name ?? string.Empty;
                            worksheet.Cells[row, 12].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(1)?.Email ?? string.Empty;
                            worksheet.Cells[row, 13].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(1)?.PhoneNumber ?? string.Empty;
                            worksheet.Cells[row, 14].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(1)?.Position ?? string.Empty;

                            worksheet.Cells[row, 15].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(2)?.Name ?? string.Empty;
                            worksheet.Cells[row, 16].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(2)?.Email ?? string.Empty;
                            worksheet.Cells[row, 17].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(2)?.PhoneNumber ?? string.Empty;
                            worksheet.Cells[row, 18].Value = clientInfo?.SupplierContactPersons?.ElementAtOrDefault(2)?.Position ?? string.Empty;
                            row++;
                        }

                        var fileId = Guid.NewGuid().ToString();
                        string fileName = $"Supplier-Update-List.xlsx";
                        var success = await _storageDataService.UploadFileAsync(fileId, fileName, package.GetAsByteArray());
                        if (success) return fileId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName}.", nameof(ProcessDownloadSupplierData));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

    }

}
