using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Update;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports.Update
{
    public abstract class AbstractUpdateCirsReportCommandValidationHandler<TCirsReportCommand>
        : IValidationHandler<TCirsReportCommand, CommandResponse> where TCirsReportCommand : AbstractUpdateCirsReportCommand
    {
        private readonly AbstractUpdateCirsReportCommandValidator<TCirsReportCommand> _validator;

        protected AbstractUpdateCirsReportCommandValidationHandler(
            AbstractUpdateCirsReportCommandValidator<TCirsReportCommand> validator)
        {
            _validator = validator;
        }

        public CommandResponse Validate(TCirsReportCommand command)
        {
            return ValidateAsync(command).Result;
        }

        public Task<CommandResponse> ValidateAsync(TCirsReportCommand command)
        {
            var validationResult = _validator.Validate(command);
            return Task.FromResult(validationResult.IsValid
                ? new CommandResponse()
                : new CommandResponse(validationResult));
        }
    }
}