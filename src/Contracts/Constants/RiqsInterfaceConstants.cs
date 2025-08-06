using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public static class RiqsInterfaceConstants
    {
        public const string MicrosoftProvider = "Microsoft";
        public const string GoogleProvider = "Google";

        public static readonly Dictionary<string, string> GoogleDriveMimeTypeToExtension = new()
        {
            { "application/vnd.google-apps.document", ".docx" },
            { "application/vnd.google-apps.spreadsheet", ".xlsx" },
            { "application/vnd.google-apps.presentation", ".pptx" },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
            { "application/msword", ".doc" },
            { "application/pdf", ".pdf" },
            { "application/vnd.ms-powerpoint", ".ppt" },
            { "application/vnd.openxmlformats-officedocument.presentationml.slideshow", ".pptx" },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx" },
            { "text/csv", ".csv" },
            { "application/vnd.ms-excel", ".xls" },
            { "application/vnd.ms-excel.addin.macroenabled.12", ".xlam" },
            { "image/jpeg", ".jpeg" },
            { "image/png", ".png" },
            { "image/gif", ".gif" },
            { "image/bmp", ".bmp" },
            { "video/x-flv", ".flv" },
            { "video/x-m4v", ".m4v" },
            { "video/x-matroska", ".mkv" },
            { "video/webm", ".webm" },
            { "video/mp4", ".mp4" }
        };

        public static bool IsValidGoogleDriveMimeType(string mimeType)
        {
            var validMimeTypes = new HashSet<string>
            {
                "application/vnd.google-apps.folder",
                "application/vnd.google-apps.document",
                "application/vnd.google-apps.spreadsheet",
                "application/vnd.google-apps.presentation",
                "application/msword",  
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",  
                "application/pdf",  
                "application/vnd.ms-powerpoint",  
                "application/vnd.openxmlformats-officedocument.presentationml.slideshow",  
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",  
                "text/csv",  
                "application/vnd.ms-excel",  
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",  
                "application/vnd.ms-excel.addin.macroenabled.12",  
                "image/jpeg",  
                "image/png",  
                "image/gif",  
                "image/bmp",  
                "video/x-flv",  
                "video/x-m4v", 
                "video/x-matroska",  
                "video/webm",  
                "video/mp4"
            };

            return validMimeTypes.Contains(mimeType);
        }

        public static string GoogleDriveExtensionToExportableMimeType(string fileExtension) => fileExtension.ToLower() switch
        {
            ".csv" => "text/csv",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppsx" => "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => null
        };
    }
}
