using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces
{
    public class GetItemsResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public ModifiedUser LastModifiedBy { get; set; }
        public bool IsFolder { get; set; }
    }

    public class ModifiedUser
    {
        public string EmailAddress { get; set; }
        public string DisplayName { get; set; }
    }
}
