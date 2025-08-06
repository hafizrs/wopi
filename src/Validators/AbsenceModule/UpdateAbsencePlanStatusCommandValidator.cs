using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule
{
    public class UpdateAbsencePlanStatusCommandValidator : AbstractValidator<UpdateAbsencePlanStatusCommand>
    {
        private readonly IRepository _repository;

        public UpdateAbsencePlanStatusCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.ItemId)
                .NotEmpty()
                .WithMessage("ItemId is required");

            RuleFor(x => x)
                .MustAsync(ExistsAsync)
                .WithMessage((command, context) => $"Absence plan with id '{command.ItemId}' does not exist");

            RuleFor(x => x.ReasonToDeny)
                .NotEmpty()
                .When(x => x.Status == AbsencePlanStatus.Denied)
                .WithMessage("ReasonToDeny is required when status is Denied");
        }

        private async Task<bool> ExistsAsync(UpdateAbsencePlanStatusCommand command, CancellationToken ct)
        {
            return await _repository.ExistsAsync<RiqsAbsencePlan>(ap => ap.ItemId == command.ItemId && !ap.IsMarkedToDelete);
        }

        public async Task<ValidationResult> IsSatisfiedby(UpdateAbsencePlanStatusCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }
}