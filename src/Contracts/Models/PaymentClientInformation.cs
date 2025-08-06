using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PaymentClientInformation
    {
        public string FileName { get; set; }
        public byte[] FileContent { get; set; }
        public string FileId { get; set; }
        public PraxisClient ClientData { get; set; }
        public string NavigationProcessType { get; set; }
        public List<NavInfo> NavigationList { get; set; }
    }
}