using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports
{
    public class DeleteCirsReportsCommandValidationHandler
        : IValidationHandler<DeleteCirsReportsCommand, CommandResponse>
    {
        private readonly DeleteCirsReportsCommandValidator _validator;

        public DeleteCirsReportsCommandValidationHandler(
            DeleteCirsReportsCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(DeleteCirsReportsCommand command)
        {
            return ValidateAsync(command).Result;
        }

        public Task<CommandResponse> ValidateAsync(DeleteCirsReportsCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}