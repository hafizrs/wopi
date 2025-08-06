using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class ObjectArifactVersionService : IObjectArifactVersionService
    {
        private readonly ILogger<ObjectArifactVersionService> _logger;
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly IRiqsPediaViewControlService _riqsPediaViewControlService;


        public ObjectArifactVersionService(
            ILogger<ObjectArifactVersionService> logger,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            IRiqsPediaViewControlService riqsPediaViewControlService
        )
        {
            _logger = logger;
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _riqsPediaViewControlService = riqsPediaViewControlService;
        }

        public string GenerateParentVersionIfParentArtifactIsNullOrEmpty()
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                if (_securityHelperService.IsAAdminBUser())
                {
                    return "1.00";
                }

                var riqsViewControl = _riqsPediaViewControlService.GetRiqsPediaViewControl().GetAwaiter().GetResult();
                bool isAdminViewEnabled = riqsViewControl?.IsAdminViewEnabled ?? false;

                if (isAdminViewEnabled)
                {
                    return "1.00";
                }

                return "0.01";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in GenerateVersionIfParentArtifactIsNullOrEmpty -> message: {ex.Message} Exception Details: {ex.StackTrace}");
                return string.Empty;
            }
        }

        public bool GenerateParentVersionWithLibraryAdminIfParentArtifactIsNotEmpty()
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();

                if (_securityHelperService.IsAAdmin()) return false;

                if (_securityHelperService.IsAAdminBUser())
                {
                    return true;
                }

                var riqsViewControl = _riqsPediaViewControlService.GetRiqsPediaViewControl().GetAwaiter().GetResult();
                bool isAdminViewEnabled = riqsViewControl?.IsAdminViewEnabled ?? false;

                if (isAdminViewEnabled)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in GenerateVersionWithLibraryAdminIfParentArtifactIsNotEmpty -> message: {ex.Message} Exception Details: {ex.StackTrace}");
                return false;
            }
        }
    }
}
