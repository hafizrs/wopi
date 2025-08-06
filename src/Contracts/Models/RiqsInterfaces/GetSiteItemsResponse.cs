using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces
{
    public class GetSiteItemsResponse
    {
        public List<ItemInfo> value { get; set; }
    }

    public class ItemInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public LastModifiedUser lastModifiedBy { get; set; }
        public Folder folder { get; set; }
        public bool isFolder => folder != null;
    }

    public class LastModifiedUser
    {
        public ApplicationInfo application { get; set; }
        public UserInfo user { get; set; }
    }

    public class ApplicationInfo
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class UserInfo
    {
        public string email { get; set; }
        public string displayName { get; set; }
    }

    public class Folder
    {
        public int childCount { get; set; }
    }
}
