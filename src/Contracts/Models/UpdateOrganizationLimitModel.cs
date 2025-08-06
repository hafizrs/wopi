using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class UpdateOrganizationLimitModel
    {
        public string OrganizationId { get; set; }
        public int TotalDepartmentUserLimit { get; set; }
        public int UserCount { get; set; }
        public double StorageLimit { get; set; }
        public double LanguageTokenLimit { get; set; }
        public double ManualTokenLimit { get; set; }
    }
} 
