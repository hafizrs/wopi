using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DepartmentWiseUserAdditionalInfosResponse
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public List<MinimalUserAdditionalInfo> UserAdditionalInfos { get; set; }
    }

    public class MinimalUserAdditionalInfo
    {
        public string AdditionalId { get; set; }
        public string AdditionalName { get; set; }
    }
}