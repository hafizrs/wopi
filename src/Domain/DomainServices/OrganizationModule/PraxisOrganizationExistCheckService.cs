using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisOrganizationExistService : IPraxisOrganizationExistCheckService
    {
        private readonly IRepository _repository;

        public PraxisOrganizationExistService(
            IRepository repository)
        {
            _repository = repository;
        }

        public async Task<QueryHandlerResponse> CheckOrganizationNameExistance(string organizationName, string organizationId)
        {
            if (organizationId != null)
            {
                var organization = await FindOrganizationById(organizationId);
                if (organization == null)
                {
                    var response = new QueryHandlerResponse()
                    {
                        StatusCode = 1,
                        ErrorMessage = $"Organization not found with id: {organizationId}"
                    };
                    return response;
                }
                else
                {
                    var existingOrganization = await FindOrganizationByName(organizationName);
                    return NameCheckForExistingOrganization(existingOrganization, organizationId);
                }
            }
            else
            {
                var organization = await FindOrganizationByName(organizationName);
                return NameCheckForOrganization(organization);

            }
        }

        private async Task<PraxisOrganization> FindOrganizationById(string orgId)
        {
            var organization = await _repository.GetItemAsync<PraxisOrganization>(
                  o => !o.IsMarkedToDelete &&
                  o.ItemId == orgId);
            return organization;
        }

        private async Task<PraxisOrganization> FindOrganizationByName(string organizationName)
        {
            var organization = await _repository.GetItemAsync<PraxisOrganization>(
                  o => !o.IsMarkedToDelete &&
                  o.ClientName.Equals(organizationName));
            return organization;
        }

        private QueryHandlerResponse NameCheckForExistingOrganization(PraxisOrganization organization, string organizationId)
        {
            QueryHandlerResponse response = (organization == null || organization.ItemId == organizationId) ? OrganizationNonExistResponse()
                                            : OrganizationExistResponse();

            return response;

        }

        private QueryHandlerResponse NameCheckForOrganization(PraxisOrganization organization)
        {
            QueryHandlerResponse response = organization == null ? OrganizationNonExistResponse()
                                            : OrganizationExistResponse();
            return response;
        }

        private QueryHandlerResponse OrganizationNonExistResponse()
        {
            QueryHandlerResponse response = new QueryHandlerResponse()
            {
                Results = new
                {
                    OrganizationExist = false
                },
                StatusCode = 0
            };

            return response;
        }

        private QueryHandlerResponse OrganizationExistResponse()
        {
            QueryHandlerResponse response = new QueryHandlerResponse()
            {
                Results = new
                {
                    OrganizationExist = true
                },
                StatusCode = 0
            };
            return response;
        }
    }
}
