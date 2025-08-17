using System;

namespace Selise.Ecap.SC.Wopi.Contracts.Models.WopiModule
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
        
        // CRITICAL MISSING PROPERTIES for Collabora to recognize writable files
        public bool SupportsPutFile { get; set; }
        public bool SupportsUnlock { get; set; }
        public bool SupportsRefreshLock { get; set; }
        public bool SupportsGetFile { get; set; }
        public bool SupportsCheckFileInfo { get; set; }
        public bool SupportsDeleteFile { get; set; }
        public bool SupportsRenameFile { get; set; }
        public bool SupportsPutRelativeFile { get; set; }
        public bool SupportsGetFileWopiSrc { get; set; }
        public bool SupportsExecuteCobaltRequest { get; set; }
        public bool SupportsUserInfo { get; set; }
        public bool SupportsFolders { get; set; }
        public bool SupportsFileCreation { get; set; }
        
        // Additional critical properties for Collabora
        public bool AllowWrite { get; set; }
        
        // Custom UI hiding properties (may be recognized by Collabora)
        public bool HideFileMenu { get; set; }
        public bool HideHelpMenu { get; set; }
        public bool HideToolsMenu { get; set; }
        public bool HideViewMenu { get; set; }
        public bool HideUserList { get; set; }
        public bool DisableCopy { get; set; }
        public bool DisablePrint { get; set; }
        public bool DisableExport { get; set; }
        public bool DisableSave { get; set; }
        public bool EnableShare { get; set; }
        public bool EnableInsertRemoteImage { get; set; }
    }
} 