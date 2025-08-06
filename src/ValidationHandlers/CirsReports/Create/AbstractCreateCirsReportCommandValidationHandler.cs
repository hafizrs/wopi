using System;
using System.Threading.Tasks;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Validators.CirsReports.Create;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.CirsReports.Create
{
    public abstract class AbstractCreateCirsReportCommandValidationHandler<TCirsReportCommand>
        : IValidationHandler<TCirsReportCommand, CommandResponse> where TCirsReportCommand : AbstractCreateCirsReportCommand
    {
        private readonly AbstractCreateCirsReportCommandValidator<TCirsReportCommand> _validator;

        protected AbstractCreateCirsReportCommandValidationHandler(
            AbstractCreateCirsReportCommandValidator<TCirsReportCommand> validator)
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