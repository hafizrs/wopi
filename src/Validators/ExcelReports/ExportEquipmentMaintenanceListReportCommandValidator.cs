using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports
{
    public class ExportEquipmentMaintenanceListReportCommandValidator : AbstractValidator<ExportEquipmentMaintenanceListReportCommand>
    {
        public ExportEquipmentMaintenanceListReportCommandValidator()
        {
            RuleFor(command => command.ClientId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("ClientId required.");

            RuleFor(command => command.ReportFileId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("ReportFileId required.");

            RuleFor(command => command.FileNameWithExtension)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("FileNameWithExtension required.");

            RuleFor(command => command.FilterString)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotNull()
                .NotEmpty()
                .WithMessage("FilterString required.");
        }

        public ValidationResult IsSatisfiedby(ExportEquipmentMaintenanceListReportCommand command)
        {
            var commandValidity = Validate(command);

            if (!commandValidity.IsValid) return commandValidity;

            return new ValidationResult();
        }
    }
}
