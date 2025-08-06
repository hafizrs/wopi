using Microsoft.Extensions.DependencyInjection;
using System;

namespace Selise.Ecap.SC.Wopi.Contracts.Infrastructure
{
    public static class ServiceLocator
    {
        private static IServiceProvider _provider;

        public static void Initialize(IServiceProvider provider)
        {
            _provider = provider;
        }

        public static T GetService<T>()
        {
            return _provider.GetService<T>();
        }
    }
}