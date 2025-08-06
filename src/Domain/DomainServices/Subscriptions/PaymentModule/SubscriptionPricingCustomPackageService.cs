using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using System.Collections.Generic;

using Selise.Ecap.SC.PraxisMonitor.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;


namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
   public class SubscriptionPricingCustomPackageService : ISubscriptionPricingCustomPackageService
    {
        private readonly IRepository _repository;
        private readonly ILogger<SubscriptionPricingCustomPackageService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;

        public SubscriptionPricingCustomPackageService(IRepository repository,
            ILogger<SubscriptionPricingCustomPackageService> logger,
            ISecurityContextProvider securityContextProvider)
        {
            this._repository = repository;
            this._logger = logger;
            _securityContextProvider = securityContextProvider;
        }

        public async Task<List<SubscriptionPricingCustomPackageResponse>> GetSubscriptionPricingCustomPackages()
        {
            try
            {
                var existingData = _repository.GetItems<RiqsSubscriptionPricingCustomPackage>().OrderByDescending(o => o.CreateDate).ToList();

                var responseList = new List<SubscriptionPricingCustomPackageResponse>();

                foreach (var item in existingData)
                {
                    var response = new SubscriptionPricingCustomPackageResponse
                    {
                        ItemId = item.ItemId,
                        NumberOfUser = item.NumberOfUser,
                        DiscountOnPerUserAmount = item.DiscountOnPerUserAmount,
                        DiscountAmount = item.DiscountAmount,
                        DiscountPercentage = item.DiscountPercentage,
                        ValidityDate = item.ValidityDate,
                        IsSubscriptionUsed = item.IsSubscriptionUsed
                    };

                    responseList.Add(response);
                }

                return await Task.FromResult(responseList);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in GetSubscriptionPricingCustomPackages. Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return null;
        }

        public async Task<SubscriptionPricingCustomPackageResponse> GetSubscriptionPricingCustomPackage(string id) 
        {
            try
            {
                var existingData = await _repository.GetItemAsync<RiqsSubscriptionPricingCustomPackage>( item => item.ItemId == id);

                var response = new SubscriptionPricingCustomPackageResponse
                { 
                    ItemId = existingData.ItemId,
                    NumberOfUser = existingData.NumberOfUser,
                    DiscountOnPerUserAmount = existingData.DiscountOnPerUserAmount,
                    DiscountAmount = existingData.DiscountAmount,
                    DiscountPercentage = existingData.DiscountPercentage,
                    ValidityDate = existingData.ValidityDate,
                    IsSubscriptionUsed = existingData.IsSubscriptionUsed
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in GetSubscriptionPricingCustomPackage. Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }

            return null;
        }

        public async Task SaveOrUpdateSubscriptionCustomPricingPackage(SubscriptionPricingCustomPackageCommand command)
        {
            try
            {
                var existingData = await _repository.GetItemAsync<RiqsSubscriptionPricingCustomPackage>(x => x.ItemId == command.ItemId);

                if (existingData != null)
                {
                    existingData.LastUpdateDate = DateTime.UtcNow.ToLocalTime();
                    existingData.NumberOfUser = command.NumberOfUser;
                    existingData.DiscountOnPerUserAmount = command.DiscountOnPerUserAmount;
                    existingData.DiscountAmount = command.DiscountAmount;
                    existingData.DiscountPercentage = command.DiscountPercentage;
                    existingData.ValidityDate = DateTime.UtcNow.ToLocalTime().AddDays(2);
                    existingData.IsSubscriptionUsed = false;
                    await _repository.UpdateAsync<RiqsSubscriptionPricingCustomPackage>(c => c.ItemId == existingData.ItemId, existingData);
                }
                else
                {
                    var securityContext = _securityContextProvider.GetSecurityContext();
                    var data = new RiqsSubscriptionPricingCustomPackage()
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        SubscriptionId = string.Empty,
                        CreateDate = DateTime.UtcNow.ToLocalTime(),
                        LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                        CreatedBy = securityContext.UserId,
                        NumberOfUser = command.NumberOfUser,
                        DiscountOnPerUserAmount = command.DiscountOnPerUserAmount,
                        DiscountAmount = command.DiscountAmount,
                        DiscountPercentage = command.DiscountPercentage,
                        ValidityDate = DateTime.UtcNow.ToLocalTime().AddDays(2),
                        IsSubscriptionUsed = false
                    };
                    await _repository.SaveAsync(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured during incident creation. Message: {Message}. Exception Details: {StackTrace}.", ex.Message, ex.StackTrace);
            }
        }

        public async Task DeleteSubscriptionCustomPricingPackage(DeleteSubscriptionPricingCustomPackageCommand command)
        {
            try
            {
                var existingData = await _repository.GetItemAsync<RiqsSubscriptionPricingCustomPackage>(x => x.ItemId == command.ItemId);

                if (existingData != null)
                {
                    await _repository.DeleteAsync<RiqsSubscriptionPricingCustomPackage>(c => c.ItemId == existingData.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception Message: {Message} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        public bool UpdateSubscriptionUsageID(string itemId, string subscriptionId)
        {
            try
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    var existingData =  _repository.GetItem<RiqsSubscriptionPricingCustomPackage>(x => x.ItemId == itemId);
                    if (existingData != null)
                    {
                        existingData.SubscriptionId = subscriptionId;
                    }
                    _repository.Update<RiqsSubscriptionPricingCustomPackage>(cs => cs.ItemId.Equals(itemId), existingData);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task UpdateSubscriptionUsageStatus(string subscriptionId) 
        {
            try
            {
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    var existingData = await _repository.GetItemAsync<RiqsSubscriptionPricingCustomPackage>(x => x.SubscriptionId == subscriptionId);

                    if(existingData != null)
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "IsSubscriptionUsed", true }
                        };
                        await _repository.UpdateAsync<RiqsSubscriptionPricingCustomPackage>(cs => cs.ItemId.Equals(existingData.ItemId), updates);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occured in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }
    }
}
