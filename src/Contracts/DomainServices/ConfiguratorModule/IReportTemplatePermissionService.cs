using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule
{
    public interface IReportTemplatePermissionService
    {
        Task<ReportTemplatePermissionRecord> ReportTemplatePermissions(string clientId, string organizationId);
        Task<EquipmentReportPermissionRecord> EquipmentReportPermissions(string clientId, string organizationId, string equipmentId);
    }
}
