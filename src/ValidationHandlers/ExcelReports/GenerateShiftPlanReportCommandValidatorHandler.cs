using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class GenerateShiftPlanReportCommandValidatorHandler : IValidationHandler<GenerateShiftPlanReportCommand, CommandResponse>
    {
        private readonly GenerateShiftPlanReportCommandValidator _validator;

        public GenerateShiftPlanReportCommandValidatorHandler
        (
            GenerateShiftPlanReportCommandValidator validator    
        )
        {
            _validator = validator;
        }

        public CommandResponse Validate(GenerateShiftPlanReportCommand command)
        {
            throw new NotImplementedException();
        }

        public Task<CommandResponse> ValidateAsync(GenerateShiftPlanReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedBy(command);

            return Task.FromResult(validationResult.IsValid ? new CommandResponse() : new CommandResponse(validationResult));
        }
    }
}
