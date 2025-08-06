using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class GetCirsAdminsService : IGetCirsAdminsService
{
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IRepository _repository;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    private readonly ICirsPermissionService _cirsPermissionService;

    public GetCirsAdminsService(
        ISecurityContextProvider securityContextProvider,
        IRepository repository,
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        ICirsPermissionService cirsPermissionService)
    {
        _securityContextProvider = securityContextProvider;
        _repository = repository;
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _cirsPermissionService = cirsPermissionService;
    }

    public async Task<List<CirsAdminsResponse>> GetCirsAdmins(GetCirsAdminsQuery query)
    {
        var permission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(query.PraxisClientId, query.DashboardNameEnum);
        var appointedAdminPraxisUserIds = permission?.AdminIds?.Select(id => id.PraxisUserId)?.ToList() ?? new List<string>();

        var praxisUsers = await GetPraxisUsers(query, appointedAdminPraxisUserIds);
        var cirsAdmins = await PrepareCirsAdminResponse(query, appointedAdminPraxisUserIds, praxisUsers);
        return cirsAdmins;
    }

    private async Task<List<PraxisUser>> GetPraxisUsers(
        GetCirsAdminsQuery query,
        List<string> adminPraxisUserIds = null,
        bool isExcludeIds = true,
        bool isPermissionCheckRequired = true)
    {
        var riqsIncidentRepo =
            _ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisUser>("PraxisUsers");
        var filter = GetPraxisUserQueryFilter(query, adminPraxisUserIds, isExcludeIds, isPermissionCheckRequired);
        var projection = GetPraxisUserQueryProjection();

        var praxisUserDocuments = await riqsIncidentRepo
            .Find(filter)
            .Sort("{DisplayName: 1}")
            .Project(projection)
            .ToListAsync();

        var praxisUsers = praxisUserDocuments.Select(i => BsonSerializer.Deserialize<PraxisUser>(i)).ToList();
        return praxisUsers;
    }

    private FilterDefinition<PraxisUser> GetPraxisUserQueryFilter(
        GetCirsAdminsQuery query,
        List<string> adminPraxisUserIds,
        bool isExcludeIds,
        bool isPermissionCheckRequired)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        var builder = Builders<PraxisUser>.Filter;
        var filter =
            builder.ElemMatch(pu => pu.ClientList, Builders<PraxisClientInfo>.Filter.Eq(pci => pci.ClientId, query.PraxisClientId)) &
            builder.Eq(pu => pu.IsMarkedToDelete, false) &
            builder.Eq(pu => pu.Active, true) &
            builder.AnyIn(pu => pu.Roles, query.Roles);

        if (!query.Roles.Contains(RoleNames.AdminB))
        {
            filter &= builder.Not(builder.AnyEq(pu => pu.Roles, RoleNames.AdminB));
        }
        filter &= builder.Not(builder.AnyEq(pu => pu.Roles, RoleNames.GroupAdmin));

        if (adminPraxisUserIds != null && adminPraxisUserIds.Count > 0)
        {
            filter &= isExcludeIds ?
                builder.Not(builder.In(pu => pu.ItemId, adminPraxisUserIds)) :
                builder.In(pu => pu.ItemId, adminPraxisUserIds);
        }

        if (isPermissionCheckRequired)
        {
            filter &= builder.AnyIn(pu => pu.RolesAllowedToRead, securityContext.Roles) |
                builder.AnyEq(pu => pu.IdsAllowedToRead, securityContext.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchKey))
        {
            filter &= builder.Regex(nameof(PraxisUser.DisplayName), $"/{query.SearchKey}/i");
        }

        return filter;
    }

    private ProjectionDefinition<PraxisUser> GetPraxisUserQueryProjection()
    {
        return
            Builders<PraxisUser>.Projection
            .Include(r => r.ItemId)
            .Include(r => r.DisplayName);
    }

    private async Task<List<CirsAdminsResponse>> PrepareCirsAdminResponse(
        GetCirsAdminsQuery query,
        List<string> currentAdminIds,
        List<PraxisUser> praxisUsers)
    {
        var cirsAdmins = new List<CirsAdminsResponse>();

        if (currentAdminIds != null && currentAdminIds.Count > 0)
        {
            var currentAdmins = await PrepareCurrentCirsAdminData(query, currentAdminIds);
            cirsAdmins.AddRange(currentAdmins);
        }

        var cirsAdminEligibleUsers = PrepareCirsAdminEligibleUsers(praxisUsers);
        cirsAdmins.AddRange(cirsAdminEligibleUsers);

        return cirsAdmins;
    }

    private async Task<List<CirsAdminsResponse>> PrepareCurrentCirsAdminData(GetCirsAdminsQuery query, List<string> currentAdminIds)
    {
        var cirsAdmins = new List<CirsAdminsResponse>();
        var currentAdmins = await GetPraxisUsers(query, currentAdminIds, false, false);
        var accessableAdmins = await GetPraxisUsers(query, currentAdminIds, false, true);
        currentAdmins.ForEach(ca =>
        {
            var cirsAdmin = new CirsAdminsResponse()
            {
                PraxisUserId = ca.ItemId,
                DisplayName = ca.DisplayName,
                IsAAdmin = true,
                IsChangeable = accessableAdmins.FirstOrDefault(aa => aa.ItemId == ca.ItemId) != null,
            };
            cirsAdmins.Add(cirsAdmin);
        });
        return cirsAdmins;
    }

    private List<CirsAdminsResponse> PrepareCirsAdminEligibleUsers(List<PraxisUser> praxisUsers)
    {
        var users = new List<CirsAdminsResponse>();
        praxisUsers.ForEach(praxisUser =>
        {
            var user = new CirsAdminsResponse()
            {
                PraxisUserId = praxisUser.ItemId,
                DisplayName = praxisUser.DisplayName,
                IsAAdmin = false,
                IsChangeable = true
            };
            users.Add(user);
        });
        return users;
    }
}