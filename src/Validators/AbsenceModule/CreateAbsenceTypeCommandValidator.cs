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
    public class CreateAbsenceTypeCommandValidator : AbstractValidator<CreateAbsenceTypeCommand>
    {
        private readonly IRepository _repository;

        public CreateAbsenceTypeCommandValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.AbsenceTypes)
                .NotNull()
                .WithMessage("AbsenceTypes list cannot be null")
                .NotEmpty()
                .WithMessage("AbsenceTypes list cannot be empty");

            RuleForEach(x => x.AbsenceTypes)
                .SetValidator(new AbsenceTypeDataValidator(_repository));
        }

        public async Task<ValidationResult> IsSatisfiedby(CreateAbsenceTypeCommand command)
        {
            var commandValidity = await ValidateAsync(command);
            return !commandValidity.IsValid ? commandValidity : new ValidationResult();
        }
    }

    public class AbsenceTypeDataValidator : AbstractValidator<AbsenceTypeData>
    {
        private readonly IRepository _repository;

        public AbsenceTypeDataValidator(IRepository repository)
        {
            _repository = repository;

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Type is required");

            RuleFor(x => x.Color)
                .NotEmpty()
                .WithMessage("Color is required");

            RuleFor(x => x.DepartmentId)
                .NotEmpty()
                .WithMessage("DepartmentId is required");

            RuleFor(x => x)
                .MustAsync(NotExistAsync)
                .WithMessage((data, context) => $"Absence type '{data.Type}' already exists in department '{data.DepartmentId}'");
        }

        private async Task<bool> NotExistAsync(AbsenceTypeData data, CancellationToken ct)
        {
            var exists = await _repository.ExistsAsync<RiqsAbsenceType>(at => at.Type == data.Type && at.DepartmentId == data.DepartmentId);
            return !exists;
        }
    }
}