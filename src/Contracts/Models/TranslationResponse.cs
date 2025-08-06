using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TranslationResponse
    {
        public string text { get; set; }
        public bool is_error { get; set; }
        public string error_message { get; set; }
        public string[] target_languages { get; set; }
        public string current_language { get; set; }
        public List<TranslateInfo> translates { get; set; }
        public UsageMetadata usage_metadata { get; set; }
        public UsageMetadata calculated_usage_metadata { get; set; }
    }

    public class UsageMetadata
    {
        public int input_tokens { get; set; } 
        public int output_tokens { get; set; }
        public int total_tokens { get; set; } 
    }

    public class TranslateInfo
    {
        public string name { get; set; }
        public string translation { get; set; }

    }
}

