using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Validators
{
    public class OpenOrganizationCommandValidator : AbstractValidator<UpdateOpenOrganizationCommand>
    {
        private readonly IRepository _repository;

        public OpenOrganizationCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(command => command.ClientId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("ClientId can't be null.")
                .NotEmpty()
                .WithMessage("ClientId can't be empty.")
                .Must(IsValidClient)
                .WithMessage("ClientId does not exist.");

            RuleFor(command => command.IsOpenOrganization)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("IsOpenOrganization can't be null.");

            RuleFor(command => command.ActionName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("ActionName can't be null.")
                .NotEmpty()
                .WithMessage("ActionName can't be empty.");

            RuleFor(command => command.Context)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .WithMessage("Context can't be null.")
                .NotEmpty()
                .WithMessage("Context can't be empty.");
        }

        private bool IsValidClient(string clientId)
        {
            var existingClient =
                _repository.GetItem<PraxisClient>(a => a.ItemId == clientId && !a.IsMarkedToDelete);
            return existingClient != null;
        }

        public ValidationResult IsSatisfiedby(UpdateOpenOrganizationCommand command)
        {
            var commandValidity = Validate(command);

            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}