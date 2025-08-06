using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class GenerateLibraryReportCommandValidationHandler : IValidationHandler<GenerateLibraryReportCommand, CommandResponse>
    {
        private readonly GenerateLibraryReportCommandValidator _validator;

        public GenerateLibraryReportCommandValidationHandler
        (
            GenerateLibraryReportCommandValidator validator    
        )
        {
            _validator = validator;
        }

        public CommandResponse Validate(GenerateLibraryReportCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GenerateLibraryReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
