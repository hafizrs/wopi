using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.RiqsInterfaces
{
    public class GetDriveItemsResponse
    {
        public List<DriveItem> files { get; set; }
    }

    public class DriveItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<string> parents { get; set; }
        public string mimeType { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime modifiedTime { get; set; }
        public DriveModifiedUser lastModifyingUser { get; set; }
        public bool IsFolder => mimeType == "application/vnd.google-apps.folder";
    }

    public class DriveModifiedUser
    {
        public string emailAddress { get; set; }
        public string displayName { get; set; }
    }
}
