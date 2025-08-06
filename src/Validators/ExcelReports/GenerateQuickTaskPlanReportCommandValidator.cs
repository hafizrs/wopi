using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class GenerateQuickTaskPlanReportCommandValidator : AbstractValidator<GenerateQuickTaskPlanReportCommand>
    {
        public GenerateQuickTaskPlanReportCommandValidator()
        {
            RuleFor(command => command.ReportFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.StartDate)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("{PropertyName} must not be empty.");

            RuleFor(command => command.FileNameWithExtension)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("{PropertyName} must not be empty.");

            RuleFor(command => command.FileName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("{PropertyName} must not be empty.");

            RuleFor(command => command.ClientId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .WithMessage("{PropertyName} must be a valid GUID.");
        }

        public ValidationResult IsSatisfiedBy(GenerateQuickTaskPlanReportCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public static bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }
    }
} 