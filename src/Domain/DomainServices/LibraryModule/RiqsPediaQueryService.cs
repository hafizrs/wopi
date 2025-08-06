using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.GraphQL.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class RiqsPediaQueryService : IRiqsPediaQueryService
    {
        private readonly IRepository _repository;
        private readonly ILogger<RiqsPediaQueryService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;

        public RiqsPediaQueryService(
            IRepository repository,
            ILogger<RiqsPediaQueryService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IBlocksMongoDbDataContextProvider ecapRepository,
            IMongoSecurityService mongoSecurityService,
            IRiqsPediaViewControlService riqsPediaViewControlService
        )
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _ecapRepository = ecapRepository;
            _mongoSecurityService = mongoSecurityService;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public async Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForRiqsPedia(GetPraxisClientsForRiqsPediaQuery query)
        {
            return await ExecuteQueryAsync<GetPraxisClientsForRiqsPediaQuery, ProjectedClientResponse>(query, "PraxisClients", DeserializeAndProjectPraxisClient);
        }

        public async Task<EntityQueryResponse<ProjectedUserResponse>> GetPraxisUserForRiqsPedia(GetPraxisUserForRiqsPediaQuery query)
        {
            var additionalFilter = Builders<BsonDocument>.Filter.Eq("Active", true);
            additionalFilter &= Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.AnyEq("Roles", RoleNames.GroupAdmin));
            return await ExecuteQueryAsync<GetPraxisUserForRiqsPediaQuery, ProjectedUserResponse>(query, "PraxisUsers", DeserializeAndProjectUser);
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
            var collections = _ecapRepository
                .GetTenantDataContext()
                .GetCollection<BsonDocument>(collectionName)
                .Aggregate()
                .Match(combinedFilter);

            totalRecord = collections.ToEnumerable().Count();

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

        private async Task<FilterDefinition<BsonDocument>> InjectRowLevelSecurity<TQuery>(TQuery query) where TQuery : GenericEntityQuery
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;

            var hasAdditionalAdminRole = await IsAdditionalAdminRole(userId);

            var rolesAllowedToRead = GetRolesForCurrentUser(hasAdditionalAdminRole);

            FilterDefinition<BsonDocument> queryFilter = string.IsNullOrEmpty(query.FilterString)
                ? new BsonDocument()
                : BsonSerializer.Deserialize<BsonDocument>(query.FilterString);

            queryFilter = queryFilter.InjectRowLevelSecurityFilter(
                PdsActionEnum.Read,
                securityContext,
                rolesAllowedToRead.ToList()
            );

            return queryFilter;
        }

        private string[] GetRolesForCurrentUser(bool hasAdditionalAdminRole)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;

            if (_securityHelperService.IsAAdmin() ||
                _securityHelperService.IsAAdminBUser() ||
                !hasAdditionalAdminRole)
            {
                return securityContext.Roles.ToArray();
            }

            var clientRoles = GetPraxisClientRoles(userId);

            return clientRoles?.Any() == true ? clientRoles : securityContext.Roles.ToArray();
        }

        private string[] GetPraxisClientRoles(string userId)
        {
            var praxisUser = _repository.GetItem<PraxisUser>(x => x.UserId.Equals(userId));

            if (praxisUser?.ClientList == null || !praxisUser.ClientList.Any())
            {
                return null;
            }

            var client = praxisUser.ClientList.FirstOrDefault();
            var orgId = client?.ParentOrganizationId;

            if (string.IsNullOrEmpty(orgId))
            {
                return new[] { string.Empty };
            }

            var praxisClients = _repository.GetItems<PraxisClient>(x => x.ParentOrganizationId.Equals(orgId)).ToList();

            if (praxisClients == null || !praxisClients.Any())
            {
                return new[] { string.Empty };
            }

            var clientRoles = new List<string>();

            foreach (var clientId in praxisClients.Select(x => x.ItemId))
            {
                var clientAdminRole = _mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
                clientRoles.Add(clientAdminRole);
            }

            var orgAdminRole = _mongoSecurityService.GetRoleName(RoleNames.AdminB_Dynamic, orgId);
            clientRoles.Add(orgAdminRole);

            return clientRoles.ToArray();
        }

        private async Task<bool> IsAdditionalAdminRole(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            var riqsViewControl = await _riqsPediaViewControlService.GetRiqsPediaViewControl();

            return riqsViewControl?.IsAdminViewEnabled ?? false;
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
    }
}
