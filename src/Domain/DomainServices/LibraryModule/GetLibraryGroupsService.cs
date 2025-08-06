using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class GetLibraryGroupsService : IGetLibraryGroupsService
    {
        private readonly IRepository _repository;

        public GetLibraryGroupsService
        (
            IRepository repository
        )
        {
            _repository = repository;
        }

        public Task<GetLibraryGroupsResponse> GetLibraryGroupsAsync(GetLibraryGroupsQuery query)
        {
            var orgReadRole = $"{RoleNames.Organization_Read_Dynamic}_{query.OrganizationId}";
            var allGroups = _repository.GetItems<RiqsLibraryGroup>(x => x.OrganizationId == query.OrganizationId && x.RolesAllowedToRead.Contains(orgReadRole));

            var mainGroups = allGroups.Where(x => x.GroupType == Contracts.Constants.LibraryGroupType.MAIN_GROUP)
                                   .Select(MapToRiqLibraryGroupResponse)
                                   .ToList();

            var subGroups = allGroups.Where(x => x.GroupType == Contracts.Constants.LibraryGroupType.SUB_GROUP)
                                  .Select(MapToRiqLibraryGroupResponse)
                                  .ToList();

            var subSubGroups = allGroups.Where(x => x.GroupType == Contracts.Constants.LibraryGroupType.SUB_SUB_GROUP)
                                     .Select(MapToRiqLibraryGroupResponse)
                                     .ToList();

            var libraryGroupsResponse = new GetLibraryGroupsResponse()
            {
                OrganizationId = query.OrganizationId,
                Groups = mainGroups,
                SubGroups = subGroups,
                SubSubGroups = subSubGroups
            };

            return Task.FromResult(libraryGroupsResponse);
        }

        private RiqsLibraryGroupResponse MapToRiqLibraryGroupResponse(RiqsLibraryGroup group)
        {
            var response = new RiqsLibraryGroupResponse()
            {
                Id = group.ItemId,
                Name = group.Name,
                ParentId = group.ParentId
            };

            return response;
        }
    }
}
