using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface ITranslationService
    {
        Task<TranslationResponse> GetTranslation(TranslationPayload payload);  
        Task<List<TranslationResponse>> GetTranslationMultiple(TranslationMultiplePayload payload);   
    }
}
