using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsTranslations
{
    public class RiqsTranslation : EntityBase
    {
        public List<Translation> Translations { get; set; }
        public List<BillingHistory> BillingHistories { get; set; } 

    }
}

public class Translation
{
    public string HashKey { get; set; }
    public string Text { get; set; }
    public string LangKey { get; set; }
    public List<string> PraxisClientIds { get; set; }
    public List<string> OrganizationIds { get; set; }
}

public class BillingHistory
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public List<string> LangKeys { get; set; }
}