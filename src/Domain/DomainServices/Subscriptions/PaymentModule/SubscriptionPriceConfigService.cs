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

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
   public class SubscriptionPriceConfigService: ISubscriptionPriceConfigService
    {
        private readonly IRepository _repository;
        private readonly ILogger<SubscriptionPriceConfigService> _logger;

        public SubscriptionPriceConfigService(IRepository repository,
            ILogger<SubscriptionPriceConfigService> logger
        )
        {
            this._repository = repository;
            this._logger = logger;
        }

        public async Task<SubscriptionPriceConfigResponse> GetSubscriptionPriceConfig()
        {
            try
            {
                var seedInfo = await _repository.GetItemAsync<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
                var subscriptionPackageInfo = await _repository.GetItemAsync<PraxisSubscriptionPackagePrice>(x => x.SubscriptionPakage == "COMPLETE_PACKAGE");

                return new SubscriptionPriceConfigResponse()
                {
                    PerGBStorageCost = seedInfo?.StorageSubscriptionSeed?.PricePerGigaBiteStorage ?? 0.0,
                    PerMillionTokenCost = seedInfo?.TokenSubscriptionSeed?.PricePerMillionToken ?? 0.0,
                    PerUserAnnualCost = subscriptionPackageInfo?.OriginalPrice ?? 0.0,
                    PerUserSemiAnnualCost = subscriptionPackageInfo?.SemiAnnuallyPrice ?? 0.0,
                    PerUserQuaterlyCost = subscriptionPackageInfo?.QuarterlyPrice ?? 0.0,
                    TaxForCountry = seedInfo?.TaxForCountry ?? new List<TaxForCountryModel>()
                };
            }
            catch ( Exception ex )
            {
                _logger.LogError($"Exception occured in GetSubscriptionPriceConfig. Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }
            return null;
        }

        public async Task UpdateSubscriptionPriceConfig(UpdateSubscriptionPriceConfigCommand command)
        {
            try
            {
                var seedInfo = await _repository.GetItemAsync<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
                var subscriptionPackageInfo = await _repository.GetItemAsync<PraxisSubscriptionPackagePrice>(x => x.SubscriptionPakage == "COMPLETE_PACKAGE");

                if (seedInfo != null)
                {
                    if (command.PerMillionTokenCost != null) seedInfo.TokenSubscriptionSeed.PricePerMillionToken = command.PerMillionTokenCost;
                    if (command.PerGBStorageCost != null) seedInfo.StorageSubscriptionSeed.PricePerGigaBiteStorage = command.PerGBStorageCost;
                    if (command.TaxForCountry != null)
                    {
                        seedInfo.TaxForCountry = command.TaxForCountry;
                    }

                    await _repository.UpdateAsync(s => s.ItemId == seedInfo.ItemId, seedInfo);
                }
                if (subscriptionPackageInfo != null)
                {
                    if (command.PerUserAnnualCost != null) subscriptionPackageInfo.OriginalPrice = command.PerUserAnnualCost.Value;
                    if (command.PerUserSemiAnnualCost != null) subscriptionPackageInfo.SemiAnnuallyPrice = command.PerUserSemiAnnualCost.Value;
                    if (command.PerUserQuaterlyCost != null) subscriptionPackageInfo.QuarterlyPrice = command.PerUserQuaterlyCost.Value;

                    await _repository.UpdateAsync(s => s.ItemId == subscriptionPackageInfo.ItemId, subscriptionPackageInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured in UpdateSubscriptionPriceConfig. Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }
        }
    }
}
