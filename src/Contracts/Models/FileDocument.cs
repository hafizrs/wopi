using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class FileDocument
    {
        public string FilePath { get; set; }
        public string FileContent { get; set; }
        public DateTime UploadedAt { get; set; }
    }

}
