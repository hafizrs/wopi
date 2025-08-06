using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Validators;

public class DeleteCockpitObjectArtifactSummaryCommandValidator : AbstractValidator<DeleteCockpitObjectArtifactSummaryCommand>
{
    private readonly IRepository _repository;
    private readonly ISecurityHelperService _securityHelperService;
    public DeleteCockpitObjectArtifactSummaryCommandValidator(
        IRepository repository,
        ISecurityHelperService securityHelperService)
    {
        _repository = repository;
        _securityHelperService = securityHelperService;
        RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .WithMessage("Command can't be null");
        RuleFor(command => command)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .Must(SystemAdmin)
            .WithMessage("You can't delete this entity data. Only System Admin can delete");
        RuleFor(command => command.ObjectArtifactSummaryIds)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotNull()
            .WithMessage("ObjectArtifactSummaryId can't be null")
            .NotEmpty()
            .WithMessage("ObjectArtifactSummaryId can't be empty");
    }

    private bool SystemAdmin(DeleteCockpitObjectArtifactSummaryCommand command)
    {
        return _securityHelperService.IsAAdminOrTaskConrtroller();
    }
    public ValidationResult IsSatisfiedby(DeleteCockpitObjectArtifactSummaryCommand command)
    {
        var commandValidity = Validate(command);

        if (!commandValidity.IsValid) return commandValidity;

        return new ValidationResult();
    }
}