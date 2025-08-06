using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Validators;

namespace Selise.Ecap.SC.PraxisMonitor.ValidationHandlers
{
    public class DeleteDataValidatorHandler : IValidationHandler<DeleteDataCommand, CommandResponse>
    {
        private readonly DeleteDataCommandValidator _deleteDataCommandValidator;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;
        public DeleteDataValidatorHandler(
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider,
            DeleteDataCommandValidator deleteDataCommandValidator)
        {
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
            _deleteDataCommandValidator = deleteDataCommandValidator;
        }

        public CommandResponse Validate(DeleteDataCommand command)
        {
            throw new NotImplementedException();
        }

        public async Task<CommandResponse> ValidateAsync(DeleteDataCommand command)
        {
            var validationResult = _deleteDataCommandValidator.IsSatisfiedby(command);

            if (!validationResult.IsValid)
                return new CommandResponse(validationResult);

            var response = new CommandResponse();
            if (command.EntityName != "PraxisClientSubCategory" && command.EntityName != "PraxisUserAdditionalInfo")
            {
                var dataExist = await CheckDataExist(command);
                if (!dataExist)
                    response.SetError("Exception", $"No data found to delete with ItemId: {command.ItemId} and entity: {command.EntityName}.");
            }
            return response;
        }

        private async Task<bool> CheckDataExist(DeleteDataCommand command)
        {
            var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<BsonDocument>(string.Format("{0}s", command.EntityName));
            var filter = Builders<BsonDocument>.Filter.Eq("_id", command.ItemId) & Builders<BsonDocument>.Filter.Eq("IsMarkedToDelete", false);

            return await collection.FindAsync(filter).Result.AnyAsync();
        }
    }
}
