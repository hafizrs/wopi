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
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule
{
   public class PricingSeedDataService : IPricingSeedDataService
    {
        private readonly IRepository _repository;
        private readonly ILogger<PricingSeedDataService> _logger;

        public PricingSeedDataService(IRepository repository,
            ILogger<PricingSeedDataService> logger
        )
        {
            this._repository = repository;
            this._logger = logger;
        }

        public async Task<PricingSeedDataResponse> GetPricingSeedData()
        {
            try
            {
                var seedInfo = await _repository.GetItemAsync<PraxisPaymentModuleSeed>(x => x.ItemId == PraxisPriceSeed.PraxisPaymentModuleSeedId);
                var subscriptionPackageInfo = await _repository.GetItemAsync<PraxisSubscriptionPackagePrice>(x => x.SubscriptionPakage == "COMPLETE_PACKAGE");

                return new PricingSeedDataResponse()
                {
                    SubscriptionPackages = seedInfo?.SubscriptionPackages?.Where(x => x.Title == "COMPLETE_PACKAGE") ?? new List<SubscriptionPackage>(),
                    Subscriptions = GetPricingSubscriptions(),
                    TaxForCountry = seedInfo?.TaxForCountry ?? new List<TaxForCountryModel>(),
                    DefaultCurrency = seedInfo?.DefaultCurrency ?? "chf",
                    SupportSubscriptionPackage = seedInfo?.SupportSubscriptionPackage ?? new SupportSubscriptionPackage(),
                    StorageSubscriptionSeed = seedInfo?.StorageSubscriptionSeed ?? new StorageSubscriptionSeed(),
                    TokenSubscriptionSeed = seedInfo?.TokenSubscriptionSeed ?? new TokenSubscriptionSeed(),
                    PackagePrice = subscriptionPackageInfo ?? new PraxisSubscriptionPackagePrice(),
                };
            }
            catch ( Exception ex )
            {
                _logger.LogError($"Exception occured in GetPricingSeedData. Message: {ex.Message} Exception Details: {ex.StackTrace}.");
            }
            return null;
        }

        public List<PricingSubscription> GetPricingSubscriptions()
        {
            return new List<PricingSubscription>
            {
                new PricingSubscription
                {
                    ItemId = "b04bdc98-86d2-47ba-a3d0-5d6ee9e6eb43",
                    Title = "STANDARD_SUBSCRIPTION",
                    Type = new PraxisKeyValue
                    {
                        Key = "standard",
                        Value = "STANDARD"
                    }
                },
                new PricingSubscription
                {
                    ItemId = "3c0f785e-69dc-11eb-9439-0242ac130002",
                    Title = "STANDARD_SUBSCRIPTION",
                    Type = new PraxisKeyValue
                    {
                        Key = "standard",
                        Value = "STANDARD"
                    }
                }
            };
        }
    }
}
