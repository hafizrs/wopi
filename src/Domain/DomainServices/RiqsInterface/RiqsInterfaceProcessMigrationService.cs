using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{

    public class RiqsInterfaceProcessMigrationService : IRiqsInterfaceProcessMigrationService
    {
        private readonly ILogger<RiqsInterfaceGoogleDriveMigrationService> _logger;
        private readonly IRepository _repository;
        private readonly IRiqsInterfaceTokenService _riqsInterfaceTokenService;
        private readonly IRiqsInterfaceGoogleDriveMigrationService _riqsInterfaceGoogleDriveMigrationService;
        private readonly IRiqsInterfaceSharePointMigrationService _riqsInterfaceSharePointMigrationService;

        public RiqsInterfaceProcessMigrationService(
            ILogger<RiqsInterfaceGoogleDriveMigrationService> logger,
            IRepository repository,
            IRiqsInterfaceTokenService riqsInterfaceTokenService,
            IRiqsInterfaceGoogleDriveMigrationService riqsInterfaceGoogleDriveMigrationService,
            IRiqsInterfaceSharePointMigrationService riqsInterfaceSharePointMigrationService)
        {
            _logger = logger;
            _repository = repository;
            _riqsInterfaceTokenService = riqsInterfaceTokenService;
            _riqsInterfaceGoogleDriveMigrationService = riqsInterfaceGoogleDriveMigrationService;
            _riqsInterfaceSharePointMigrationService = riqsInterfaceSharePointMigrationService;
        }

        public async Task<bool> InitiateProcessFileMigration(ProcessInterfaceMigrationCommand command)
        {
            try
            {
                var tokenInfo = await _riqsInterfaceTokenService.GetInterfaceTokenAsyncByUserId();

                if (tokenInfo == null || string.IsNullOrEmpty(tokenInfo.access_token)) return false;

                return tokenInfo.provider.ToLower() switch
                {
                    "microsoft" => await _riqsInterfaceSharePointMigrationService.ProcessFileMigration(command, tokenInfo),
                    "google" => await _riqsInterfaceGoogleDriveMigrationService.ProcessFileMigration(command, tokenInfo),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InitiateProcessFileMigration");
                return false;
            }
        }
    }

}
