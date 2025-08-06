using System;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Validators.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers.ExcelReports
{
    public class ExportTrainingDetailsReportCommandValidatorHandler : IValidationHandler<ExportTrainingDetailsReportCommand, CommandResponse>
    {
        private readonly ExportTrainingDetailsReportCommandValidator _validator;
        private readonly IPraxisClientService _praxisClientService;
        private readonly IRepository _repository;

        public ExportTrainingDetailsReportCommandValidatorHandler(
            ExportTrainingDetailsReportCommandValidator validator,
            IPraxisClientService praxisClientService,
            IRepository repository)
        {
            _validator = validator;
            _praxisClientService = praxisClientService;
            _repository = repository;
        }
        public CommandResponse Validate(ExportTrainingDetailsReportCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(ExportTrainingDetailsReportCommand command)
        {
            var validationResult = _validator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();

            bool isValidGuid = Guid.TryParse(command.ReportFileId, out _);

            if (!isValidGuid)
                response.SetError("Exception", "Guid is not valid of ReportFileId");

            bool isValidClientId = Guid.TryParse(command.ClientId, out _);

            if (!isValidClientId)
            {
                response.SetError("Exception", "Guid is not valid of ClientId");
            }
            else
            {
                var client = await _praxisClientService.GetPraxisClient(command.ClientId);
                if (client == null)
                    response.SetError("Exception", "Client not found for given ClientId");
            }

            bool isValidTrainingId = Guid.TryParse(command.TrainingId, out _);

            if (!isValidTrainingId)
            {
                response.SetError("Exception", "Guid is not valid of TrainingId");
            }
            else
            {
                var training = await _repository.ExistsAsync<PraxisTraining>(t => t.ItemId == command.TrainingId && !t.IsMarkedToDelete);
                if (!training)
                    response.SetError("Exception", "Client not found for given TrainingId");
            }
            return response;
        }
    }
}
