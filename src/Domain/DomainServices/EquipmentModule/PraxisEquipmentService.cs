using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisEquipmentService : IPraxisEquipmentService, IDeleteDataForClientInCollections
    {
        private const string ProcessGuideListingKey = "ProcessGuideListing";

        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ILogger<PraxisEquipmentService> _logger;
        private readonly IPraxisQrGeneratorService _praxisQrGeneratorService;
        private readonly ICommonUtilService _commonUtilService;
        private readonly IPraxisFileService _fileService;
        private readonly IDmsService _dmsService;
        private readonly IPraxisProcessGuideService _processGuideService;
        private readonly string _taskManagementServiceBaseUrl;
        private readonly IAuthUtilityService _authUtilityService;
        private readonly IServiceClient _serviceClient;
        private readonly IPraxisRoomService _praxisRoomService;

        public PraxisEquipmentService(
            IMongoSecurityService mongoSecurityService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IPraxisQrGeneratorService praxisQrGeneratorService,
            ILogger<PraxisEquipmentService> logger,
            ICommonUtilService commonUtilService,
            IPraxisFileService fileService,
            IDmsService dmsService,
            IPraxisProcessGuideService praxisProcessGuide,
            IConfiguration configuration,
            IAuthUtilityService authUtilityService,
            IServiceClient serviceClient,
            IPraxisRoomService praxisRoomService
        )
        {
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _ecapRepository = ecapRepository;
            _logger = logger;
            _praxisQrGeneratorService = praxisQrGeneratorService;
            _commonUtilService = commonUtilService;
            _fileService = fileService;
            _dmsService = dmsService;
            _processGuideService = praxisProcessGuide;
            _taskManagementServiceBaseUrl = configuration["TaskManagementServiceBaseUrl"];
            _authUtilityService = authUtilityService;
            _serviceClient = serviceClient;
            _praxisRoomService = praxisRoomService;
        }
        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            _logger.LogInformation("Entered AddRowLevelSecurity of PraxisEquipmentService");
            var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);

            permission.RolesAllowedToDelete.Add(clientAdminAccessRole);

            _mongoSecurityService.UpdateEntityReadWritePermission<PraxisEquipment>(permission);
        }

        public List<PraxisEquipment> GetAllPraxisPraxisEquipment()
        {
            throw new NotImplementedException();
        }

        public async Task<PraxisEquipment> GetPraxisEquipment(string itemId)
        {
            return await _repository.GetItemAsync<PraxisEquipment>(equipment => equipment.ItemId.Equals(itemId));
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }

        public void UpdatePraxisEquipment(string itemId)
        {
            throw new NotImplementedException();
        }

        public async Task<EntityQueryResponse<PraxisEquipment>> GetPraxisEquipments(string filter, string sort, int pageNumber, int pageSize)
        {
            return await Task.Run(() =>
            {
                FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                if (!string.IsNullOrEmpty(filter))
                {
                    queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);
                }

                var securityContext = _securityContextProvider.GetSecurityContext();

                queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                    PdsActionEnum.Read,
                    securityContext,
                    securityContext.Roles.ToList()
                );

                long totalRecord = 0;

                pageNumber += 1;
                var skip = pageSize * (pageNumber - 1);

                var collections = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>($"PraxisEquipments")
                    .Aggregate()
                    .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();

                if (!string.IsNullOrEmpty(sort))
                {
                    collections = collections.Sort(BsonDocument.Parse(sort));
                }

                collections = collections.Skip(skip).Limit(pageSize);

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisEquipment>(document));

                return new EntityQueryResponse<PraxisEquipment>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };

            });
        }

        public async Task<EntityQueryResponse<PraxisEquipment>> GetEquipmentsReportData(string filter, string sort)
        {
            return await Task.Run(() =>
            {
                FilterDefinition<BsonDocument> queryFilter = new BsonDocument();

                if (!string.IsNullOrEmpty(filter))
                {
                    queryFilter = BsonSerializer.Deserialize<BsonDocument>(filter);
                }

                var securityContext = _securityContextProvider.GetSecurityContext();

                queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                    PdsActionEnum.Read,
                    securityContext,
                    securityContext.Roles.ToList()
                );

                long totalRecord = 0;

                var collections = _ecapRepository
                    .GetTenantDataContext()
                    .GetCollection<BsonDocument>($"PraxisEquipments")
                    .Aggregate()
                    .Match(queryFilter);

                totalRecord = collections.ToEnumerable().Count();

                if (!string.IsNullOrEmpty(sort))
                {
                    collections = collections.Sort(BsonDocument.Parse(sort));
                }

                var results = collections.ToEnumerable()
                    .Select(document => BsonSerializer.Deserialize<PraxisEquipment>(document));

                return new EntityQueryResponse<PraxisEquipment>
                {
                    Results = results.ToList(),
                    TotalRecordCount = totalRecord
                };

            });
        }

        public async Task DeleteDataForClient(string clientId, string orgId = null)
        {
            _logger.LogInformation("Going to delete {Equipment} and {Maintenance} for client {ClientId}",
                nameof(PraxisEquipment), nameof(PraxisEquipmentMaintenance), clientId);
            try
            {
                var deleteTasks = new List<Task>
                {
                    _repository.DeleteAsync<PraxisEquipment>(equipment => equipment.ClientId.Equals(clientId)),
                    _repository.DeleteAsync<PraxisEquipmentMaintenance>(equipmentMaintenance => equipmentMaintenance.ClientId.Equals(clientId))
                };

                await Task.WhenAll(deleteTasks);

            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to delete {Equipment} and {Maintenance} for client {ClientId} Error: {ErrorMessage} Stacktrace: {StackTrace}",
                    nameof(PraxisEquipment), nameof(PraxisEquipmentMaintenance), clientId, e.Message, e.StackTrace);
            }
        }


        public async Task GenerateQrFileForEquipment(PraxisEquipment equipment)
        {

            dynamic equipmentQrObject = new ExpandoObject();
            var equipmentQrDictionary = (IDictionary<string, object>)equipmentQrObject;
            equipmentQrDictionary["EquipmentId"] = equipment.ItemId;


            var EquipmentMetaDatakeys = new List<string> {
                ReportConstants.EquipmentMetaDataKeys.SerialNumber,
                ReportConstants.EquipmentMetaDataKeys.InternalNumber,
                ReportConstants.EquipmentMetaDataKeys.InstallationNumber,
                ReportConstants.EquipmentMetaDataKeys.UDINumber
            };
            foreach (var key in EquipmentMetaDatakeys)
            {
                var metaDataValue = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == key)?.MetaData?.Value;
                if (metaDataValue != null)
                {
                    equipmentQrDictionary[key] = metaDataValue;
                }
            }


            var equipmentQrContent = JsonConvert.SerializeObject(equipmentQrObject);
            await _praxisQrGeneratorService.QRCodeGenerateAsync(equipment, equipmentQrContent, 150, 150, 0);
        }

        public async Task UpdateRolesAllowedToReadOfPraxisEquipment()
        {
            var equipments = (await _commonUtilService.GetEntityQueryResponse<PraxisEquipment>("{}")).Results;
            var rolesToBeAdded = new[]
            {
                RoleNames.PowerUser, RoleNames.Leitung, RoleNames.MpaGroup1, RoleNames.MpaGroup2
            };

            foreach (var equipment in equipments)
            {
                var rolesAllowedToUpdate = equipment.RolesAllowedToUpdate.ToList();
                rolesAllowedToUpdate.AddRange(rolesToBeAdded);
                equipment.RolesAllowedToUpdate = rolesAllowedToUpdate.Distinct().ToArray();
                await _repository.UpdateAsync(e => e.ItemId == equipment.ItemId, equipment);
            }
        }

        public async Task DeleteEquipmentFilesAsync(PraxisEquipment equipment)
        {
            var fileIds = new List<string>();
            var thumbnailIds = new List<string>();

            if (equipment.Photos != null)
            {
                var photos = equipment.Photos.ToArray();

                if (photos != null && photos.Length > 0)
                {
                    foreach (var photo in photos)
                    {
                        fileIds.Add(photo.FileId);
                        var thumbnails = photo.Thumbnails;
                        if (thumbnails != null)
                        {
                            foreach (var thumbnail in thumbnails)
                            {
                                thumbnailIds.Add(thumbnail.FileId);
                            }
                        }
                    }
                }
            }

            if (equipment.LocationImages != null)
            {
                var locationImages = equipment.LocationImages.ToArray();
                if (locationImages != null && locationImages.Length > 0)
                {
                    foreach (var locationImage in locationImages)
                    {
                        fileIds.Add(locationImage.FileId);
                        var locationThumbnails = locationImage.Thumbnails;
                        if (locationThumbnails != null)
                        {
                            foreach (var locationThumbnail in locationThumbnails)
                            {
                                thumbnailIds.Add(locationThumbnail.FileId);
                            }
                        }
                    }
                }
            }

            if (thumbnailIds.Count > 0) await _fileService.DeleteFilesFromStorage(thumbnailIds);
            if (fileIds.Count > 0) await _dmsService.DeleteObjectArtifactsAsync(fileIds);
        }

        public async Task<PraxisGenericReportResult> PrepareEquipmentPhotoDocumentationData(GetReportQuery filter)
        {
            var response = new List<PraxisEquipmentForReport>();
            var metaDataList = new List<MetaData>();
            try
            {
                _logger.LogInformation("Preparing Equipment PhotoDocumentation Data");
                var equipments =
                    (await _commonUtilService.GetEntityQueryResponse<PraxisEquipment>(
                        filter.FilterString,
                        filter.SortBy
                    )).Results;

                var roomIds = equipments?.Select(r => r.RoomId)?.ToList() ?? new List<string>();
                var praxisRooms = _praxisRoomService.GetPraxisRoomsByIds(roomIds);

                foreach (var equipment in equipments)
                {
                    _logger.LogInformation("Equipment Name: {EquipmentName}", equipment.Name);
                    try
                    {
                        var serialNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.SerialNumber)?.MetaData?.Value;
                        var internalNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InternalNumber)?.MetaData?.Value;
                        var installationNumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.InstallationNumber)?.MetaData?.Value;
                        var UDINumber = equipment?.MetaDataList?.FirstOrDefault(x => x.Key == EquipmentMetaDataKeys.UDINumber)?.MetaData?.Value;
                        var locationAddress = praxisRooms?.FirstOrDefault(r => r.ItemId == equipment?.RoomId)?.Address?.FullAddress ?? string.Empty;
                        var exactLocation = equipment?.MetaValues?.FirstOrDefault(x => x.Key == "ExactLocation")?.Value ?? string.Empty;

                        var equipmentForReport = new PraxisEquipmentForReport
                        {
                            EquipmentName = equipment.Name,
                            Department = equipment.ClientName,
                            ClientId = equipment.ClientId,
                            DateOfPurchase = equipment.DateOfPurchase?
                                .ToString(),
                            DateOfPlacingInService = equipment.DateOfPlacingInService
                                .ToString(CultureInfo.InvariantCulture),
                            MaintenanceMode = equipment.MaintenanceMode,
                            Suppliers = equipment.SupplierName,
                            Manufacturer = equipment.Manufacturer,
                            SerialNumber = serialNumber ?? equipment.SerialNumber,
                            InternalNumber = internalNumber ?? string.Empty,
                            InstallationNumber = installationNumber ?? string.Empty,
                            UDINumber = UDINumber ?? string.Empty,
                            LocationAddress = locationAddress,
                            ExactLocation = exactLocation,
                            LastMaintenanceDate = equipment.MaintenanceDates?
                                .LastOrDefault(date => date.Date < DateTime.UtcNow.Date)?
                                .Date?
                                .ToString(),
                            NextMaintenanceDate = equipment.MaintenanceDates?
                                .FirstOrDefault(date => date.Date >= DateTime.UtcNow.Date)?
                                .Date?
                                .ToString(),
                            Category = equipment.CategoryName,
                            SubCategory = equipment.SubCategoryName,
                            AdditionalInformation = equipment.AdditionalInfos,
                            ContactInformation = new PraxisEquipmentContactInformation()
                            {
                                Company = equipment.Company,
                                ContactPerson = equipment.ContactPerson,
                                Email = equipment.Email,
                                Phone = equipment.PhoneNumber
                            },
                            Location = equipment.RoomName,
                            Photos = equipment.Photos,
                            LocationLog = await GetPraxisEquipmentLocationHistoryById(equipment.ItemId),
                            Files = await _processGuideService.ProcessFilesForReportAsync(equipment.Files?.ToList() ??
                                new List<PraxisDocument>()),
                            UserAdditionalInformation =
                                GetAdditionalInfoByPraxisAdditionalInfoTitles(
                                    equipment.PraxisUserAdditionalInformationTitles?.ToList() ??
                                    new List<ItemIdAndTitle>(), equipment)
                        };

                        response.Add(equipmentForReport);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Error occurred while preparing Equipment PhotoDocumentation Data for '{EquipmentName}'", equipment.Name);
                        _logger.LogError("Error message: {ErrorMessage} StackTrace: {StackTrace}", e.Message, e.StackTrace);
                    }
                }

                metaDataList.Add(new MetaData()
                {
                    Name = "Equipments",
                    Values = response.Select(equipment =>
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            JsonConvert.SerializeObject(equipment))).ToList()
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while trying to prepare Equipment PhotoDocumentation Data");
                _logger.LogError("Error message: {ErrorMessage} StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }

            return new PraxisGenericReportResult()
            {
                MetaDataList = metaDataList,
                ClientIds = response
                    .Select(equipment => equipment.ClientId)
                    .Distinct(),
                IsFileSizeExceeded = false,
                PraxisEquipmentForReport = response
            };
        }

        public async Task AssignProcessGuide(AssignProcessGuideForEquipmentCommand command)
        {
            var equipment = await GetPraxisEquipment(command.EquipmentId);
            var praxisForms = _repository?
                .GetItems<PraxisForm>(form => command.FormIds.Contains(form.ItemId))?
                .ToList() ?? new List<PraxisForm>();
            if (praxisForms.Count() != command.FormIds.Count)
            {
                _logger.LogError("All the PraxisForms are not found with formIds: {FormIds}", JsonConvert.SerializeObject(command.FormIds));
                return;
            }

            if (equipment == null)
            {
                _logger.LogError("PraxisEquipment not found with equipmentId: {EquipmentId}", command.EquipmentId);
                return;
            }

            var taskList = praxisForms
                .Select(async form =>
                {
                    await AssignGuide(form, equipment);
                })
                .ToList();
            await Task.WhenAll(taskList);
        }

        private async Task AssignGuide(PraxisForm praxisForm, PraxisEquipment equipment)
        {
            var processGuideConfig = await SaveNewProcessGuideConfig(praxisForm, equipment.ClientId, equipment);
            var taskSchedulerModel = GetTaskSchedulerModel(processGuideConfig, praxisForm, equipment);

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(_taskManagementServiceBaseUrl + "TaskManagementService/TaskManagementCommand/CreateTaskSchedule"),
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(taskSchedulerModel),
                        Encoding.UTF8,
                        "application/json")
                };

                var token = await _authUtilityService.GetAdminToken();
                request.Headers.Add("Authorization", $"Bearer {token}");

                HttpResponseMessage response = await _serviceClient.SendToHttpAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to create Process Guide for Equipment with Process Guide ConfigId -> {ItemId}",
                        processGuideConfig.ItemId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while trying to assign process guide for equipment");
                _logger.LogError("Error message: {ErrorMessage} StackTrace: {StackTrace}", e.Message, e.StackTrace);
            }
        }

        public async Task UpdateEquipmentForAssignedProcessGuide(string equipmentId, string processGuideId)
        {
            var equipment = await GetPraxisEquipment(equipmentId);
            if (equipment == null)
            {
                _logger.LogError("PraxisEquipment not found with equipmentId: {EquipmentId}", equipmentId);
                return;
            }

            var relatedEntityInfo = new
            {
                RelatedEntityName = nameof(PraxisProcessGuide),
                RelatedEntityId = processGuideId
            };

            equipment.Topic.Value = JsonConvert.SerializeObject(relatedEntityInfo);
            await _repository.UpdateAsync(e => e.ItemId == equipmentId, equipment);
        }

        public async Task UpdateProcessGuideListingOfEquipment(AssignProcessGuideForEquipmentCommand command)
        {
            var equipment = await GetPraxisEquipment(command.EquipmentId);
            var praxisForms = _repository.GetItems<PraxisForm>(form => command.FormIds.Contains(form.ItemId));
            if (equipment == null)
            {
                _logger.LogError("PraxisEquipment not found with equipmentId: {EquipmentId}", command.EquipmentId);
                return;
            }
            if (praxisForms == null || praxisForms.Count() != command.FormIds.Count)
            {
                _logger.LogError("All PraxisForms not found with formIds: {FormIds}", JsonConvert.SerializeObject(command.FormIds));
                return;
            }

            var processGuideListing = praxisForms
                .Select(form =>
                    new EquipmentProcessGuideListing
                    {
                        FormId = form.ItemId,
                        FormTitle = form.Description
                    }
                )
                .ToList();
            var processGuideMetaValues = new PraxisKeyValue
            {
                Key = ProcessGuideListingKey,
                Value = JsonConvert.SerializeObject(processGuideListing)
            };
            var metaValues = equipment.MetaValues?.ToList() ?? new List<PraxisKeyValue>();
            DeleteKey(ProcessGuideListingKey, metaValues);
            metaValues.Add(processGuideMetaValues);
            equipment.MetaValues = metaValues;
            await _repository.UpdateAsync(e => e.ItemId == command.EquipmentId, equipment);
        }
        public async Task DeleteProcessGuideFromEquipment(DeleteProcessGuideFromEquipmentCommand command)
        {
            var equipment = await GetPraxisEquipment(command.EquipmentId);
            if (equipment == null)
            {
                _logger.LogError("PraxisEquipment not found with equipmentId: {EquipmentId}", command.EquipmentId);
                return;
            }

            var existingProcessGuideListing = new List<EquipmentProcessGuideListing>();
            if (equipment.MetaValues?.Any() ?? false)
            {
                existingProcessGuideListing = JsonConvert.DeserializeObject<List<EquipmentProcessGuideListing>>(
                    equipment.MetaValues.FirstOrDefault(meta => meta.Key.Equals(ProcessGuideListingKey))?.Value ?? "[]"
                );
            }
            existingProcessGuideListing.RemoveAll(f => command.FormIds.Contains(f.FormId));
            var processGuideMetaValues = new PraxisKeyValue
            {
                Key = ProcessGuideListingKey,
                Value = JsonConvert.SerializeObject(existingProcessGuideListing)
            };
            var metaValues = equipment.MetaValues?.ToList() ?? new List<PraxisKeyValue>();
            DeleteKey(ProcessGuideListingKey, metaValues);
            metaValues.Add(processGuideMetaValues);
            equipment.MetaValues = metaValues;
            await _repository.UpdateAsync(e => e.ItemId == command.EquipmentId, equipment);
            await DeleteGuidesFromActiveMaintenances(command);
        }

        private async Task DeleteGuidesFromActiveMaintenances(DeleteProcessGuideFromEquipmentCommand command)
        {
            var activeMaintenances = _repository.GetItems<PraxisEquipmentMaintenance>(maintenance =>
                    !maintenance.IsMarkedToDelete &&
                    !(maintenance.CompletionStatus != null &&
                     maintenance.CompletionStatus.Value == "DONE") &&
                    maintenance.PraxisFormInfo != null &&
                    command.FormIds.Contains(maintenance.PraxisFormInfo.FormId) &&
                    command.EquipmentId == maintenance.PraxisEquipmentId)?
                .ToList() ?? new List<PraxisEquipmentMaintenance>();
            var taskList = activeMaintenances
                .Select(async maintenance =>
                {
                    maintenance.PraxisFormInfo = null;
                    maintenance.ProcessGuideId = null;
                    await _repository.UpdateAsync(m => m.ItemId == maintenance.ItemId, maintenance);
                })
                .ToList();
            await Task.WhenAll(taskList);
        }

        public async Task DeleteLibraryFilesFromEquipment(DeleteLibraryFilesFromEquipmentCommand command)
        {
            var equipment = await _repository.GetItemAsync<PraxisEquipment>(e => e.ItemId == command.EquipmentId);
            if (equipment == null)
            {
                _logger.LogError("PraxisEquipment not found with equipmentId: {EquipmentId}", command.EquipmentId);
                return;
            }
            equipment.Files = equipment.Files?
                .Where(f => !command.FileIds.Contains(f.DocumentId))
                .ToList();
            await _repository.UpdateAsync(e => e.ItemId == command.EquipmentId, equipment);

            // Here we have to delete the files being used in active maintenances
            var activeMaintenances = _repository.GetItems<PraxisEquipmentMaintenance>(maintenance =>
                    !maintenance.IsMarkedToDelete &&
                    !(maintenance.CompletionStatus != null &&
                     maintenance.CompletionStatus.Value == "DONE") &&
                    maintenance.LibraryForms != null &&
                    maintenance.LibraryForms.Any(f => command.FileIds.Contains(f.LibraryFormId)) &&
                    maintenance.PraxisEquipmentId == command.EquipmentId)?
                .ToList() ?? new List<PraxisEquipmentMaintenance>();
            var taskList = activeMaintenances
                .Select(async maintenance =>
                {
                    maintenance.LibraryForms = maintenance.LibraryForms?
                        .Where(f => !command.FileIds.Contains(f.LibraryFormId))
                        .ToList();
                    await _repository.UpdateAsync(m => m.ItemId == maintenance.ItemId, maintenance);
                })
                .ToList();
            await Task.WhenAll(taskList);
        }

        private async Task<LocationChangeLog> GetPraxisEquipmentLocationHistoryById(string equipmentId)
        {
            var response = await _repository.GetItemAsync<PraxisEquipmentLocationHistory>(log => log.EquipmentId.Equals(equipmentId));
            return response?.LocationChangeLog ?? new LocationChangeLog();
        }

        private List<PraxisAdditionalInfoTitleWithUser> GetAdditionalInfoByPraxisAdditionalInfoTitles(
            List<ItemIdAndTitle> praxisItemIdAndTitles, PraxisEquipment equipment)
        {
            var responses = new List<PraxisAdditionalInfoTitleWithUser>();
            foreach (var info in praxisItemIdAndTitles)
            {
                var userList = GetPraxisUserByClientIdAndAdditionalInfo(equipment.ClientId, info.ItemId);
                responses.Add(new PraxisAdditionalInfoTitleWithUser
                {
                    Title = info.Title,
                    UserList = userList?.Select(user => user.DisplayName)?.ToList() ?? new List<string>()
                });
            }
            return responses;
        }

        private List<PraxisUser> GetPraxisUserByClientIdAndAdditionalInfo(string clientId, string additionalInfoItemId)
        {
            return _repository
                .GetItems<PraxisUser>(user =>
                    !user.IsMarkedToDelete &&
                    !(user.Roles != null && user.Roles.Contains(RoleNames.GroupAdmin)) &&
                    user.ClientList != null &&
                    user.AdditionalInfo != null &&
                    user.ClientList.Any(client => client.ClientId.Equals(clientId)) &&
                    user.AdditionalInfo.Any(info => info.ItemId.Equals(additionalInfoItemId)))
                .ToList();
        }

        private async Task<PraxisProcessGuideConfig> SaveNewProcessGuideConfig(PraxisForm praxisForm, string departmentId, PraxisEquipment equipment)
        {
            var assignedUsers = GetAssignedControlledUsers(equipment.ItemId, departmentId);
            var assignedClients = GetProcessGuideClientInfo(assignedUsers, equipment);
            var currentDate = DateTime.UtcNow;
            var taskTimetable = new PraxisTaskTimetable
            {
                SubmissionDates = new List<DateTime> { currentDate }
            };

            var newPgConfig = new PraxisProcessGuideConfig
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = currentDate,
                FormId = praxisForm.ItemId,
                TopicKey = praxisForm.TopicKey,
                TopicValue = praxisForm.TopicValue,
                Title = praxisForm.Title,
                TaskTimetable = taskTimetable,
                Clients = assignedClients,
                ControlledMembers = assignedUsers,
                DueDate = currentDate,
                RolesAllowedToRead = new string[] { RoleNames.Admin, RoleNames.AppUser },
                RolesAllowedToUpdate = new string[]
                    { RoleNames.Admin, RoleNames.TaskController, RoleNames.PowerUser, RoleNames.Leitung },
                RolesAllowedToDelete = new string[] { RoleNames.Admin },
                IdsAllowedToDelete = new string[] { _securityContextProvider.GetSecurityContext().UserId }
            };

            await _repository.SaveAsync(newPgConfig);

            return newPgConfig;
        }

        private CreateTaskScheduleRequestModel GetTaskSchedulerModel(PraxisProcessGuideConfig processGuideConfig, PraxisForm praxisForm, PraxisEquipment equipment)
        {
            var taskDataList = new List<TaskData>();
            var taskData = new TaskData()
            {
                HasRelatedEntity = true,
                HasTaskScheduleIntoRelatedEntity = true,
                RelatedEntityName = EntityName.PraxisProcessGuide,
                TaskSummaryId = Guid.NewGuid().ToString(),
                Title = processGuideConfig.Title,
                RelatedEntityObject = GetRelatedEntityObject(processGuideConfig, praxisForm, equipment)
            };
            taskDataList.Add(taskData);

            var taskScheduleDetails = new TaskScheduleDetails()
            {
                HasToMoveNextDay = true,
                IsRepeat = false,
                SubmissionDates = new List<string> { DateTime.UtcNow.ToString("yyyy-MM-dd") },
            };
            return new CreateTaskScheduleRequestModel()
            {
                TaskScheduleDetails = taskScheduleDetails,
                TaskDatas = taskDataList,
                AssignMembers = new List<object>()
            };
        }
        private List<ProcessGuideClientInfo> GetProcessGuideClientInfo(List<string> userIds, PraxisEquipment equipment)
        {
            var clients = new List<ProcessGuideClientInfo>();

            var clientInfo = new ProcessGuideClientInfo
            {
                ClientId = equipment?.ClientId,
                ClientName = equipment?.ClientName,
                CategoryId = equipment?.CategoryId ?? "",
                CategoryName = equipment?.CategoryName ?? "",
                SubCategoryId = equipment?.SubCategoryId ?? "",
                SubCategoryName = equipment?.SubCategoryName ?? "",
                ControlledMembers = userIds,
                HasSpecificControlledMembers = false
            };
            clients.Add(clientInfo);
            return clients;
        }

        private RelatedEntityObject GetRelatedEntityObject(PraxisProcessGuideConfig processGuideConfig, PraxisForm praxisForm, PraxisEquipment equipment)
        {
            return new RelatedEntityObject()
            {
                ItemId = Guid.NewGuid().ToString(),
                FormId = processGuideConfig.FormId,
                FormName = praxisForm.Description,
                Title = praxisForm.Title,
                Tags = new[] { "Is-Valid-PraxisProcessGuide" },
                Language = "en-US",
                TopicKey = processGuideConfig.TopicKey,
                TopicValue = processGuideConfig.TopicValue,
                Description = praxisForm.Title,
                PatientDateOfBirth = DateTime.UtcNow,
                IsActive = true,
                ControlledMembers = processGuideConfig.ControlledMembers,
                Clients = processGuideConfig.Clients,
                ClientId = processGuideConfig.Clients.FirstOrDefault()?.ClientId,
                ClientName = processGuideConfig.Clients.FirstOrDefault()?.ClientName,
                DueDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PraxisProcessGuideConfigId = processGuideConfig.ItemId,
                RelatedEntityId = equipment.ItemId,
                RelatedEntityName = EntityName.PraxisEquipment
            };
        }

        private List<string> GetAssignedControlledUsers(string equipmentId, string departmentId)
        {
            var equipmentSpecificUsers = _repository.GetItem<PraxisEquipmentRight>(right =>
                right.EquipmentId.Equals(equipmentId) && right.DepartmentId.Equals(departmentId));

            if (equipmentSpecificUsers != null)
                return equipmentSpecificUsers
                    .AssignedAdmins?
                    .Select(user => user.PraxisUserId)
                    .ToList() ?? new List<string>();

            return _repository.GetItem<PraxisEquipmentRight>(right =>
                    right.DepartmentId.Equals(departmentId) && right.IsOrganizationLevelRight == true)?
                .AssignedAdmins?
                .Select(user => user.PraxisUserId)
                .ToList() ?? new List<string>();
        }

        private void DeleteKey(string key, List<PraxisKeyValue> metaValues)
        {
            metaValues ??= new List<PraxisKeyValue>();
            var keyToDelete = metaValues.FirstOrDefault(meta => meta.Key.Equals(key));
            if (keyToDelete != null)
            {
                metaValues.Remove(keyToDelete);
            }
        }
    }
}
