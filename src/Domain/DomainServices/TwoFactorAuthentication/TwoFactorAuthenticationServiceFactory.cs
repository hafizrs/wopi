using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.TwoFactorAuthentication
{
    public class TwoFactorAuthenticationServiceFactory : ITwoFactorAuthenticationServiceFactory
    {
        private readonly Dictionary<TwoFactorType, Func<ITwoFactorAuthenticationService>> _factories;

        public TwoFactorAuthenticationServiceFactory(
            Dictionary<TwoFactorType, Func<ITwoFactorAuthenticationService>> factories
            )
        {
            _factories = factories;
        }

        public ITwoFactorAuthenticationService GetService(TwoFactorType twoFactorType)
        {
            if (!_factories.TryGetValue(twoFactorType, out var factory) || factory is null)
            {
                throw new ArgumentOutOfRangeException(nameof(twoFactorType), $"type '{twoFactorType}' is not registered");
            }
            return factory();
        }
    }

}
