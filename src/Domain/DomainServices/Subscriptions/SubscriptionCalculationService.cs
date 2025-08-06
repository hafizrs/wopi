using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enum = System.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class SubscriptionCalculationService : ISubscriptionCalculationService
    {
        private readonly IRepository repositoryService;
        private readonly ILogger<SubscriptionCalculationService> _logger;
        private readonly ISubscriptionInstallmentPaymentCalculationService _subscriptionInstallmentPaymentCalculationService;

        public SubscriptionCalculationService(
            IRepository repositoryService,
            ILogger<SubscriptionCalculationService> logger,
            ISubscriptionInstallmentPaymentCalculationService subscriptionInstallmentPaymentCalculationService)
        {
            this.repositoryService = repositoryService;
            _logger = logger;
            _subscriptionInstallmentPaymentCalculationService = subscriptionInstallmentPaymentCalculationService;
        }

        public PraxisPaymentModuleSeed GetPricingSubscriptionSeedData(string subscriptionTypeSeedId)
        {
            return repositoryService.GetItem<PraxisPaymentModuleSeed>(x => x.ItemId == subscriptionTypeSeedId);
        }

        public async Task<PraxisPaymentModuleSeed> GetSubscriptionSeedData(string subscriptionTypeSeedId)
        {
            return await repositoryService.GetItemAsync<PraxisPaymentModuleSeed>(x => x.ItemId == subscriptionTypeSeedId);
        }

        public List<CalculatedPriceModel> CalculateSubscriptionPrice(int numberOfUser, string subscriptionTypeSeedId, int durationOfSubscription, double ? SubscriptionPrice = 0)
        {
            List<CalculatedPriceModel> calculatedPriceList = new List<CalculatedPriceModel>();
            try
            {
                PraxisPaymentModuleSeed pricingSubscriptionSeed = GetPricingSubscriptionSeedData(subscriptionTypeSeedId);
                foreach (var subscriptionPackage in pricingSubscriptionSeed.SubscriptionPackages)
                {
                    calculatedPriceList.Add(GetCalculationPrice(numberOfUser, durationOfSubscription, subscriptionPackage.Title, SubscriptionPrice));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error in Subscription calculation service");
                return new List<CalculatedPriceModel>();
            }
            return calculatedPriceList;
        }

        public List<CalculatedPriceModel> CalculateSubscriptionUpdatePrice(int numberOfUser, string subscriptionTypeSeedId, int durationOfSubscription, string clientId)
        {
            List<CalculatedPriceModel> calculatedPriceList = new List<CalculatedPriceModel>();
            try
            {
                PraxisPaymentModuleSeed pricingSubscriptionSeed = GetPricingSubscriptionSeedData(subscriptionTypeSeedId);
                foreach (var title in pricingSubscriptionSeed.SubscriptionPackages.Select(s => s.Title))
                {
                    if (title.Equals("COMPLETE_PACKAGE"))
                    {
                        calculatedPriceList.Add(GetUpdateCalculationPrice(numberOfUser, durationOfSubscription, title, clientId, calculatedPriceList[0].PerUserMonthlyPrice));
                    }
                    else
                    {
                        calculatedPriceList.Add(GetUpdateCalculationPrice(numberOfUser, durationOfSubscription, title, clientId));
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error in Subscription update calculation service");
                return new List<CalculatedPriceModel>();
            }
            return calculatedPriceList;
        }

        public SubscriptionEstimatedBillResponse CalculateOtherPropertiesOfBillCosts(PraxisPaymentModuleSeed praxisPaymentModuleSeed, SubscriptionEstimatedBillResponse billResponse, string subscriptionId, string organizationId, int durationOfSubscription)
        {
            double taxAmount = 0.0;
            double packageCost = 0.0;
            double taxRate = 0.0;

            var completePackageBill = billResponse.PackageCosts.FirstOrDefault(c => c.SubscriptionPackage == "COMPLETE_PACKAGE");
            if (completePackageBill != null)
            {
                packageCost = completePackageBill.CalculatedPrice;
            }

            if (praxisPaymentModuleSeed.TaxForCountry != null)
            {
                var subscriptionData = repositoryService.GetItem<PraxisClientSubscription>(s => s.ItemId == subscriptionId);
                if (subscriptionData != null)
                {
                    var taxForCountry = praxisPaymentModuleSeed.TaxForCountry.Find(t => t.CountryCode.Equals(subscriptionData.Location));
                    if (taxForCountry != null)
                    {
                        taxRate = taxForCountry.CountryTax;
                    }
                }
            }
            var totalCost = !string.IsNullOrEmpty(organizationId) ? packageCost + billResponse.SupportSubscriptionCost + billResponse.AdditionalStorageCost + billResponse.AdditionalTokenCost + billResponse.AdditionalManualTokenCost : billResponse.AdditionalStorageCost + billResponse.AdditionalTokenCost + billResponse.AdditionalManualTokenCost;
            taxAmount = Math.Round(((totalCost * taxRate) / 100), 2);

            billResponse.TaxAmount = taxAmount;
            billResponse.GrandTotal = totalCost + taxAmount;

            return billResponse;
        }

        public PraxisClientSubscription GetCurrentSubscriptionData(string organizationId, string subscriptionId)
        {
            var subscriptionData = repositoryService.GetItem<PraxisClientSubscription>(s => s.ItemId == subscriptionId && s.OrganizationId == organizationId && s.IsLatest);
            return subscriptionData;
        }

        private CalculatedPriceModel GetCalculationPrice(int numberOfUser, int subscriptionDuration, string subscriptionPackage, double? SubscriptionPrice = 0)
        {
            SubscriptionPackagePriceDto subscriptionPriceData = GetSubscriptionPriceModel(numberOfUser, subscriptionPackage, SubscriptionPrice);
            var calculatedPrice = CalculatePrice(numberOfUser, subscriptionPriceData);
            return new CalculatedPriceModel
            {
                SubscriptionPackage = subscriptionPackage,
                CalculatedPrice = calculatedPrice * subscriptionDuration,
                CalculatedPriceWithoutDuration = calculatedPrice,
                PerUserMonthlyPrice = numberOfUser > 0 ? Math.Round((calculatedPrice / numberOfUser), 3) : 0,
                TotalUserMonthlyPrice = Math.Round((calculatedPrice), 3),
                TotalUserNumber = numberOfUser,
                DurationOfSubscription = subscriptionDuration
            };
        }

        public List<(SubscriptionPeriod period, double price)> CalculatePeriodPrices(int numberOfUser, string subscriptionPackage)
        {
            var prices = new List<(SubscriptionPeriod period, double price)>();
            var subscriptionPriceData = GetSubscriptionPriceModel(numberOfUser, subscriptionPackage, 0);

            foreach (SubscriptionPeriod period in Enum.GetValues(typeof(SubscriptionPeriod)))
            {
                double basePrice = period switch
                {
                    SubscriptionPeriod.Annual => subscriptionPriceData.SubscriptionPackageInfo.OriginalPrice,
                    SubscriptionPeriod.SemiAnnual => subscriptionPriceData.SubscriptionPackageInfo.SemiAnnuallyPrice,
                    SubscriptionPeriod.Quarterly => subscriptionPriceData.SubscriptionPackageInfo.QuarterlyPrice,
                    _ => 0
                };

                var subscriptionPriceModels = subscriptionPriceData.SubscriptionPrices;
                double subscriptionPrice = 0;
                var previousRangePrice = basePrice;

                for (int index = 0; index < subscriptionPriceModels.Count; index++)
                {
                    var pricemodel = subscriptionPriceModels[index];
                    var userCount = 0;
                    var discountedPrice = previousRangePrice - (previousRangePrice * (pricemodel.DiscountOnOriginalPrice / 100));

                    if (index == subscriptionPriceModels.Count - 1)
                    {
                        userCount = numberOfUser - pricemodel.SubscriptionUserBreakingPoint + 1;
                        subscriptionPrice += (userCount * discountedPrice);
                    }
                    else
                    {
                        userCount = subscriptionPriceModels[index + 1].SubscriptionUserBreakingPoint - pricemodel.SubscriptionUserBreakingPoint;
                        subscriptionPrice += (userCount * discountedPrice);
                    }

                    previousRangePrice = discountedPrice;
                }
              
                prices.Add((period, subscriptionPrice));
            }

            return prices;
        }

        private SubscriptionPackagePriceDto GetSubscriptionPriceModel(int numberOfUser, string subscriptionPackage, double? subscriptionPrice = 0)
        {
            var subscriptionPackageInfo = repositoryService.GetItem<PraxisSubscriptionPackagePrice>(x => x.SubscriptionPakage == subscriptionPackage);
            var result = subscriptionPackageInfo.SubscriptionPackagePrices.Where(x => x.SubscriptionUserBreakingPoint <= numberOfUser).ToList();
            return new SubscriptionPackagePriceDto
            {
                SubscriptionPrices = result,
                SubscriptionPrice = subscriptionPrice.GetValueOrDefault() > 0 ? subscriptionPrice.GetValueOrDefault() : subscriptionPackageInfo.OriginalPrice,
                SubscriptionPackageInfo = subscriptionPackageInfo
            };
        }

        private CalculatedPriceModel GetUpdateCalculationPrice(int numberOfUser, int subscriptionDuration, string subscriptionPackage, string clientId, double previousPackageValue = -1)
        {
            var subscriptionList = repositoryService.GetItems<PraxisClientSubscription>(x => x.ClientId == clientId).ToList();
            var isCustomSubscriptionExist = subscriptionList.Any(s => s.PaymentMethod == "Cash");
            var latestSubscriptionData = repositoryService.GetItem<PraxisClientSubscription>(x => x.ClientId == clientId && x.IsLatest);
            var totalNumberOfUser = numberOfUser + latestSubscriptionData.NumberOfUser;
            SubscriptionPackagePriceDto subscriptionPriceData = GetSubscriptionPriceModel(totalNumberOfUser, subscriptionPackage);
            var expiredDays = (DateTime.UtcNow - latestSubscriptionData.SubscriptionDate).TotalDays;
            var expiredMonths = (int)Math.Round(expiredDays / 30);
            var remainingSubscriptionDuration = subscriptionDuration - expiredMonths;
            if (!latestSubscriptionData.SubscriptionPackage.Equals("COMPLETE_PACKAGE") && subscriptionPackage.Equals("COMPLETE_PACKAGE"))
            {
                subscriptionDuration = remainingSubscriptionDuration;
            }
            else
            {
                previousPackageValue = -1;
            }

            if (isCustomSubscriptionExist)
            {
                var previousSubscriptionPriceData = GetSubscriptionPriceModel(latestSubscriptionData.NumberOfUser, subscriptionPackage);
                var previousCalculatedPrice = CalculatePrice(latestSubscriptionData.NumberOfUser, previousSubscriptionPriceData);
                var currentCalculatedPrice = CalculatePrice(totalNumberOfUser, subscriptionPriceData);
                var subscriptionPrice = (currentCalculatedPrice * remainingSubscriptionDuration) - (previousCalculatedPrice * subscriptionDuration);
                return new CalculatedPriceModel
                {
                    SubscriptionPackage = subscriptionPackage,
                    CalculatedPrice = previousPackageValue != -1 ? (subscriptionPrice + (previousPackageValue * latestSubscriptionData.NumberOfUser * expiredMonths)) : subscriptionPrice,
                    PerUserMonthlyPrice = subscriptionPrice != 0 ? (numberOfUser > 0 ? Math.Round((subscriptionPrice / numberOfUser), 3) : 0) : subscriptionPrice,
                    TotalUserMonthlyPrice = subscriptionPrice != 0 ? Math.Round((subscriptionPrice), 3) : subscriptionPrice,
                    TotalUserNumber = numberOfUser
                };
            }
            var calculatedPrice = CalculatePrice(totalNumberOfUser, subscriptionPriceData);
            var updatedCalculatedPrice = GetUpdatedCalculatedPrice(totalNumberOfUser > 0 ? calculatedPrice / totalNumberOfUser : 0, subscriptionDuration, remainingSubscriptionDuration, numberOfUser, latestSubscriptionData.NumberOfUser);
            return new CalculatedPriceModel
            {
                SubscriptionPackage = subscriptionPackage,
                CalculatedPrice = previousPackageValue != -1 ? (updatedCalculatedPrice + (previousPackageValue * latestSubscriptionData.NumberOfUser * expiredMonths)) : updatedCalculatedPrice,
                PerUserMonthlyPrice = calculatedPrice != 0 ? Math.Round((totalNumberOfUser > 0 ? calculatedPrice / totalNumberOfUser : 0), 3) : calculatedPrice,
                TotalUserMonthlyPrice = calculatedPrice != 0 ? Math.Round((calculatedPrice), 3) : calculatedPrice,
                TotalUserNumber = numberOfUser
            };
        }

        private double GetUpdatedCalculatedPrice(double perUserSubscriptionDuration, int subscriptionDuration, int remainingSubscriptionDuration,
            int numberOfUser, int existingUser)
        {
            if (perUserSubscriptionDuration == 0)
                return perUserSubscriptionDuration;
            var price = (perUserSubscriptionDuration * numberOfUser * remainingSubscriptionDuration) +
                        (perUserSubscriptionDuration * existingUser * subscriptionDuration);
            return price;
        }

        private double CalculatePrice(int numberOfUser, SubscriptionPackagePriceDto subscriptionPriceData)
        {
            var subscriptionPriceModels = subscriptionPriceData.SubscriptionPrices;
            var index = 0;
            double subscriptionPrice = 0;
            var previousRangePrice = subscriptionPriceData.SubscriptionPrice;
            foreach (var pricemodel in subscriptionPriceModels)
            {
                var userCount = 0;
                var discountedPrice = previousRangePrice - (previousRangePrice * (pricemodel.DiscountOnOriginalPrice / 100));
                if (index == subscriptionPriceModels.Count - 1)
                {
                    userCount = numberOfUser - pricemodel.SubscriptionUserBreakingPoint + 1;
                    subscriptionPrice += (userCount * discountedPrice);
                }
                else
                {
                    userCount = subscriptionPriceModels[index + 1].SubscriptionUserBreakingPoint - pricemodel.SubscriptionUserBreakingPoint;
                    subscriptionPrice += (userCount * discountedPrice);
                }
                previousRangePrice = discountedPrice;
                index++;
            }
            return subscriptionPrice;
        }

        public List<CalculatedPriceModel> GetCompletePackageSubscriptionUpdatePrice(
            string organizationId,
            string subscriptionTypeSeedId,
            int numberOfUser,
            int durationOfSubscription)
        {
            List<CalculatedPriceModel> calculatedPriceList = new List<CalculatedPriceModel>();
            try
            {
                var currentSubscriptionData = GetLatestPraxisClientSubscription(organizationId);
                PraxisPaymentModuleSeed pricingSubscriptionSeed = GetPricingSubscriptionSeedData(subscriptionTypeSeedId);
                var completeSubscriptionPackage = pricingSubscriptionSeed.SubscriptionPackages.FirstOrDefault(sp => sp.Title == "COMPLETE_PACKAGE");
                if (completeSubscriptionPackage != null)
                {
                    calculatedPriceList.Add(GetPackageWisePriceModel(currentSubscriptionData, numberOfUser, durationOfSubscription, "COMPLETE_PACKAGE"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ServiceName}. Exception Message: {ErrorMessage}. Exception details: {StackTrace}.",
                    nameof(SubscriptionCalculationService), ex.Message, ex.StackTrace);
            }
            return calculatedPriceList;
        }

        public List<CalculatedPriceModel> GetCompletePackageSubscriptionUpdatePriceForClient(
          string clientId,
          string subscriptionTypeSeedId,
          int numberOfUser,
          int durationOfSubscription)
        {
            List<CalculatedPriceModel> calculatedPriceList = new List<CalculatedPriceModel>();
            try
            {
                var currentSubscriptionData = GetLatestPraxisClientSubscriptionForClient(clientId);
                PraxisPaymentModuleSeed pricingSubscriptionSeed = GetPricingSubscriptionSeedData(subscriptionTypeSeedId);
                var completeSubscriptionPackage = pricingSubscriptionSeed.SubscriptionPackages.FirstOrDefault(sp => sp.Title == "COMPLETE_PACKAGE");
                if (completeSubscriptionPackage != null)
                {
                    calculatedPriceList.Add(GetPackageWisePriceModel(currentSubscriptionData, numberOfUser, durationOfSubscription, "COMPLETE_PACKAGE"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error occured in {nameof(SubscriptionCalculationService)}." +
                    $"Exception Message: {ex.Message}." +
                    $"Exception detaiils: {ex.StackTrace}.");
            }
            return calculatedPriceList;
        }

        private PraxisClientSubscription GetLatestPraxisClientSubscription(string organizationId)
        {
            return repositoryService.GetItem<PraxisClientSubscription>(pcs => pcs.OrganizationId == organizationId && pcs.IsActive && pcs.IsLatest);
        }

        private PraxisClientSubscription GetLatestPraxisClientSubscriptionForClient(string clientId)
        {
            return repositoryService.GetItem<PraxisClientSubscription>(pcs => pcs.ClientId == clientId && pcs.IsActive && pcs.IsLatest);
        }

        private PraxisSubscriptionPackagePrice GetPraxisSubscriptionPackagePrice(string subscriptionPackage)
        {
            return repositoryService.GetItem<PraxisSubscriptionPackagePrice>(x => x.SubscriptionPakage == subscriptionPackage);
        }

        private CalculatedPriceModel GetPackageWisePriceModel(
            PraxisClientSubscription currentSubscriptionData,
            int userAddedCount,
            int subscriptionDuration,
            string subscriptionPackage)
        {
            var subscriptionPackageData = GetPraxisSubscriptionPackagePrice(subscriptionPackage);
            var remainingMonthCount = GetRemaingMonthCount(currentSubscriptionData, subscriptionDuration);
            var calculatedPrice = CalculateAdditionalUserPrice(currentSubscriptionData.NumberOfUser, userAddedCount, subscriptionPackageData);
            return new CalculatedPriceModel
            {
                SubscriptionPackage = subscriptionPackage,
                CalculatedPrice = calculatedPrice * remainingMonthCount,
                PerUserMonthlyPrice = userAddedCount > 0 ? Math.Round((calculatedPrice / userAddedCount), 3) : 0,
                TotalUserMonthlyPrice = Math.Round((calculatedPrice), 3),
                TotalUserNumber = userAddedCount,
                DurationOfSubscription = remainingMonthCount
            };
        }

        private int GetRemaingMonthCount(PraxisClientSubscription currentSubscriptionData, int subscriptionDuration)
        {
            var expiredDays = (DateTime.UtcNow - currentSubscriptionData.SubscriptionDate).TotalDays;
            var expiredMonths = (int)Math.Round(expiredDays / 30);
            var remainingSubscriptionDuration = Math.Max(subscriptionDuration - expiredMonths, 0);

            return remainingSubscriptionDuration;
        }

        private double CalculateAdditionalUserPrice(
            int currentUserCount,
            int userAddedCount,
            PraxisSubscriptionPackagePrice subscriptionPackageData)
        {
            double subscriptionPrice = 0;
            var subscriptionPackagePrices = subscriptionPackageData.SubscriptionPackagePrices;
            var startIndex = GetCurrentSubscriptionUserBreakingPointIndex(currentUserCount, subscriptionPackagePrices);
            if (startIndex > -1)
            {
                var accumulatedUserCount = currentUserCount;
                var remainingUser = userAddedCount;

                for (int i = startIndex; i < subscriptionPackagePrices.Count - 1; i++)
                {
                    if (remainingUser <= 0)
                    {
                        break;
                    }

                    var userBreakingPointUpperLimit =
                        i == subscriptionPackagePrices.Count - 2 ?
                        subscriptionPackagePrices[i + 1].SubscriptionUserBreakingPoint :
                        subscriptionPackagePrices[i + 1].SubscriptionUserBreakingPoint - 1;

                    var remainingCapacity = userBreakingPointUpperLimit - accumulatedUserCount;
                    var takenUser = Math.Min(remainingCapacity, remainingUser);

                    remainingUser -= takenUser;
                    accumulatedUserCount += takenUser;

                    var price = subscriptionPackageData.OriginalPrice - (subscriptionPackageData.OriginalPrice * (subscriptionPackagePrices[i].DiscountOnOriginalPrice / 100));
                    subscriptionPrice += takenUser * price;
                }
            }

            return subscriptionPrice;
        }

        private int GetCurrentSubscriptionUserBreakingPointIndex(int numberOfUser, List<SubscriptionPackagePriceModel> subscriptionPackagePrices)
        {
            for (int i = 0; i < subscriptionPackagePrices.Count; i++)
            {
                if (i == 0)
                {
                    if (numberOfUser == subscriptionPackagePrices[i].SubscriptionUserBreakingPoint)
                    {
                        return i;
                    }
                }
                else if (i < subscriptionPackagePrices.Count - 1)
                {
                    if (numberOfUser < subscriptionPackagePrices[i].SubscriptionUserBreakingPoint)
                    {
                        return i - 1;
                    }
                }
                else
                {
                    if (numberOfUser <= subscriptionPackagePrices[i].SubscriptionUserBreakingPoint)
                    {
                        return i - 1;
                    }
                }
            }
            return -1;
        }

        public string GetSubscriptionPaymentMethod(int duration)
        {
            return duration switch
            {
                3  => "Quarterly",
                6  => "Semi-annually",
                12 => "Annually",
                _  => string.Empty
            };
        }
    }
}