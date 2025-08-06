using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportEquipmentMaintenanceListReportValidationHandler 
        : IValidationHandler<ExportEquipmentMaintenanceListReportCommand, CommandResponse> 
    {
        private readonly ExportEquipmentMaintenanceListReportCommandValidator _validator;
        private readonly IPraxisClientService _praxisClientService;

        public ExportEquipmentMaintenanceListReportValidationHandler(
            ExportEquipmentMaintenanceListReportCommandValidator validator,
            IPraxisClientService praxisClientService)
        {
            _validator = validator;
            _praxisClientService = praxisClientService;
        }

        public CommandResponse Validate(ExportEquipmentMaintenanceListReportCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(ExportEquipmentMaintenanceListReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            PraxisClient client = await _praxisClientService.GetPraxisClient(command.ClientId);

            if (client == null)
                response.SetError("Exception", "Client not found for given ClientId");

            return response;
        }
    }
}
