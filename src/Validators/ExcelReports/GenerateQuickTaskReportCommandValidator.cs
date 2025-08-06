using FluentValidation;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule;
using System;
using FluentValidation.Results;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

public class GenerateQuickTaskReportCommandValidator : AbstractValidator<GenerateQuickTaskReportCommand>
{
    public GenerateQuickTaskReportCommandValidator()
    {
        RuleFor(command => command.ReportFileId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .Must(BeAValidGuid)
            .WithMessage("{PropertyName} must be a valid GUID.");

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

    public ValidationResult IsSatisfiedBy(GenerateQuickTaskReportCommand command)
    {
        var commandValidity = Validate(command);

        return !commandValidity.IsValid ? commandValidity : new ValidationResult();
    }

    private bool BeAValidGuid(string guid)
    {
        return Guid.TryParse(guid, out _);
    }
} 