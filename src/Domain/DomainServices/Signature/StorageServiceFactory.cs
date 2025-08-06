using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Utils
{
    public class StorageServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        public StorageServiceFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }
        public StorageDataService Create(bool isBlocksService)
        {
            var serviceClient = _serviceProvider.GetRequiredService<IServiceClient>();
            var logger = _serviceProvider.GetRequiredService<ILogger<StorageDataService>>();
            var securityContextProvider = _serviceProvider.GetRequiredService<ISecurityContextProvider>();
            var accessTokenProvider = _serviceProvider.GetRequiredService<AccessTokenProvider>();
            var storageServiceBaseUrl = _configuration["StorageServiceBaseUrl"];
           

            if (isBlocksService)
            {
                storageServiceBaseUrl = new UrlFactoryProvider(_configuration).GetUrl(isBlocksService, storageServiceBaseUrl);
            }

            var storageDataService = new StorageDataService(
                serviceClient,
                logger,
                _configuration,
                securityContextProvider,
                accessTokenProvider
            );
            storageDataService.UpdateStorageBaseUrl(storageServiceBaseUrl);

            return storageDataService;
        }
    }
}

