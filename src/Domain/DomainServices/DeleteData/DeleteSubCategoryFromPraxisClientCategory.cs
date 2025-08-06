using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DeleteData;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData
{
    public class DeleteSubCategoryFromPraxisClientCategory : IDeleteDataByCollectionSpecific
    {
        private readonly ISecurityContextProvider _securityContextProviderService;
        private readonly ILogger<DeleteSubCategoryFromPraxisClientCategory> _logger;
        private readonly IBlocksMongoDbDataContextProvider _mongoDbDataContextProvider;

        public DeleteSubCategoryFromPraxisClientCategory(
            ISecurityContextProvider securityContextProviderService,
            ILogger<DeleteSubCategoryFromPraxisClientCategory> logger,
            IBlocksMongoDbDataContextProvider mongoDbDataContextProvider)
        {
            _securityContextProviderService = securityContextProviderService;
            _logger = logger;
            _mongoDbDataContextProvider = mongoDbDataContextProvider;
        }
        public async Task<bool> DeleteData(string entityName, string itemId, string additionalInfosItemId = null, string additionalTitleId = null)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            _logger.LogInformation($"Going to delete {nameof(PraxisClientCategory)} data for subcategory with ItemId: {itemId} and tenantId: {securityContext.TenantId}.");

            try
            {
                var collection = _mongoDbDataContextProvider.GetTenantDataContext().GetCollection<PraxisClientCategory>("PraxisClientCategorys");
                var filter = Builders<PraxisClientCategory>.Filter.Eq("SubCategories.ItemId", itemId) & Builders<PraxisClientCategory>.Filter.Eq("IsMarkedToDelete", false);

                var categoryData = collection.Find(filter).FirstOrDefault();
                if (categoryData != null)
                {
                    var subCategoryData = categoryData.SubCategories.Where(s => s.ItemId != itemId).ToList();
                    categoryData.SubCategories = subCategoryData;
                    UpdateClientCategoryData(categoryData);
                }
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during delete {nameof(PraxisClientCategory)} entity SubCategory data with ItemId: {itemId} and tenantId: {securityContext.TenantId} for Admin role. Exception Message: {ex.Message}. Exception detaiils: {ex.StackTrace}.");
                return false;
            }
        }

        private void UpdateClientCategoryData(PraxisClientCategory clientCategory)
        {
            var securityContext = _securityContextProviderService.GetSecurityContext();
            var db = _mongoDbDataContextProvider.GetTenantDataContext(securityContext.TenantId.Trim());

            var filter = Builders<PraxisClientCategory>.Filter.Eq("_id", clientCategory.ItemId);
            var update = Builders<PraxisClientCategory>.Update
                .Set(nameof(PraxisClientCategory.LastUpdateDate), DateTime.UtcNow.ToLocalTime())
                .Set(nameof(PraxisClientCategory.SubCategories), clientCategory.SubCategories);

            db.GetCollection<PraxisClientCategory>($"PraxisClientCategorys").UpdateOne(filter, update);
            _logger.LogInformation($"Data has been successfully updated for {nameof(PraxisClientCategory)} entity with ItemId: {clientCategory.ItemId}.");
        }
    }
}
