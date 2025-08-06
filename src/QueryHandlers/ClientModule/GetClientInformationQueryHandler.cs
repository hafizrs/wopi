using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetClientInformationQueryHandler : IQueryHandler<GetClientInformationQuery, ClientInformationResponse>
    {
        private readonly ILogger<GetClientInformationQueryHandler> _logger;
        private readonly IRepository _repository;

        public GetClientInformationQueryHandler(
            ILogger<GetClientInformationQueryHandler> logger,
            IRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        [Invocable]
        public ClientInformationResponse Handle(GetClientInformationQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
                nameof(GetClientInformationQueryHandler), JsonConvert.SerializeObject(query));
            
            ClientInformationResponse response;
            try
            {
                var clientInformationList = IsAAdmiBUser(query.PersonaNames)? GetOrganizationList (query.PersonaNames) : GetDepartmentList(query.PersonaNames);

                if (query.PersonaNames.Count() == clientInformationList.Count)
                {
                    response = new ClientInformationResponse
                    {
                        StatusCode = 200,
                        Message = string.Empty,
                        Results = clientInformationList.OrderBy(client => client.Title).ToList()
                    };
                }
                else
                {
                    response = new ClientInformationResponse
                    {
                        StatusCode = 500,
                        Message = "Exception occured during process client information by persona names.",
                        Results = new List<ClientInformation>()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                    nameof(GetClientInformationQueryHandler), ex.Message, ex.StackTrace);
                
                response = new ClientInformationResponse
                {
                    StatusCode = 500,
                    Message = "Exception occured during process client information by persona names.",
                    Results = new List<ClientInformation>()
                };
            }
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetClientInformationQueryHandler), JsonConvert.SerializeObject(response));
            return response;
        }

        public Task<ClientInformationResponse> HandleAsync(GetClientInformationQuery query)
        {
            throw new NotImplementedException();
        }

        private bool IsAAdmiBUser(IEnumerable<string> personaNames)
        {
            return personaNames.All(name => name.Contains(RoleNames.AdminB_Dynamic));
        }

        private List<ClientInformation> GetOrganizationList(IEnumerable<string> personaNames)
        {
            var orgInformationList = new List<ClientInformation>();

            foreach (var personaName in personaNames)
            {
                var orgInformation = new ClientInformation();

                var organizationId = personaName.Split('_')[1];
                var organization = _repository.GetItem<PraxisOrganization>(c => c.ItemId == organizationId && !c.IsMarkedToDelete);
                if (organization != null)
                {
                    orgInformation.ItemId = organization.ItemId;
                    orgInformation.Title = organization.ClientName;
                    orgInformation.Role = personaName;
                    orgInformation.Logo = organization.Logo;

                    orgInformationList.Add(orgInformation);
                }
            }

            return orgInformationList;
        }

        private List<ClientInformation> GetDepartmentList(IEnumerable<string> personaNames)
        {
            var clientInformationList = new List<ClientInformation>();

            foreach (var personaName in personaNames)
            {
                var clientInformation = new ClientInformation();

                var clientId = personaName.Split('_')[1];
                var client = _repository.GetItem<PraxisClient>(c => c.ItemId == clientId && !c.IsMarkedToDelete);
                if (client != null)
                {
                    clientInformation.ItemId = client.ItemId;
                    clientInformation.Title = client.ClientName;
                    clientInformation.Role = personaName;
                    clientInformation.Logo = client.Logo;

                    clientInformationList.Add(clientInformation);
                }
            }

            return clientInformationList;
        }
    }
}
