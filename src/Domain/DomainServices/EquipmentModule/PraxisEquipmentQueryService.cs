using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.EquipmentModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.EquipmentModule
{
    public class PraxisEquipmentQueryService : IPraxisEquipmentQueryService
    {
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ILogger<PraxisEquipmentQueryService> _logger;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISecurityHelperService _securityHelperService;
        public PraxisEquipmentQueryService(
         IMongoSecurityService mongoSecurityService,
         IRepository repository,
         ISecurityContextProvider securityContextProvider,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ILogger<PraxisEquipmentQueryService> logger,
         ICommonUtilService commonUtilService,
         ISecurityHelperService securityHelperService


     )
        {
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _ecapRepository = ecapRepository;
            _logger = logger;
            _commonUtilService = commonUtilService;
            _securityHelperService = securityHelperService;

        }
        public Task<EntityQueryResponse<ProjectedEquipmentResponse>> GetPraxisEquipmentDetail(GetEquipementQuery query)
        {
            throw new NotImplementedException();
        }


        public async Task<EntityQueryResponse<ProjectedEquipmentMaintenanceResponse>> GetPraxisEquipmentMaintenances(GetPraxisEquipmentMaintenancesQuery query)
        {
            return await ExecuteQueryAsync<GetPraxisEquipmentMaintenancesQuery, ProjectedEquipmentMaintenanceResponse>(query, "PraxisEquipmentMaintenances", DeserializeAndProjectEquipmentMaintenance);
        }

        public async Task<EntityQueryResponse<ProjectedEquipmentResponse>> GetPraxisEquipments(GetEquipementQuery query)
        {
            return await ExecuteQueryAsync<GetEquipementQuery, ProjectedEquipmentResponse>(query, "PraxisEquipments", DeserializeAndProjectEquipment);
        }
        private async Task<bool> IsAssignEquipmentAdmin(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var organizationIds = _securityHelperService.ExtractOrganizationIdsFromOrgLevelUser();
            if (organizationIds == null || !organizationIds.Any()) return false;
            return await _repository.ExistsAsync<PraxisEquipmentRight>(x => organizationIds.Contains(x.OrganizationId) && x.IsOrganizationLevelRight == true && x.AssignedAdmins.Any(y => y.UserId.Equals(userId)));
        }


        private string[] GetPraxiClientRoles(string userId)
        {
            var praxisUser = _repository.GetItem<PraxisUser>(x => x.UserId.Equals(userId));
            if (praxisUser == null || praxisUser.ClientList == null || !praxisUser.ClientList.Any()) return null;
            var client = praxisUser.ClientList.FirstOrDefault();
            var orgId = client.ParentOrganizationId;
            if (string.IsNullOrEmpty(orgId)) return new[] { "" };
            var praxisClietns = _repository.GetItems<PraxisClient>(x => x.ParentOrganizationId.Equals(orgId)).ToList();
            if (praxisClietns == null) return new[] { "" };
            var clientIds = praxisClietns.Select(x => x.ItemId).ToList();

            var clientRoles = new List<string>();
            foreach (var clientId in clientIds)
            {
                var clientAdminAccessRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
                clientRoles.Add(clientAdminAccessRole);
            }
            clientRoles.Add(_mongoSecurityService.GetRoleName(RoleNames.Organization_Read_Dynamic, orgId));
            return clientRoles.ToArray();
        }


        private string[] GetRolesForCurrentUser(bool hasAdminRight)
        {

            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;
            if (_securityHelperService.IsAAdmin()) return new[] { RoleNames.Admin };
            if (!hasAdminRight) return securityContext.Roles.ToArray();
            var clientRoles = GetPraxiClientRoles(userId);
            if (clientRoles != null && clientRoles.Any()) return clientRoles;
            return (string[])securityContext.Roles;
        }
        private async Task<FilterDefinition<BsonDocument>> InjectRowLevelSecurity<TQuery>(TQuery query) where TQuery : GenericEntityQuery
        {

            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;
            var hasEquipmentRight = await IsAssignEquipmentAdmin(userId);
            var rolesAllowedToRead = GetRolesForCurrentUser(hasEquipmentRight);
            var filter = BsonSerializer.Deserialize<BsonDocument>(query.FilterString);
            FilterDefinition<BsonDocument> queryFilter = new BsonDocument();
            if (!string.IsNullOrEmpty(query.FilterString))
            {
                queryFilter = BsonSerializer.Deserialize<BsonDocument>(query.FilterString);
            }

            queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                 PdsActionEnum.Read,
                 securityContext,
                 rolesAllowedToRead.ToList()
             );


            return queryFilter;
        }


        private async Task<EntityQueryResponse<TResponse>> ExecuteQueryAsync<TQuery, TResponse>(
            TQuery query,
            string collectionName,
            Func<BsonDocument, TResponse> projection,
            FilterDefinition<BsonDocument> additionalFilter = null
            )
            where TQuery : GenericEntityQuery
        {
            var queryFilter = await InjectRowLevelSecurity(query);
            long totalRecord = 0;

            query.PageNumber += 1;
            var skip = query.PageSize * (query.PageNumber - 1);
            var isMarkTodelete = Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);
            var combinedFilter = Builders<BsonDocument>.Filter.And(queryFilter, isMarkTodelete);
            if (additionalFilter != null)
            {
                combinedFilter = Builders<BsonDocument>.Filter.And(combinedFilter, additionalFilter);
            }
            var totalCollections = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<BsonDocument>(collectionName);

            totalRecord = (await totalCollections.CountDocumentsAsync(combinedFilter)); // use it for non aggregate query filter

            var collections = totalCollections
                    .Find(combinedFilter);

            // totalRecord = (await collections.Count().FirstOrDefaultAsync())?.Count ?? 0; // use it for complex aggregate query filter

            if (!string.IsNullOrEmpty(query.SortBy))
            {
                collections = collections.Sort(BsonDocument.Parse(query.SortBy));
            }

            collections = collections.Skip(skip).Limit(query.PageSize);

            var results = collections.ToEnumerable()
                .Select(document => projection(document))
                .ToList();

            return new EntityQueryResponse<TResponse>
            {
                Results = results,
                TotalRecordCount = totalRecord
            };
        }

        private ProjectedEquipmentMaintenanceResponse DeserializeAndProjectEquipmentMaintenance(BsonDocument document)
        {
            var equipmentMaintenance = BsonSerializer.Deserialize<PraxisEquipmentMaintenance>(document);
            return new ProjectedEquipmentMaintenanceResponse(
                ClientId: equipmentMaintenance.ClientId,
                PraxisEquipmentId: equipmentMaintenance.PraxisEquipmentId,
                Title: equipmentMaintenance.Title,
                EquipmentTitle: equipmentMaintenance.EquipmentTitle,
                Description: equipmentMaintenance.Remarks,
                MaintenanceDate: equipmentMaintenance.MaintenanceDate,
                MaintenanceEndDate: equipmentMaintenance.MaintenanceEndDate,
                MaintenancePeriod: equipmentMaintenance.MaintenancePeriod,
                ResponsiblePersonIds: equipmentMaintenance.ResponsiblePersonIds,
                CompletionStatus: equipmentMaintenance.CompletionStatus,
                CompletionStatusDetail: equipmentMaintenance.CompletionStatusDetail,
                Remarks: equipmentMaintenance.Remarks,
                Answers: equipmentMaintenance.Answers,
                LibraryForms: equipmentMaintenance.LibraryForms,
                LibraryFormResponses: equipmentMaintenance.LibraryFormResponses,
                ExecutivePersonIds: equipmentMaintenance.ExecutivePersonIds,
                ApprovedPersonIds: equipmentMaintenance.ApprovedPersonIds,
                ScheduleType: equipmentMaintenance.ScheduleType,
                ProcessGuideId: equipmentMaintenance.ProcessGuideId,
                ExternalUserInfos: equipmentMaintenance.ExternalUserInfos,
                ApprovalRequired: equipmentMaintenance.ApprovalRequired,
                PraxisFormInfo: equipmentMaintenance.PraxisFormInfo,
                CreatedBy: equipmentMaintenance.CreatedBy,
                CreateDate: equipmentMaintenance.CreateDate,
                ItemId: equipmentMaintenance.ItemId,
                LastUpdateDate: equipmentMaintenance.LastUpdateDate,
                MetaDataList: equipmentMaintenance.MetaDataList
            );
        }

        private ProjectedEquipmentResponse DeserializeAndProjectEquipment(BsonDocument document)
        {
            var equipment = BsonSerializer.Deserialize<PraxisEquipment>(document);
            return new ProjectedEquipmentResponse(
                equipment.ClientId,
                equipment.ClientName,
                equipment.Name,
                equipment.RoomId,
                equipment.RoomName,
                equipment.CategoryId,
                equipment.CategoryName,
                equipment.SubCategoryId,
                equipment.SubCategoryName,
                equipment.Topic,
                equipment.Manufacturer,
                equipment.AdditionalInfos,
                equipment.InstallationDate,
                equipment.LastMaintenanceDate,
                equipment.NextMaintenanceDate,
                equipment.Email,
                equipment.PhoneNumber,
                equipment.Remarks,
                equipment.SupplierId,
                equipment.SupplierName,
                equipment.SerialNumber,
                equipment.DateOfPurchase,
                equipment.MaintenanceMode,
                equipment.Company,
                equipment.ContactPerson,
                equipment.Photos,
                equipment.MaintenanceDates,
                equipment.DateOfPlacingInService,
                equipment.EquipmentQrFileId,
                equipment.LocationImages,
                equipment.Files,
                equipment.PraxisUserAdditionalInformationTitles,
                equipment.CompanyId,
                equipment.ManufacturerId,
                equipment.CreatedBy,
                equipment.CreateDate,
                equipment.ItemId,
                equipment.LastUpdateDate,
                equipment.Language,
                equipment.EquipmentContactsInformation,
                equipment.MetaValues,
                equipment.MetaDataList
            );

        }


        private ProjectedClientResponse DeserializeAndProjectPraxisClient(BsonDocument document)
        {
            var client = BsonSerializer.Deserialize<PraxisClient>(document);

            return new ProjectedClientResponse(
                client.ParentOrganizationId,
                client.ParentOrganizationName,
                client.ClientName,
                client.ClientNumber,
                client.MemberPhysicianNetwork,
                client.WebPageUrl,
                client.MedicalSoftware,
                client.ComputerSystem,
                client.Logo,
                client.IsSameAddressAsParentOrganization,
                client.Address,
                client.ContactEmail,
                client.ContactPhone,
                client.AdditionalInfos,
                client.PraxisUserAdditionalInformationTitles,
                client.CompanyTypes,
                client.Navigations,
                client.IsOpenOrganization,
                client.IsOrgTypeChangeable,
                client.IsCreateUserEnable,
                client.UserLimit,
                client.AuthorizedUserLimit,
                client.UserCount,
                client.CirsReportConfig,
                client.IsSubscriptionExpired,
                client.AdminUserId,
                client.DeputyAdminUserId,
                client.CirsAdminIds,
                client.CreatedBy,
                client.CreateDate,
                client.ItemId,
                client.LastUpdateDate
            );


        }

        private ProjectedUserResponse DeserializeAndProjectUser(BsonDocument document)
        {
            var user = BsonSerializer.Deserialize<PraxisUser>(document);

            return new ProjectedUserResponse(
                user.UserId,
                user.Image,
                user.Salutation,
                user.FirstName,
                user.LastName,
                user.DisplayName,
                user.Gender,
                user.DateOfBirth,
                user.Nationality,
                user.MotherTongue,
                user.OtherLanguage,
                user.Designation,
                user.Email,
                user.Phone,
                user.AcademicTitle,
                user.WorkLoad,
                user.KuNumber,
                user.NumberOfChildren,
                user.Roles,
                user.Skills,
                user.Specialities,
                user.CertificateOfCompetence,
                user.DateOfJoining,
                user.NumberOfPatient,
                user.Telephone,
                user.GlnNumber,
                user.ZsrNumber,
                user.KNumber,
                user.Remarks,
                user.PhoneExtensionNumber,
                user.Active,
                user.ClientList,
                user.IsEmailVerified,
                user.ShowIntroductionTutorial,
                user.AdditionalInfo,
                user.ClientId,
                user.ClientName,
                user.CreatedBy,
                user.CreateDate,
                user.ItemId,
                user.LastUpdateDate
            );
        }
        private ProjectedPraxisClientCategoryResponse DeserializeAndProjectClientCategory(BsonDocument document)
        {
            var clientCategory = BsonSerializer.Deserialize<PraxisClientCategory>(document);

            return new ProjectedPraxisClientCategoryResponse(
                clientCategory.ClientId,
                clientCategory.OrganizationId,
                clientCategory.Name,
                clientCategory.ParentId,
                clientCategory.ControllingGroup,
                clientCategory.ControlledGroup,
                clientCategory.SubCategories,
                clientCategory.CreatedBy,
                clientCategory.CreateDate,
                clientCategory.ItemId,
                clientCategory.LastUpdateDate
            );
        }
        private ProjectedPraxisRoomResponse DeserializeAndProjectRoom(BsonDocument document)
        {
            var room = BsonSerializer.Deserialize<PraxisRoom>(document);

            return new ProjectedPraxisRoomResponse(
                room.ClientId,
                room.Name,
                room.Description,
                room.RoomKey,
                room.RoomLevel,
                room.Address,
                room.CreatedBy,
                room.CreateDate,
                room.ItemId,
                room.LastUpdateDate,
                room.Remarks,
                room.ServiceProviderId,
                room.ServiceProviderName
            );
        }


        public async Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForEquipement(GetEquipementClientQuery query)
        {
            return await ExecuteQueryAsync<GetEquipementClientQuery, ProjectedClientResponse>(query, "PraxisClients", DeserializeAndProjectPraxisClient);
        }

        public async Task<EntityQueryResponse<ProjectedUserResponse>> GetPraxisUsersForEquipement(GetEquipementUserQuery query)
        {
            var additionalFilter = Builders<BsonDocument>.Filter.Eq("Active", true);
            additionalFilter &= Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.AnyEq("Roles", RoleNames.AdminB));
            return await ExecuteQueryAsync<GetEquipementUserQuery, ProjectedUserResponse>(query, "PraxisUsers", DeserializeAndProjectUser);
        }

        public async Task<EntityQueryResponse<ProjectedPraxisClientCategoryResponse>> GetPraxisClientCategoryForEquipement(GetEquipementClientCategoryQuery query)
        {
            return await ExecuteQueryAsync<GetEquipementClientCategoryQuery, ProjectedPraxisClientCategoryResponse>(query, "PraxisClientCategorys", DeserializeAndProjectClientCategory);
        }

        public async Task<EntityQueryResponse<ProjectedPraxisRoomResponse>> GetPraxisRoomForEquipement(GetEquipementRoomQuery query)
        {
            return await ExecuteQueryAsync<GetEquipementRoomQuery, ProjectedPraxisRoomResponse>(query, "PraxisRooms", DeserializeAndProjectRoom);
        }
    }
}
