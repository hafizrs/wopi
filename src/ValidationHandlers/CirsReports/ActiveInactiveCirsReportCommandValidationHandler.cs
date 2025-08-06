using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports
{
    public class ActiveInactiveCirsReportCommandValidationHandler
        : IValidationHandler<ActiveInactiveCirsReportCommand, CommandResponse>
    {
        private readonly ActiveInactiveCirsReportCommandValidator _validator;

        public ActiveInactiveCirsReportCommandValidationHandler(
            ActiveInactiveCirsReportCommandValidator validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(ActiveInactiveCirsReportCommand command)
        {
            return ValidateAsync(command).Result;
        }

        public Task<CommandResponse> ValidateAsync(ActiveInactiveCirsReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}