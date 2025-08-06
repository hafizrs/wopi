using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsTranslationResponse
    {
        public bool IsValid { get; set; }  
        public bool IsTranslated { get; set; } 
        public string OriginalText { get; set; } 
        public string OriginalLangKey { get; set; }  
        public string TranslatedLangKey { get; set; } 
        public string TranslatedText { get; set; }   
        public string ErrorMessage { get; set; }
    }
}

public class RiqsExternalTranslation 
{
    public string OriginalText { get; set; }
    public string OriginalTextHashKey { get; set; }
    public string OriginalLangKey { get; set; }
    public string TranslatedText { get; set; }
    public string TranslatedLangKey { get; set; } 
    public string TranslatedTextHashKey { get; set; }

}
