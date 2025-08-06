using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetLibraryAdminsQueryHandler : IQueryHandler<GetPraxisOrganizationUserQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetLibraryAdminsQueryHandler> _logger;
        private readonly IPraxisOrganizationUserService _praxisOrganizationUserService;

        public GetLibraryAdminsQueryHandler(
            ILogger<GetLibraryAdminsQueryHandler> logger,
            IPraxisOrganizationUserService praxisOrganizationUserService)
        {
            _logger = logger;
            _praxisOrganizationUserService = praxisOrganizationUserService;
        }

        public QueryHandlerResponse Handle(GetPraxisOrganizationUserQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetPraxisOrganizationUserQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter in {HandlerName} with query {QueryName}",
                nameof(GetLibraryAdminsQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                if (!IsAValidRequest(query))
                {
                    response.StatusCode = 1;
                    response.ErrorMessage = "Not a valid request";
                }
                else 
                {
                    List<PraxisUserResponse> praxisUsers = await _praxisOrganizationUserService.GetOrganizationUsers(query);
                    response.Data = praxisUsers;
                    response.StatusCode = 0;
                    response.TotalCount = praxisUsers.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in {HandlerName}. Exception message {Message}. Exception Details: {StackTrace}.",
                    nameof(GetLibraryAdminsQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Handled By {HandlerName} with response {Response}",
                nameof(GetLibraryAdminsQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }

        private bool IsAValidRequest(GetPraxisOrganizationUserQuery query)
        {
            return query != null &&
                !string.IsNullOrWhiteSpace(query.OrganizationId) &&
                query.Roles.Count > 0 &&
                query.Roles.TrueForAll(r => r == RoleNames.PowerUser || r == RoleNames.Leitung || r == RoleNames.AdminB || r == RoleNames.MpaGroup1 || r == RoleNames.MpaGroup2);
        }
    }
}
