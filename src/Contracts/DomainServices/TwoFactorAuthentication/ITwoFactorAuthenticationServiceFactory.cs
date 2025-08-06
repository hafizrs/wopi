using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication
{
    public interface ITwoFactorAuthenticationServiceFactory
    {
        ITwoFactorAuthenticationService GetService(TwoFactorType twoFactorType); 
    }
}
