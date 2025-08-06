using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public record ProjectedClientResponse(
        string ParentOrganizationId,
        string ParentOrganizationName,
        string ClientName,
        string ClientNumber,
        string MemberPhysicianNetwork,
        string WebPageUrl,
        string MedicalSoftware,
        string ComputerSystem,
        PraxisImage Logo,
        bool IsSameAddressAsParentOrganization,
        PraxisAddress Address,
        string ContactEmail,
        string ContactPhone,
        IEnumerable<ClientAdditionalInfo> AdditionalInfos,
        IEnumerable<ItemIdAndTitle> PraxisUserAdditionalInformationTitles,
        IEnumerable<string> CompanyTypes,
        IEnumerable<NavigationDto> Navigations,
        bool? IsOpenOrganization,
        bool? IsOrgTypeChangeable,
        bool IsCreateUserEnable,
        int UserLimit,
        int AuthorizedUserLimit,
        int UserCount,
        CirsReportConfigModel CirsReportConfig,
        bool IsSubscriptionExpired,
        string AdminUserId,
        string DeputyAdminUserId,
        List<PraxisIdDto> CirsAdminIds,
        string CreatedBy,
        DateTime CreateDate,
        string ItemId,
        DateTime LastUpdateDate
    );

}
