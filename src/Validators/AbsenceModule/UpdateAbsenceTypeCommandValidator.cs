using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.AbsenceModule
{
    public class UpdateAbsenceTypeCommandValidator : AbstractValidator<UpdateAbsenceTypeCommand>
    {
        private readonly IRepository _repository;

        public UpdateAbsenceTypeCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.AbsenceTypes)
                .NotNull()
                .WithMessage("AbsenceTypes list cannot be null")
                .NotEmpty()
                .WithMessage("AbsenceTypes list cannot be empty")
                .Must((command, context) => NotExistSameAbsenceTypeInCommand(command))
                .WithMessage("AbsenceTypes list cannot contain multiple items with the same Type value");

            RuleForEach(x => x.AbsenceTypes)
                .SetValidator(new AbsenceTypeUpdateDataValidator(_repository));
        }

        private static bool NotExistSameAbsenceTypeInCommand(UpdateAbsenceTypeCommand command)
        {
            return command.AbsenceTypes != null && 
                   command.AbsenceTypes.GroupBy(x => x.Type).All(g => g.Count() == 1);
        }

        public async Task<ValidationResult> IsSatisfiedby(UpdateAbsenceTypeCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }

    public class AbsenceTypeUpdateDataValidator : AbstractValidator<AbsenceTypeUpdateData>
    {
        private readonly IRepository _repository;

        public AbsenceTypeUpdateDataValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.ItemId)
                .NotEmpty()
                .WithMessage("ItemId is required");

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Type is required");

            RuleFor(x => x.Color)
                .NotEmpty()
                .WithMessage("Color is required");

            RuleFor(x => x)
                .MustAsync(ExistsAsync)
                .WithMessage((data, context) => $"Absence type with id '{data.ItemId}' does not exist");

            RuleFor(x => x)
                .MustAsync(NotExistWithSameTypeAsync)
                .WithMessage((data, context) => $"Absence type '{data.Type}' already exists in this department");
        }

        private async Task<bool> ExistsAsync(AbsenceTypeUpdateData data, CancellationToken ct)
        {
            return await _repository.ExistsAsync<RiqsAbsenceType>(at => at.ItemId == data.ItemId);
        }

        private async Task<bool> NotExistWithSameTypeAsync(AbsenceTypeUpdateData data, CancellationToken ct)
        {
            var existingType = await _repository.GetItemAsync<RiqsAbsenceType>(at => at.ItemId == data.ItemId);
            if (existingType == null) return true;

            var exists = await _repository.ExistsAsync<RiqsAbsenceType>(at => 
                at.Type == data.Type && 
                at.DepartmentId == existingType.DepartmentId && 
                at.ItemId != data.ItemId);
            return !exists;
        }
    }
}