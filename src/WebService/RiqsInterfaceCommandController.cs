using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.DmsMigration;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.WebService
{
    public class RiqsInterfaceCommandController : ControllerBase
    {
        private readonly CommandHandler _commandService;
        private readonly ILogger<RiqsInterfaceCommandController> _logger;
        private readonly IServiceClient _serviceClient;

        public RiqsInterfaceCommandController(
            CommandHandler commandService,
            ILogger<RiqsInterfaceCommandController> logger,
            IServiceClient serviceClient
        )
        {
            _commandService = commandService;
            _logger = logger;
            _serviceClient = serviceClient;
        }

        [HttpPost]
        [Authorize]
        public CommandResponse ProcessInterfaceMigration([FromBody] ProcessInterfaceMigrationCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }

            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisDmsConversionQueueName(), command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> InterfaceMigrationFolderAndFile([FromBody] InterfaceMigrationFolderAndFileCommand command)
        {
            if (command == null)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<InterfaceMigrationFolderAndFileCommand, CommandResponse>(command);
        }


        [HttpPost]
        [Authorize]
        public CommandResponse UplaodEquipemtInterfaceData([FromBody] UplaodEquipemtInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.FileId) || string.IsNullOrEmpty(command.MigrationSummaryId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and FileId are required.");
                return response;
            }
            return _serviceClient.SendToQueue<CommandResponse>(PraxisConstants.GetPraxisDmsConversionQueueName(), command);
        }

        [HttpPost]
        [AnonymousEndPoint]
        public async Task<RiqsCommandResponse> DownloadEquipemtInterfaceData([FromBody] DownloadEquipemtInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ClientId))
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<DownloadEquipemtInterfaceDataCommand, RiqsCommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateEquipemtInterfaceAdditioanalData([FromBody] UpdateEquipemtInterfaceAdditioanalDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.MigrationSummaryId) || command.EquipmentIds == null ||  command.EquipmentIds.Count== 0)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and EquipmentIds are required.");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateEquipemtInterfaceAdditioanalDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> CreateEquimentFromRiqsInterfaceMigration([FromBody] CreateEquimentFromRiqsInterfaceMigrationCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.MigrationSummaryId) || command.EquipmentIds == null || command.EquipmentIds.Count == 0)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and EquipmentIds are required.");
                return response;
            }
            return await _commandService.SubmitAsync<CreateEquimentFromRiqsInterfaceMigrationCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpsertRiqsInterfaceConfiguration([FromBody] UpsertRiqsInterfaceConfigurationCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.Provider))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<UpsertRiqsInterfaceConfigurationCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> LogOutInterfaceManager([FromBody] LogoutInterfaceManagerCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.Provider))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<LogoutInterfaceManagerCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UplaodUserInterfaceData([FromBody] UplaodUserInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.FileId) || string.IsNullOrEmpty(command.MigrationSummaryId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and FileId are required.");
                return response;
            }
            return await _commandService.SubmitAsync<UplaodUserInterfaceDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateUserInterfaceAdditioanalData([FromBody] UpdateUserInterfaceAdditioanalDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.MigrationSummaryId) || command.UserIds == null || command.UserIds.Count == 0)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and UserIds are required.");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateUserInterfaceAdditioanalDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> DownloadUserInterfaceData([FromBody] DownloadUserInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ClientId))
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<DownloadUserInterfaceDataCommand, RiqsCommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UplaodSupplierInterfaceData([FromBody] UplaodSupplierInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.FileId) || string.IsNullOrEmpty(command.MigrationSummaryId))
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and FileId are required.");
                return response;
            }
            return await _commandService.SubmitAsync<UplaodSupplierInterfaceDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<CommandResponse> UpdateSupplierInterfaceAdditioanalData([FromBody] UpdateSupplierInterfaceAdditioanalDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.MigrationSummaryId) || command.SupplierIds == null || command.SupplierIds.Count == 0)
            {
                var response = new CommandResponse();
                response.SetError("Command", "Invalid value. MigrationSummaryId and UserIds are required.");
                return response;
            }
            return await _commandService.SubmitAsync<UpdateSupplierInterfaceAdditioanalDataCommand, CommandResponse>(command);
        }

        [HttpPost]
        [Authorize]
        public async Task<RiqsCommandResponse> DownloadSupplierInterfaceData([FromBody] DownloadSupplierInterfaceDataCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ClientId))
            {
                var response = new RiqsCommandResponse();
                response.SetError("Command", "Invalid value");
                return response;
            }
            return await _commandService.SubmitAsync<DownloadSupplierInterfaceDataCommand, RiqsCommandResponse>(command);
        }
    }
}

