using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.UserServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class UserCheckingStrategyService : IUserCheckingStrategy
    {
        private readonly ProcessNewUserDataService _processNewUserDataService;
        private readonly ProcessExistingUserDataService _processExistingUserDataService;
        private readonly UserUpdateService _userUpdateService;
        private readonly UserCreateService _userCreateService;

        public UserCheckingStrategyService(
            ProcessNewUserDataService processNewUserDataService,
            ProcessExistingUserDataService processExistingUserDataService,
            UserUpdateService userUpdateService,
            UserCreateService userCreateService
        )
        {
            _processNewUserDataService = processNewUserDataService;
            _processExistingUserDataService = processExistingUserDataService;
            _userUpdateService = userUpdateService;
            _userCreateService = userCreateService;
        }

        public IProcessUserInformation GetServiceType(bool isExist, string context)
        {
            return isExist switch
            {
                true when context.Equals("payment") => _processExistingUserDataService,
                true => _userUpdateService,
                false when context.Equals("payment") => _processNewUserDataService,
                false => _userCreateService
            };
        }
    }
}