using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteDataForUser : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly ILogger<DeleteDataForUser> _logger;
        private readonly IDeleteUserRelatedData _deleteUserRelatedDataService;
        private readonly IRepository _repository;
        private readonly IRevokePermissionByRoleStrategy _revokePermissionByRoleStrategyService;


        public DeleteDataForUser(
            ISecurityContextProvider securityContextProviderService,
            ILogger<DeleteDataForUser> logger,
            IDeleteUserRelatedData deleteUserRelatedDataService,
            IRepository repository,
            IRevokePermissionByRoleStrategy revokePermissionByRoleStrategyService
            )
        {
            _securityContextProviderService = securityContextProviderService;
            _logger = logger;
            _deleteUserRelatedDataService = deleteUserRelatedDataService;
            _repository = repository;
            _revokePermissionByRoleStrategyService = revokePermissionByRoleStrategyService;
        }

        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(User)} related all data with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");

            try
            {
                var response = _deleteUserRelatedDataService.DeleteData(itemId);
                if (response.Result.Item1)
                {
                    var praxisUser = await _repository.GetItemAsync<PraxisUser>(u => u.UserId == itemId);
                    if (praxisUser != null)
                    {
                        var userRoles = praxisUser.ClientList.Select(c => c.Roles).ToList();
                        var mergedRoles = userRoles.SelectMany(r => r).Distinct().ToList();

                        foreach (var role in mergedRoles)
                        {
                            var service = _revokePermissionByRoleStrategyService.GetService(role);
                            service?.RevokePermission(response.Result.Item2, response.Result.Item3).Wait();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Exception occured during delete {nameof(User)} related all data with ItemId: {itemId} " +
                    $"and tenantId: {securityContext.TenantId}." +
                    $"Exception Message: {ex.Message}. Exception details: {ex.StackTrace}."
                );
                return false;
            }

            return true;
        }
    }
}
