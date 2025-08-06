using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using OfficeOpenXml;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.LIM;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule;
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
    public class RiqsInterfaceUserMigrationService : IRiqsInterfaceUserMigrationService
    {
        private readonly IPraxisFileService _fileService;
        private readonly ILogger<RiqsInterfaceUserMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IStorageDataService _storageDataService;
        private readonly INotificationService _notificationService;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IServiceClient _serviceClient;
        private readonly IProcessUserData _processUserDataService;
        private readonly IUilmResourceKeyService _uilmResourceKeyService;
        private Dictionary<string, string> _translatedStringsAsDictionary = new();

        public RiqsInterfaceUserMigrationService(
         ILogger<RiqsInterfaceUserMigrationService> logger,
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

        public async Task<string> ProcessUploadUserData(UplaodUserInterfaceDataCommand command)
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
                    _logger.LogInformation("Enter {ClassName} with payload: {Payload}", nameof(ProcessUploadUserData), JsonConvert.SerializeObject(command));

                    var interfaceSummary = await PrepareUserInterfaceSummery(fileStream, command);

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
                _logger.LogError("Error occurred in {ClassName} ", nameof(ProcessUploadUserData));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        private async Task<Stream> DownloadFileStreamFromFileId(string fileId)
        {
            var file = await _fileService.GetFileInfoFromStorage(fileId);
            return await Task.Run(() => _storageDataService.GetFileContentStream(file?.Url));
        }

        private async Task<RiqsUserInterfaceMigrationSummary> PrepareUserInterfaceSummery(Stream fileStream, UplaodUserInterfaceDataCommand command)
        {
            if (fileStream == null)
            {
                _logger.LogError("Error occurred: File stream is null in {MethodName}", nameof(PrepareUserInterfaceSummery));
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
                    var userList = new List<TempPraxisUserInterfacePastData>();

                    command.IsUpdate = worksheet.Cells[1, 1].Text.Trim() == "User ID";

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var user = MapRowToUser(worksheet, row, command);
                            if (user == null) continue;

                            userList.Add(user);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Error occurred in {ClassName}.", nameof(PrepareUserInterfaceSummery));
                            _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
                        }
                    }

                    if (userList != null && userList.Any())
                    {
                        var collection = _ecapRepository
                           .GetTenantDataContext()
                           .GetCollection<BsonDocument>("TempPraxisUserInterfacePastDatas");

                        var bulkOperations = userList.Select(equipment =>
                        {
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", equipment.ItemId);
                            var update = equipment.ToBsonDocument();
                            return new ReplaceOneModel<BsonDocument>(filter, update) { IsUpsert = true };
                        }).ToList();

                        var bulkWriteResult = await collection.BulkWriteAsync(bulkOperations);
                    }

                    var summary = new RiqsUserInterfaceMigrationSummary
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
                _logger.LogError("Error occurred in {ClassName}.", nameof(PrepareUserInterfaceSummery));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
            return null;
        }

        private TempPraxisUserInterfacePastData MapRowToUser(ExcelWorksheet worksheet, int row, UplaodUserInterfaceDataCommand command)
        {
            string userId = command.IsUpdate ? worksheet.Cells[row, 1].Text.Trim() : string.Empty;
            string email = worksheet.Cells[row, command.IsUpdate ? 2 : 1].Text.Trim();
            string fname = worksheet.Cells[row, command.IsUpdate ? 3 : 2].Text.Trim();
            string lname = worksheet.Cells[row, command.IsUpdate ? 4 : 3].Text.Trim();
            if (string.IsNullOrEmpty(email)) return null;

            var user = new TempPraxisUserInterfacePastData
            {
                MigrationSummeryId = command.MigrationSummaryId,
                ItemId = Guid.NewGuid().ToString(),
                UserId = Guid.NewGuid().ToString(),
                Email = email,
                FirstName = fname,
                LastName = lname,
                DisplayName = fname + " " + lname,
                DateOfJoining = DateTime.Now,
                Language = "en-US",
                AdditionalInfo = new List<PraxisUserAdditionalInfo>(),
                Tags = new string[] { "Is-Valid-PraxisUser" }
            };

            var praxisUser = _repository.GetItem<PraxisUser>(p => p.Email == email);

            if (praxisUser != null)
            {
                user.ItemId = praxisUser.UserId;
                user.UserId = praxisUser.UserId;
                user.ClientList = praxisUser.ClientList;
                user.MotherTongue = praxisUser.MotherTongue;
                user.Language = praxisUser.Language;
                user.OtherLanguage = praxisUser.OtherLanguage;
                user.AdditionalInfo = praxisUser.AdditionalInfo;
                user.IsExist = true;
            }

            var image = new PraxisImage
            {
                FileId = "",
                Thumbnails = new List<PraxisImageThumbnail>(),
                CreatedOn = null,
                IsUploadedFromWeb = true,
                FileName = null,
            };

            user.Image = image;

            string cellText = worksheet.Cells[row, command.IsUpdate ? 5 : 4].Text;

            if (!string.IsNullOrEmpty(cellText))
            {
                string[] parts = cellText.Split('.');
                string dateString = string.Join("-", parts);

                if (DateTime.TryParse(dateString, out DateTime dateOfBirth))
                {
                    user.DateOfBirth = dateOfBirth;
                }
            }

            string nationality = worksheet.Cells[row, command.IsUpdate ? 6 : 5].Text.Trim();
            user.Nationality = nationality;

            string academicTitle = worksheet.Cells[row, command.IsUpdate ? 7 : 6].Text.Trim();
            user.AcademicTitle = academicTitle;

            string workLoad = worksheet.Cells[row, command.IsUpdate ? 8 : 7].Text.Trim();
            user.WorkLoad = workLoad;

            string phone = worksheet.Cells[row, command.IsUpdate ? 9 : 8].Text.Trim();
            user.Phone = phone;

            string GLNNumber = worksheet.Cells[row, command.IsUpdate ? 10 : 9].Text.Trim();
            user.GlnNumber = GLNNumber;

            string ZSRNumber = worksheet.Cells[row, command.IsUpdate ? 11 : 10].Text.Trim();
            user.ZsrNumber = ZSRNumber;

            string KNumber = worksheet.Cells[row, command.IsUpdate ? 12 : 11].Text.Trim();
            user.KNumber = KNumber;

            string additionalGroup = worksheet.Cells[row, command.IsUpdate ? 13 : 12].Text.Trim();
            var additionalInfo = new List<PraxisUserAdditionalInfo>() { new PraxisUserAdditionalInfo
            {
                ItemId = null,
                InfoTitle = null,
                ClientId = null,
                AdditionalGroups = new List<PraxisUserAdditionalInfoGroup>()
                {
                    new PraxisUserAdditionalInfoGroup { Files = new List<PraxisDocument>(), GroupTitle = additionalGroup ?? string.Empty } 
                }
            }};

            if (command.IsUpdate)
            {
                if(user.AdditionalInfo != null && user.AdditionalInfo.Count() > 0)
                {
                    user.AdditionalInfo.ToList().AddRange(additionalInfo);
                }
                else { user.AdditionalInfo = additionalInfo; }
            }
            else { user.AdditionalInfo = additionalInfo; }

            string remarks = worksheet.Cells[row, command.IsUpdate ? 14 : 13].Text.Trim();
            user.Remarks = remarks;

            return user;
        }

        private async Task PublishProcessMigrationCompletedEvent(
         UplaodUserInterfaceDataCommand command
         )
        {

            await _notificationService.GetCommonSubscriptionNotification(
                        true,
                        command.NotificationSubscriptionId,
                        command.ActionName,
                        command.Context
                    );
        }

        public async Task<RiqsUserInterfaceMigrationSummary> GetUserMigrationSummery(GetUserInterfaceSummeryQuery query)
        {
            if (string.IsNullOrEmpty(query.MigrationSummaryId))
            {
                return null;
            }

            try
            {
                var migrationSummery = await _repository.GetItemAsync<RiqsUserInterfaceMigrationSummary>(x => x.ItemId == query.MigrationSummaryId);

                if (migrationSummery == null) return null;
                int skipCount = (query.PageNumber - 1) * query.PageSize;
                long totalRecord = 0;
                var queryFilter = Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", query.MigrationSummaryId);

                var collections = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<BsonDocument>("TempPraxisUserInterfacePastDatas")
                .Aggregate()
                .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();
                collections = collections.Skip(skipCount).Limit(query.PageSize);


                if (totalRecord > 0)
                {


                    var results = collections.ToEnumerable().Select(document => BsonSerializer.Deserialize<TempPraxisUserInterfacePastData>(document));
                    var users = results.ToList();
                    foreach (var user in users)
                    {
                        user.IdsAllowedToRead = null;
                        user.IdsAllowedToUpdate = null;
                        user.IdsAllowedToDelete = null;
                        user.RolesAllowedToRead = null;
                        user.RolesAllowedToUpdate = null;
                        user.RolesAllowedToDelete = null;
                    }

                    return new RiqsUserInterfaceMigrationSummary
                    {
                        ItemId = query.MigrationSummaryId,
                        IsDraft = migrationSummery.IsDraft,
                        ClientId = migrationSummery.ClientId,
                        OrganizationId = migrationSummery.OrganizationId,
                        PraxisUsers = users,
                        IsUpdate = migrationSummery.IsUpdate,
                        TotalRecord = totalRecord
                    };
                }

                // Return an empty response if no results found
                return new RiqsUserInterfaceMigrationSummary
                {
                    ItemId = query.MigrationSummaryId,
                    IsDraft = false,
                    PraxisUsers = new List<TempPraxisUserInterfacePastData>(),
                    TotalRecord = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(GetUserMigrationSummery), ex.Message, ex.StackTrace);

            }

            return null;

        }

        public async Task ProcessUploadUserAdditionalData(UpdateUserInterfaceAdditioanalDataCommand command)
        {
            if (string.IsNullOrEmpty(command.MigrationSummaryId) || !command.UserIds.Any()) return;

            try
            {
                var filter = Builders<BsonDocument>.Filter.And(
                  Builders<BsonDocument>.Filter.In("_id", command.UserIds),
                  Builders<BsonDocument>.Filter.Eq("MigrationSummeryId", command.MigrationSummaryId)
              );


                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempPraxisUserInterfacePastDatas");
                var documents = await collection.Find(filter).ToListAsync();

                var updates = new List<UpdateOneModel<BsonDocument>>();
                var createUsrIds = new List<string>();
                foreach (var document in documents)
                {
                    var additionalInfoArray = document.GetValue("AdditionalInfo", new BsonArray()).AsBsonArray;
                    var updatedAdditionalInfo = UpdateAdditionalInfoArray(additionalInfoArray, command.AdditionalInfo);

                    var update = Builders<BsonDocument>.Update
                      .Set("ClientList", command.ClientList)
                      .Set("MotherTongue", command.MotherTongue)
                      .Set("OtherLanguage", command.OtherLanguage)
                      .Set("ClientId", command.ClientId)
                      .Set("ClientName", command.ClientName)
                      .Set("Roles", command.Roles)
                      .Set("AdditionalInfo", updatedAdditionalInfo)
                      .Set("IsExist", true);

                    updates.Add(new UpdateOneModel<BsonDocument>(
                        Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                        update
                    ));
                    createUsrIds.Add(document["_id"].AsString);
                }


                if (updates.Count > 0 && createUsrIds.Count > 0)
                {
                    await collection.BulkWriteAsync(updates);

                    var updateCollection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("TempPraxisUserInterfacePastDatas");
                    var createDocuments = await updateCollection.Find(filter).ToListAsync();

                    var praxisUserToSave = createDocuments
                        .Select(doc => BsonSerializer.Deserialize<PraxisUser>(doc))
                        .Where(user => command.UserIds.Contains(user.ItemId))
                        .ToList();

                    foreach(var praxisUser in praxisUserToSave)
                    {
                        var processUserCreateUpdateCommand = new ProcessUserCreateUpdateCommand
                        {
                            PraxisUserInformation = praxisUser,
                            FileData = null,
                            NotificationSubscriptionId = Guid.NewGuid().ToString(),
                        };
                        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), processUserCreateUpdateCommand);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in {MethodName}: {Message} | StackTrace: {StackTrace}",
                    nameof(ProcessUploadUserAdditionalData), ex.Message, ex.StackTrace);
            }
        }

        public BsonArray UpdateAdditionalInfoArray(BsonArray additionalInfoArray, AdditionalInfo additionalInfo)
        {
            foreach (var item in additionalInfoArray)
            {
                var doc = item.AsBsonDocument;

                if (doc.Contains("ItemId") && doc["ItemId"].IsBsonNull)
                {
                    doc["ItemId"] = additionalInfo.ItemId;
                }
                if (doc.Contains("InfoTitle") && doc["InfoTitle"].IsBsonNull)
                {
                    doc["InfoTitle"] = additionalInfo.Title;
                }
                if (doc.Contains("ClientId") && doc["ClientId"].IsBsonNull)
                {
                    doc["ClientId"] = additionalInfo.ClientId;
                }
            }

            return additionalInfoArray;
        }

        public async Task<string> ProcessDownloadUserData(DownloadUserInterfaceDataCommand command)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("ClientId", command.ClientId);

                var collection = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>("PraxisUsers");

                var documents = await collection.Find(filter).ToListAsync();

                var userList = documents.Select(doc => BsonSerializer.Deserialize<PraxisUser>(doc)).ToList();

                _translatedStringsAsDictionary = _uilmResourceKeyService.GetResourceValueByKeyName(GetDownloadUserDataLanguageKeys(), command.Language);

                if (userList != null && userList.Count > 0)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("User-Update-List");

                    // Add Header
                    worksheet.Cells[1, 1].Value = _translatedStringsAsDictionary["User ID"];
                    worksheet.Cells[1, 2].Value = _translatedStringsAsDictionary["Email"];
                    worksheet.Cells[1, 3].Value = _translatedStringsAsDictionary["FIRST_NAME"];
                    worksheet.Cells[1, 4].Value = _translatedStringsAsDictionary["LAST_NAME"];
                    worksheet.Cells[1, 5].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.USER_DATE_OF_BIRTH"];
                    worksheet.Cells[1, 6].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.USER_NATIONALITY"];
                    worksheet.Cells[1, 7].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.USER_ACADEMIC_TITLE"];
                    worksheet.Cells[1, 8].Value = _translatedStringsAsDictionary["APP_USER_MANAGEMENT.WORKLOAD"];
                    worksheet.Cells[1, 9].Value = _translatedStringsAsDictionary["APP_INRTERFACE_MANAGER.USER_PHONE"];
                    worksheet.Cells[1, 10].Value = _translatedStringsAsDictionary["APP_USER_MANAGEMENT.GLN_NUMBER"];
                    worksheet.Cells[1, 11].Value = _translatedStringsAsDictionary["APP_USER_MANAGEMENT.ZSR_NUMBER"];
                    worksheet.Cells[1, 12].Value = _translatedStringsAsDictionary["APP_USER_MANAGEMENT.K_NUMBER"];
                    worksheet.Cells[1, 13].Value = _translatedStringsAsDictionary["APP_USER_MANAGEMENT.ADDITIONAL_GROUP"];
                    worksheet.Cells[1, 14].Value = _translatedStringsAsDictionary["Remarks"];

                    // Set column widths
                    worksheet.Column(1).Width = 40;  // User ID
                    worksheet.Column(2).Width = 40;  // Email
                    worksheet.Column(3).Width = 46;  // First-Name
                    worksheet.Column(4).Width = 20;  // Last-Name
                    worksheet.Column(5).Width = 20;  // Date Of Birth
                    worksheet.Column(6).Width = 22;  // Nationality
                    worksheet.Column(7).Width = 18;  // Academic Title
                    worksheet.Column(8).Width = 25;  // Work Load
                    worksheet.Column(9).Width = 30; // Phone
                    worksheet.Column(10).Width = 20; // GLN Number
                    worksheet.Column(11).Width = 20; // ZSR Number
                    worksheet.Column(12).Width = 20; // K Number
                    worksheet.Column(13).Width = 30; // Additional Group
                    worksheet.Column(14).Width = 50; // Remarks

                    worksheet.Row(1).Height = 20;

                    using (var range = worksheet.Cells[1, 1, 1, 14])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.General;

                        // Fill background color
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#DDEBF7"));
                    }

                    int row = 2;
                    foreach (var user in userList)
                    {
                        worksheet.Cells[row, 1].Value = user?.UserId ?? string.Empty;
                        worksheet.Cells[row, 2].Value = user?.Email ?? string.Empty;
                        worksheet.Cells[row, 3].Value = user?.FirstName ?? string.Empty;
                        worksheet.Cells[row, 4].Value = user?.LastName ?? string.Empty;
                        var dateOfBirth = user?.DateOfBirth == null ? string.Empty : user.DateOfBirth.ToString("dd.MM.yyyy");
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "@";
                        worksheet.Cells[row, 5].Value = dateOfBirth ?? string.Empty;
                        worksheet.Cells[row, 6].Value = user?.Nationality ?? string.Empty;
                        worksheet.Cells[row, 7].Value = user?.AcademicTitle ?? string.Empty;
                        worksheet.Cells[row, 8].Value = user?.WorkLoad ?? string.Empty;
                        worksheet.Cells[row, 9].Value = user?.Phone ?? string.Empty;
                        worksheet.Cells[row, 10].Value = user?.GlnNumber ?? string.Empty;
                        worksheet.Cells[row, 11].Value = user?.ZsrNumber ?? string.Empty;
                        worksheet.Cells[row, 12].Value = user?.KNumber ?? string.Empty;
                        worksheet.Cells[row, 13].Value = user?.AdditionalInfo?.FirstOrDefault()?.AdditionalGroups?.FirstOrDefault()?.GroupTitle ?? string.Empty;
                        worksheet.Cells[row, 14].Value = user?.Remarks ?? string.Empty;
                        row++;
                    }

                    var fileId = Guid.NewGuid().ToString();
                    string fileName = $"User-Update-List.xlsx";
                    var success = await _storageDataService.UploadFileAsync(fileId, fileName, package.GetAsByteArray());
                    if (success) return fileId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName}.", nameof(ProcessDownloadUserData));
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

    }

}
