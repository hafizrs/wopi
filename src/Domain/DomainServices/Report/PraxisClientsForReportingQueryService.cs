using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;
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

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class PraxisClientsForReportingQueryService : IPraxisClientsForReportingQueryService
    {
        private readonly IRepository _repository;
        private readonly IMongoSecurityService _mongoSecurityService;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapRepository;
        private readonly ILogger<PraxisClientsForReportingQueryService> _logger;
        private readonly ICommonUtilService _commonUtilService;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly ICirsPermissionService _cisPermissionService;
        public PraxisClientsForReportingQueryService(
         IMongoSecurityService mongoSecurityService,
         IRepository repository,
         ISecurityContextProvider securityContextProvider,
         IBlocksMongoDbDataContextProvider ecapRepository,
         ILogger<PraxisClientsForReportingQueryService> logger,
         ICommonUtilService commonUtilService,
         ISecurityHelperService securityHelperService,
         ICirsPermissionService cirsPermissionService
        )
        {
            _repository = repository;
            _mongoSecurityService = mongoSecurityService;
            _securityContextProvider = securityContextProvider;
            _ecapRepository = ecapRepository;
            _logger = logger;
            _commonUtilService = commonUtilService;
            _securityHelperService = securityHelperService;
            _cisPermissionService = cirsPermissionService;
        }

        private string[] GetPraxisClientRoles(string userId)
        {
            var praxisUser = _repository.GetItem<PraxisUser>(x => x.UserId.Equals(userId));
            if (praxisUser == null || praxisUser.ClientList == null || !praxisUser.ClientList.Any()) return null;
            var client = praxisUser.ClientList.FirstOrDefault();
            var orgId = client.ParentOrganizationId;
            if (string.IsNullOrEmpty(orgId)) return new[] { "" };
            var clients = _repository.GetItems<PraxisClient>(x => x.ParentOrganizationId.Equals(orgId))?.ToList();
            if (clients == null) return new[] { "" };
            var clientIds = clients.Select(x => x.ItemId).ToList();

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
            var clientRoles = GetPraxisClientRoles(userId);
            if (clientRoles != null && clientRoles.Any()) return clientRoles;
            return (string[])securityContext.Roles;
        }
        private FilterDefinition<BsonDocument> InjectRowLevelSecurity<TQuery>(TQuery query) where TQuery : GetPraxisClientsForReportingQuery
        {

            var securityContext = _securityContextProvider.GetSecurityContext();
            var userId = securityContext.UserId;
            var hasAssignAdminRight = query.HaveReportingRights; //await _cisPermissionService.HaveAllUnitViewPermissions(userId);
            var rolesAllowedToRead = GetRolesForCurrentUser(hasAssignAdminRight);
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
            where TQuery : GetPraxisClientsForReportingQuery
        {
            var queryFilter = InjectRowLevelSecurity(query);
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

            totalRecord = (await totalCollections.CountDocumentsAsync(combinedFilter));

            var collections = totalCollections
                    .Find(combinedFilter);

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

        public async Task<EntityQueryResponse<ProjectedClientResponse>> GetPraxisClientsForReport(GetPraxisClientsForReportingQuery query)
        {
            return await ExecuteQueryAsync(query, "PraxisClients", DeserializeAndProjectPraxisClient);
        }

    }

}
