using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public class LibraryModuleFileFormats
    {
        public static IEnumerable<string> GetImageTypes()
        {
            return new[] { "jpg", "jpeg", "png", "x-icon", "gif", "bmp" };
        }

        public static IEnumerable<string> GetVideoTypes()
        {
            return new[]
            {
                "flv", "m4v", "mkv", "webm", "mp4", "3gp", "MPEG-4", "video/mp4", "video/mov", "video/x-mpeg",
                "video/mpeg", "video/MPEG-4", "video/3gpp"
            };
        }

        public static IEnumerable<string> GetPdfTypes()
        {
            return new[] { "pdf" };
        }

        public static IEnumerable<string> GetExcelsTypes()
        {
            return new[] { "csv", "xls", "xlsx", "excel", "xla" };
        }

        public static IEnumerable<string> GetOtherTypes()
        {
            return new[] { "txt", "plain", "richtext", "asc", "msg" };
        }

        public static IEnumerable<string> GetPptTypes()
        {
            return new[] { "pps", "mspowerpoint", "ppsx", "pptx" };
        }

        public static IEnumerable<string> GetWordTypes()
        {
            return new[]
            {
                "doc",
                "docx"
            };
        }

        public static string GetFileExtension(string fileName)
        {
            if (fileName == null) return string.Empty;
            var extension = Path.GetExtension(fileName).ToLower();
            return !string.IsNullOrEmpty(extension) ? extension[1..] : string.Empty;
        }

        public static LibraryFileTypeEnum GetFileFormat(string extension)
        {
            if (GetPdfTypes().Contains(extension))
            {
                return LibraryFileTypeEnum.PDF;
            }

            if (GetWordTypes().Contains(extension))
            {
                return LibraryFileTypeEnum.DOCUMENT;
            }

            if (GetImageTypes().Contains(extension))
            {
                return LibraryFileTypeEnum.IMAGE;
            }

            if (GetExcelsTypes().Contains(extension))
            {
                return LibraryFileTypeEnum.EXCELS;
            }

            if (GetPptTypes().Contains(extension))
            {
                return LibraryFileTypeEnum.PPT;
            }

            return GetVideoTypes().Contains(extension) ? LibraryFileTypeEnum.VIDEO : LibraryFileTypeEnum.OTHER;
        }
    }
}
