using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class UpdateOrgTypeChangePermissionCommandValidatorHandler : IValidationHandler<UpdateOrgTypeChangePermissionCommand, CommandResponse>
    {
        private readonly UpdateOrgTypeChangePermissionCommandValidator _validationRules;

        public UpdateOrgTypeChangePermissionCommandValidatorHandler(
            UpdateOrgTypeChangePermissionCommandValidator validationRules)
        {
            _validationRules = validationRules;
        }
        public CommandResponse Validate(UpdateOrgTypeChangePermissionCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(UpdateOrgTypeChangePermissionCommand command)
        {
            var validationResult = _validationRules.IsSatisfiedby(command);
            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
