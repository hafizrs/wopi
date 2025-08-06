using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsTranslations
{
    public interface IRiqsTranslationService
    {
        Task<List<RiqsTranslationResponse>> RiqsTranslation(GetRiqsTranslationCommand command);
    }
}
