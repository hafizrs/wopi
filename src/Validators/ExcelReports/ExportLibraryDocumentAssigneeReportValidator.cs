using System;
using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

public class ExportLibraryDocumentAssigneeReportValidator : AbstractValidator<ExportLibraryDocumentAssigneesReportCommand>
{
    private readonly IPraxisClientService _praxisClientService;
    public ExportLibraryDocumentAssigneeReportValidator(IPraxisClientService praxisClientService)
    {
        _praxisClientService = praxisClientService;

        RuleFor(command => command.ClientId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .NotEmpty()
            .WithMessage("ClientId required.")
            .Must(BeExistingClient)
            .WithMessage("Client not exist");

        RuleFor(command => command.ReportFileId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .NotEmpty()
            .WithMessage("ReportFileId required.")
            .Must(BeAValidGuid)
            .WithMessage("ReportFileId must be a valid Guid.");

        RuleFor(command => command.FileNameWithExtension)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .NotEmpty()
            .WithMessage("FileNameWithExtension required.");

        RuleFor(command => command.ObjectArtifactId)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .NotEmpty()
            .WithMessage("ObjectArtifactId required.")
            .Must(BeAValidGuid)
            .WithMessage("ObjectArtifactId must be a valid Guid.");

        RuleFor(command => command.Purpose)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .NotEmpty()
            .WithMessage("Purpose required.");
    }
    public ValidationResult IsSatisfiedBy(ExportLibraryDocumentAssigneesReportCommand command)
    {
        var commandValidity = Validate(command);

        if (!commandValidity.IsValid) return commandValidity;

        return new ValidationResult();
    }
    private bool BeExistingClient(string clientId)
    {
        var client = _praxisClientService.GetPraxisClient(clientId);
        return client != null;
    }
    private bool BeAValidGuid(string reportFileId)
    {
        return Guid.TryParse(reportFileId, out _);
    }
}