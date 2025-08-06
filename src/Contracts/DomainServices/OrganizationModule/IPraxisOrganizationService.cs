using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IPraxisOrganizationService
    {
        Task<bool> UpdateOrganizationAdminIds(string orgId, string userEmail, string userStatus);
        Task UpdateAdminDeputyAdminId(string orgId, string praxisUserItemId, string designation);
        Task UpdateOrganizationLogoThumbnails(Connection fileConnection, List<PraxisImageThumbnail> thumbnails);
    }
}
