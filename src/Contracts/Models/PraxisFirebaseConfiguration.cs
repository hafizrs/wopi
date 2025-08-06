using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisFirebaseConfiguration : EntityBase
    {
        public List<PackageList> PackageList { get; set; }
    }

    public class PackageList
    {
        public string PackageName { get; set; }
        public string ApiKey { get; set; }
        public string DomainUriPrefix { get; set; }
        public string AndroidPackageName { get; set; }
        public IosInformation IosInfo { get; set; }
    }

    public class IosInformation
    {
        public string IosBundleId { get; set; }
        public string IosAppStoreId { get; set; }
    }
}
