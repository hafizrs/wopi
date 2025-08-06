using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Signature
{
    public class PraxisFileServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public PraxisFileServiceFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public PraxisFileService Create(bool isBlocksService)
        {
            var repository = _serviceProvider.GetRequiredService<IRepository>();
            var securityContextProvider = _serviceProvider.GetRequiredService<ISecurityContextProvider>();
            var serviceClient = _serviceProvider.GetRequiredService<IServiceClient>();
            var storageServiceBaseUrl = _configuration["StorageServiceBaseUrl"];
            if (isBlocksService)
            {
                storageServiceBaseUrl = new UrlFactoryProvider(_configuration).GetUrl(isBlocksService, storageServiceBaseUrl);
            }
            var praxisFileService = new PraxisFileService(repository, securityContextProvider, serviceClient, _configuration);
            praxisFileService.UpdateStorageBaseUrl(storageServiceBaseUrl);
            return praxisFileService;
        }
    }
}
