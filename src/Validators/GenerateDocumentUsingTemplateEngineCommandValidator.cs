using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class GenerateDocumentUsingTemplateEngineCommandValidator: AbstractValidator<GenerateDocumentUsingTemplateEngineCommand>
    {
        public GenerateDocumentUsingTemplateEngineCommandValidator()
        {
            RuleFor(command => command.Payload)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("Payload can't be null.")
                .NotEmpty()
                .WithMessage("Payload can't be empty.");

            RuleFor(command => command.FileNameWithExtension)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("FileName(WithExtension) can't be null.")
                .NotEmpty()
                .WithMessage("FileName(WithExtension) can't be empty.");
            
            RuleFor(command => command.ModuleName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("ModuleName can't be null.")
                .NotEmpty()
                .WithMessage("ModuleName can't be empty.");
            
            RuleFor(command => command.ReportFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("ReportFileId can't be null.")
                .NotEmpty()
                .WithMessage("ReportFileId can't be empty.");
            RuleFor(command => command.EntityName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("EntityName can't be null")
                .NotEmpty()
                .WithMessage("EntityName can't be empty");
        }

        public ValidationResult IsSatisfiedBy(GenerateDocumentUsingTemplateEngineCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}