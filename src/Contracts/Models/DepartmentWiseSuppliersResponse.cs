using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DepartmentWiseSuppliersResponse
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string OrganizationId { get; set; }
        public string Organizationname { get; set; }
        public List<CategoryWiseSuppliers> CategoryWiseSuppliers { get; set; }
    }

    public class CategoryWiseSuppliers
    {
        public string CategoryKey { get; set; }
        public List<MinimalSupplierDetail> Suppliers { get; set; }
    }

    public class MinimalSupplierDetail
    {
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
    }
}