using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule
{
    public class RiqsPediaViewControl : EntityBase
    {
        public string PraxisUserId { get; set; }
        public string UserId { get; set;}
        public bool ViewState { get; set; }
    }
}
