using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ExternalUser;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices
{
    public class ExternalUserCreateService : IExternalUserCreateService
    {
        private readonly ILogger<ExternalUserCreateService> _logger;
        private readonly IRepository _repository;

        public ExternalUserCreateService(
            ILogger<ExternalUserCreateService> logger,
            IRepository repository
        )
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task ProcessDataForCirsReport(CirsGenericReport report, PraxisClient client, string relatedEntityName)
        {
            try
            {
                var suppliers = report?.ExternalReporters?.Select(e => e.SupplierInfo)?.ToList();
                if (suppliers?.Count > 0)
                {
                    var roles = GetCirsReportRoles(client?.ItemId, report);

                    foreach (var supplier in suppliers)
                    {
                        var externalUser = GetExternalUser(supplier.SupplierId, relatedEntityName, client.ItemId);
                        if (externalUser == null)
                        {
                            var externalUserId = Guid.NewGuid().ToString();
                            var plainTextBytes = Encoding.UTF8.GetBytes(externalUserId);
                            var clientSecret = Convert.ToBase64String(plainTextBytes);

                            externalUser = new ExternalUser()
                            {
                                ItemId = externalUserId,
                                Email = supplier.SupplierEmail,
                                SupplierId = supplier.SupplierId,
                                PraxisClientId = client?.ItemId,
                                Roles = roles?.ToArray(),
                                RelatedEntityName = relatedEntityName,
                                ClientSecretId = externalUserId,
                                ClientSecret = clientSecret
                            };
                            await _repository.SaveAsync(externalUser);
                            await SaveClientCredential(externalUser);
                        }
                        else
                        {
                            externalUser.Roles = externalUser.Roles?.Union(roles)?.ToArray() ?? new string[] {};
                            await _repository.UpdateAsync(x => x.ItemId == externalUser.ItemId, externalUser);

                            await SaveClientCredential(externalUser);
                        }
                        supplier.ExternalUserId = externalUser.ItemId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during external user creation.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        private async Task SaveClientCredential(ExternalUser externalUser)
        {
            try
            {
                var clientCredential = await _repository.GetItemAsync<ClientCredential>(c => c.ItemId == externalUser.ItemId && !c.IsMarkedToDelete);
                if (clientCredential == null)
                {
                    clientCredential = new ClientCredential()
                    {
                        ItemId = externalUser.ItemId,
                        Tags = new string[] { TagName.IsAClientCredential },
                        Roles = externalUser.Roles,
                        ClientSecret = externalUser.ClientSecret
                    };
                    await _repository.SaveAsync(clientCredential);
                }
                else
                {
                    clientCredential.Roles = externalUser.Roles;
                    await _repository.UpdateAsync(c => c.ItemId == clientCredential.ItemId, clientCredential);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred on SaveClientCredential.");
                _logger.LogError("Exception Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        } 

        private List<string> GetCirsReportRoles(string clientId, CirsGenericReport report)
        {
            var roles = new List<string>()
            {
                RoleNames.Anonymous,
                RoleNames.AppUser,
                RoleNames.ExternalUser,
                $"{RoleNames.MpaGroup_Dynamic}_{clientId}",
                report.CirsDashboardName.ToString()
            };
            return roles;
        }

        private ExternalUser GetExternalUser(string supplierId, string relatedEntityName, string clientId)
        {
            return _repository.GetItem<ExternalUser>
                    (e => e.SupplierId == supplierId && e.RelatedEntityName == relatedEntityName && e.PraxisClientId == clientId);
        }
    }
}