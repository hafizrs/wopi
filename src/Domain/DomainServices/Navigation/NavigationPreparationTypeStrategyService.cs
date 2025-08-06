using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Navigation;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Navigation
{
    public class NavigationPreparationTypeStrategyService : INavigationPreparationTypeStrategy
    {
        private readonly InsertDynamicNavigationService _insertDynamicNavigationService;
        private readonly UpdateDynamicNavigationService _updateDynamicNavigationService;

        public NavigationPreparationTypeStrategyService(
            InsertDynamicNavigationService insertDynamicNavigationService,
            UpdateDynamicNavigationService updateDynamicNavigationService)
        {
            _insertDynamicNavigationService = insertDynamicNavigationService;
            _updateDynamicNavigationService = updateDynamicNavigationService;
        }

        public IDynamicNavigationPreparation GetServiceType(string type)
        {
            return type.ToUpper() switch
            {
                nameof(NavigationPrecessType.INSERT) => _insertDynamicNavigationService,
                nameof(NavigationPrecessType.UPDATE) => _updateDynamicNavigationService,
                _ => null,
            };
        }
    }
}
