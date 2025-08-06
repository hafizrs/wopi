using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GetRiqsTranslationCommand
    {
        public string PraxisClientId { get; set; }
        public string OrganizationId { get; set; } 
        public List<string> Texts { get; set; } = new List<string>();
        public List<string> TranslateLangKeys { get; set; } 
    }
}
