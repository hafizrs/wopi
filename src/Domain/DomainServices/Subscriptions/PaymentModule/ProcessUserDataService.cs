using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
    public class ProcessUserDataService : IProcessUserData
    {
        private readonly ILogger<ProcessUserDataService> _logger;
        private readonly IRepository _repository;
        private readonly IUserCheckingStrategy _userCheckingStrategyService;

        public ProcessUserDataService(
            ILogger<ProcessUserDataService> logger,
            IRepository repository,
            IUserCheckingStrategy userCheckingStrategyService
        )
        {
            _logger = logger;
            _repository = repository;
            _userCheckingStrategyService = userCheckingStrategyService;
        }

        public async Task<bool> ProcessData(PersonInfo userInformation, PraxisClient praxisClient, string designation)
        {
            var email = userInformation.PersonalInformation?.Email;
            _logger.LogInformation("Going to check if user data exists with email: {Email}.", email);
            try
            {
                var isExist = await _repository.ExistsAsync<User>(u => u.Email.Equals(email));

                var userDataProcessService = _userCheckingStrategyService.GetServiceType(isExist, "payment");
                return await userDataProcessService.ProcessData(userInformation, praxisClient, designation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during checking whether user data exists with email: {Email}. Exception Message: {Message}. Exception Details: {StackTrace}.", email, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<bool> ProcessUserCreateUpdateData(
            PraxisUser praxisUserInformation,
            FileInformation fileInformation
        )
        {
            var email = praxisUserInformation?.Email;
            _logger.LogInformation("Going to check user data exists or not with email: {Email}.", email);

            try
            {
                var isExist = await _repository.ExistsAsync<User>(u => u.Email.Equals(email));

                var userDataProcessService = _userCheckingStrategyService.GetServiceType(isExist, "portal");
                return await userDataProcessService.ProcessData(praxisUserInformation, fileInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during checking whether user data exists with email: {Email}. Exception Message: {Message}. Exception Details: {StackTrace}.", email, ex.Message, ex.StackTrace);

                return false;
            }
        }
    }
}