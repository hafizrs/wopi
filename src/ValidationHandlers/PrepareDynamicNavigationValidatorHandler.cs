using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class PrepareDynamicNavigationValidatorHandler : IValidationHandler<PrepareDynamicNavigationCommand, CommandResponse>
    {
        private readonly PrepareDynamicNavigationCommandValidator _prepareDynamicNavigationCommandValidator;
        private readonly IPraxisClientService _praxisClientService;

        public PrepareDynamicNavigationValidatorHandler(
            PrepareDynamicNavigationCommandValidator prepareDynamicNavigationCommandValidator,
            IPraxisClientService praxisClientService)
        {
            _prepareDynamicNavigationCommandValidator = prepareDynamicNavigationCommandValidator;
            _praxisClientService = praxisClientService;
        }

        public CommandResponse Validate(PrepareDynamicNavigationCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(PrepareDynamicNavigationCommand command)
        {
            var response=new CommandResponse();
            var validationResult = _prepareDynamicNavigationCommandValidator.IsSatisfiedby(command);

            if (validationResult.IsValid.Equals(false))
                return new CommandResponse(validationResult);

            var client = await _praxisClientService.GetPraxisClient(command.OrganizationId);

            if (client == null)
                response.SetError("Exception", "Client not found by given OrganizationId");
            return response;
        }
    }
}
