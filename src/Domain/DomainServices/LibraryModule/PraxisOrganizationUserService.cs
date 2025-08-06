using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class PraxisOrganizationUserService : IPraxisOrganizationUserService
    {
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
        private readonly ISecurityHelperService _securityHelperService;

        public PraxisOrganizationUserService(
            ISecurityContextProvider securityContextProvider,
            IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
            ISecurityHelperService securityHelperService)
        {
            _securityContextProvider = securityContextProvider;
            _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
            _securityHelperService = securityHelperService;
        }

        public async Task<List<PraxisUserResponse>> GetOrganizationUsers(GetPraxisOrganizationUserQuery query)
        {
            var praxisUserRepo =
                _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");
            var filter = GetPraxisUserQueryFilter(query);

            var projection = Builders<PraxisUser>.Projection
                .Include(r => r.ItemId)
                .Include(r => r.DisplayName)
                .Include(r => r.ClientList);

            var praxisUsers = await praxisUserRepo
                .Find(filter)
                .Sort(Builders<PraxisUser>.Sort.Ascending("DisplayName"))
                .Project(projection)
                .ToListAsync();

            if (_securityHelperService.IsAPowerUser() && !_securityHelperService.IsAAdminOrTaskConrtroller())
            {
                var clientId = _securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                return praxisUsers?
                    .Select(u => BsonSerializer.Deserialize<PraxisUser>(u))
                    .Where(user => user.ClientList.Any(u => u.ClientId == clientId))
                    .Select(u => new PraxisUserResponse
                    {
                        PraxisUserId = u.ItemId,
                        DisplayName = u.DisplayName
                    })
                    .ToList() ?? new List<PraxisUserResponse>();
            }

            var libraryAdmins = praxisUsers
                .Select(i => new PraxisUserResponse
                {
                    PraxisUserId = i.GetValue("_id")?.AsString ?? string.Empty,
                    DisplayName = i.GetValue("DisplayName")?.AsString ?? string.Empty
                })
                .ToList();

            return libraryAdmins;
        }

        private FilterDefinition<PraxisUser> GetPraxisUserQueryFilter(
            GetPraxisOrganizationUserQuery query)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            query.DepartmentIds ??= new List<string>();

            var builder = Builders<PraxisUser>.Filter;
            var filter =
                builder.ElemMatch(pu => pu.ClientList,
                    Builders<PraxisClientInfo>.Filter.Eq(pci => pci.ParentOrganizationId, query.OrganizationId)) &
                builder.Eq(pu => pu.IsMarkedToDelete, false) &
                builder.Eq(pu => pu.Active, true) &
                builder.AnyIn(pu => pu.Roles, query.Roles);

            if (query.DepartmentIds?.Count > 0)
            {
                filter &= builder.ElemMatch(pu => pu.ClientList,
                    Builders<PraxisClientInfo>.Filter.In(pu => pu.ClientId, query.DepartmentIds));
            }

            if (!query.Roles.Contains(RoleNames.AdminB))
            {
                filter &= builder.Not(builder.AnyEq(pu => pu.Roles, RoleNames.AdminB));
            }
            filter &= builder.Not(builder.AnyEq(pu => pu.Roles, RoleNames.GroupAdmin));

            filter &= builder.AnyIn(pu => pu.RolesAllowedToRead, securityContext.Roles) |
                      builder.AnyEq(pu => pu.IdsAllowedToRead, securityContext.UserId);

            if (!string.IsNullOrWhiteSpace(query.SearchKey))
            {
                filter &= builder.Regex(nameof(PraxisUser.DisplayName), $"/{query.SearchKey}/i");
            }

            return filter;
        }
    }
}
