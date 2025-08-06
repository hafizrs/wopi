using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class UpdateSupplierInterfaceAdditioanalDataCommand
    {
        [Required]
        public List<string> SupplierIds { get; set; }
        [Required]
        public string MigrationSummaryId { get; set; }
        public string ClientId { get; set; }
        public string CategoryKey { get; set; }
        public string CategoryName { get; set; }
    }
}
