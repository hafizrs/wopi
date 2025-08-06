using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class ProcessDraftedObjectArtifactDocumentCommandValidator : AbstractValidator<ProcessDraftedObjectArtifactDocumentCommand>
    {
        private readonly IDocumentEditMappingService _documentEditMappingService;
        public ProcessDraftedObjectArtifactDocumentCommandValidator(IDocumentEditMappingService documentEditMappingService)
        {
            _documentEditMappingService = documentEditMappingService; 

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("Command can't be null.");

            RuleFor(command => command.ObjectArtifactId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command.HtmlFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");

            RuleFor(command => command)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(IsValidObjectArtifactHtmlId)
                .WithMessage("Invalid HtmlFileId.");

            RuleFor(command => command.WorkspaceId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .Must(BeAValidGuid)
                .WithMessage("{PropertyName} must be a valid GUID.");
        }

        public ValidationResult IsSatisfiedBy(ProcessDraftedObjectArtifactDocumentCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }

        public bool BeAValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }

        public bool IsValidObjectArtifactHtmlId(ProcessDraftedObjectArtifactDocumentCommand command)
        {
            var documentMappingData = _documentEditMappingService.GetDocumentEditMappingRecordByDraftArtifact(command.ObjectArtifactId).Result;
            return documentMappingData != null && documentMappingData.CurrentHtmlFileId == command.HtmlFileId;
        } 
    }
}
