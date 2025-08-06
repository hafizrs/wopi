using System;

namespace Selise.Ecap.SC.WopiMonitor.Contracts.Models.WopiModule
{
    public class WopiFileInfo
    {
        public string BaseFileName { get; set; }
        public long Size { get; set; }
        public string OwnerId { get; set; }
        public string UserId { get; set; }
        public bool UserCanWrite { get; set; }
        public bool UserCanRename { get; set; }
        public bool UserCanNotWriteRelative { get; set; }
        public string Version { get; set; }
        public string UserFriendlyName { get; set; }
        public string PostMessageOrigin { get; set; }
        public bool EnableOwnerTermination { get; set; }
        public bool SupportsLocks { get; set; }
        public bool SupportsGetLock { get; set; }
        public bool SupportsExtendedLockLength { get; set; }
        public bool SupportsCobalt { get; set; }
        public bool SupportsUpdate { get; set; }
        public bool UserCanPresent { get; set; }
    }
} 