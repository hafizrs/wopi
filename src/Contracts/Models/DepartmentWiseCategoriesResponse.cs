using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DepartmentWiseCategoriesResponse
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string OrganizationId { get; set; }
        public string Organizationname { get; set; }
        public List<MinimalCategoryDetail> DepartmentCategories { get; set; }
    }

    public class MinimalCategoryDetail
    {
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
    }
}