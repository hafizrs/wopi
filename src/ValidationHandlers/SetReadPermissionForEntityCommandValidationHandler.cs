using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class SetReadPermissionForEntityCommandValidationHandler: IValidationHandler<SetReadPermissionForEntityCommand, CommandResponse>
    {
        private readonly SetReadPermissionForEntityCommandValidator _validator;

        public SetReadPermissionForEntityCommandValidationHandler(
            SetReadPermissionForEntityCommandValidator validator
            )
        {
            _validator = validator;
        }

        public CommandResponse Validate(SetReadPermissionForEntityCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(SetReadPermissionForEntityCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);
            return validationResult.IsValid ? Task.FromResult(new CommandResponse()) : Task.FromResult(new CommandResponse(validationResult));
        }
    }
}