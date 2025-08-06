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
    public record ProjectedPraxisClientCategoryResponse(
        string ClientId,
        string OrganizationId,
        string Name,
        string ParentId,
        IEnumerable<PraxisUser> ControllingGroup,
        IEnumerable<PraxisUser> ControlledGroup,
        IEnumerable<PraxisClientSubCategory> SubCategories,
        string CreatedBy,
        DateTime CreateDate,
        string ItemId,
        DateTime LastUpdateDate
    );

}