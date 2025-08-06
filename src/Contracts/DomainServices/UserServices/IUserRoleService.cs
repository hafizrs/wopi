using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.UserServices
{
   public interface IUserRoleService
    {
        List<string> GetPersonaRoles(List<PraxisClientInfo> clientList, List<PaymentClientRelation> paymentClientRelation = null);
        string GetDeleteFeatureRole(string clientId);
        List<string> PrepareAdminBRoles(List<PraxisClientInfo> clientList, bool isGroupAdmin= false);
        List<string> GetOrganizationWideRoles(List<PraxisClientInfo> clientList);
        string CreateRole(string role, bool isDynamic, string staticRole);
    }
}
