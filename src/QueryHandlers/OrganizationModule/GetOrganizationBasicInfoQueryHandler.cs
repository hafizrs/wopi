using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetOrganizationBasicInfoQueryHandler : IQueryHandler<GetOrganizationBasicInfoQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetOrganizationBasicInfoQueryHandler> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityHelperService _securityHelperService;

        public GetOrganizationBasicInfoQueryHandler(
            ILogger<GetOrganizationBasicInfoQueryHandler> logger,
            IRepository repository,
            ISecurityHelperService securityHelperService)
        {
            _logger = logger;
            _repository = repository;
            _securityHelperService = securityHelperService;
        }

        public QueryHandlerResponse Handle(GetOrganizationBasicInfoQuery query)
        {
            QueryHandlerResponse response = new QueryHandlerResponse();

            _logger.LogInformation("Enter {HandlerName} with query: {Query}",
                nameof(GetOrganizationBasicInfoQueryHandler), JsonConvert.SerializeObject(query));
            try
            {
                var organizations = GetOrganizations(query);
                response.Data = organizations;
                response.StatusCode = 0;
                response.TotalCount = organizations.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName} Exception Message: {ErrorMessage} Exception Details: {StackTrace}.",
                    nameof(GetOrganizationBasicInfoQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }
            
            _logger.LogInformation("Handled By {HandlerName} with response: {Response}",
                nameof(GetOrganizationBasicInfoQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }

        public Task<QueryHandlerResponse> HandleAsync(GetOrganizationBasicInfoQuery query)
        {
            throw new NotImplementedException();
        }

        private List<OrganizationBasicInfoResponse> GetOrganizations(GetOrganizationBasicInfoQuery query)
        {
            var orgIds = query.OrganizationIds ?? new List<string>();

            if (!string.IsNullOrEmpty(query.OrganizationId))
            {
                orgIds.Add(query.OrganizationId);
            }
            if (query.UseImpersonate && !_securityHelperService.IsAAdmin())
            {
                var loggedInOrgIds = _securityHelperService.ExtractOrganizationIdsFromOrgLevelUser();
                orgIds = orgIds.Where(o => loggedInOrgIds.Contains(o)).ToList();
            }
            return _repository.GetItems<PraxisOrganization>(o => !o.IsMarkedToDelete && orgIds.Contains(o.ItemId))
                .OrderBy(o => o.ClientName)
                .Select(o => new OrganizationBasicInfoResponse 
                { 
                    ItemId = o.ItemId,
                    ClientName = o.ClientName,
                    Address = o.Address,
                    ReportingConfigurations = !string.IsNullOrEmpty(query.OrganizationId) ? o.ReportingConfigurations : null,
                    HaveAdditionalPurchasePermission = o.HaveAdditionalPurchasePermission,
                    ExternelReportingOffices = !string.IsNullOrEmpty(query.OrganizationId) ? o.ExternelReportingOffices : null,
                    HaveAdditionalAllocationPermission = o.HaveAdditionalAllocationPermission
                }).ToList();
        }
    }
}
