using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    class PraxisImageDimension
    {
        protected PraxisImageDimension() { }
        public static Dictionary<string, Dimension> Dimensions { get; set; } = new Dictionary<string, Dimension>()
        {
            { "Resize-Image-1024-1024", new Dimension { Width = 1024, Height = 1024 } }

        };
    }
}
