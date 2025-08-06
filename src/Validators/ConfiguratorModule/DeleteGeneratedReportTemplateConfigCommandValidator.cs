using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ConfiguratorModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ConfiguratorModule
{
    public class DeleteGeneratedReportTemplateConfigCommandValidator : AbstractValidator<DeleteGeneratedReportTemplateConfigCommand>
    {
        public DeleteGeneratedReportTemplateConfigCommandValidator()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(command => command)
                .NotNull()
                .WithMessage("Command cannot be null.");
            RuleFor(x => x.ItemIds)
                .NotEmpty()
                .WithMessage("ItemIds cannot be empty.");
        }
        public ValidationResult IsSatisfiedBy(DeleteGeneratedReportTemplateConfigCommand command)
        {
            var validationResult = Validate(command);
            return !validationResult.IsValid ? validationResult : new ValidationResult();
        }
    }
}
