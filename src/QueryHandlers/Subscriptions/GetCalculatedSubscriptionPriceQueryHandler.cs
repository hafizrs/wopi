using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Diagnostics;
using Selise.Ecap.Entities.PrimaryEntities.IDM.Basic;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Enum = System.Enum;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetCalculatedSubscriptionPriceQueryHandler : IQueryHandler<GetCalculatedSubscriptionPriceQuery, QueryHandlerResponse>
    {
        private readonly ILogger<GetCalculatedSubscriptionPriceQueryHandler> _logger;
        private readonly ISubscriptionCalculationService _subscriptionCalculationService;
        private readonly ISubscriptionInstallmentPaymentCalculationService _subscriptionInstallmentPaymentCalculationService;
        private readonly IPraxisClientSubscriptionService _praxisClientSubscriptionService;
        private readonly IRepository _repository;
        public GetCalculatedSubscriptionPriceQueryHandler(
            ILogger<GetCalculatedSubscriptionPriceQueryHandler> logger,
            ISubscriptionCalculationService subscriptionCalculationService,
            ISubscriptionInstallmentPaymentCalculationService subscriptionInstallmentPaymentCalculationService,
            IRepository repository,
            IPraxisClientSubscriptionService praxisClientSubscriptionService)
        {
            _logger = logger;
            _subscriptionCalculationService = subscriptionCalculationService;
            _subscriptionInstallmentPaymentCalculationService = subscriptionInstallmentPaymentCalculationService;
            _repository = repository;
            _praxisClientSubscriptionService = praxisClientSubscriptionService;
        }

        public QueryHandlerResponse Handle(GetCalculatedSubscriptionPriceQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetCalculatedSubscriptionPriceQuery query)
        {
            var response = new QueryHandlerResponse();
            const int MonthsInYear = 12;

            _logger.LogInformation("Enter {HandlerName} with query: {Query}",
                nameof(GetCalculatedSubscriptionPriceQueryHandler), JsonConvert.SerializeObject(query));

            try
            {
                // Extract query values for readability
                var numberOfUsers = query.NumberOfUser;
                var subscriptionTypeSeedId = query.SubscriptionTypeSeedId;
                var durationOfSubscription = query.DurationOfSubscription;
                var countryCode = query.CountryCode ?? string.Empty;

                var additionalSupportUnits = query.NumberOfSupportUnit ?? 0;
                var additionalStorage = query.TotalAdditionalStorageInGigaBites ?? 0;
                var additionalTokens = query.TotalAdditionalTokenInMillion ?? 0;
                var additionalManualTokens = query.TotalAdditionalMaulaTokenInMillion ?? 0;

                var subscriptionPrice = durationOfSubscription == 1 ? 0 : query.SubscriptionPrice ?? 0;
                var discount = query.Discount ?? 0;
                var discountPercentage = query.DiscountPercentage ?? 0;
                const string SubscriptionPackage = "COMPLETE_PACKAGE";

                // Calculate base subscription price
                var baseResults = _subscriptionCalculationService.CalculateSubscriptionPrice(numberOfUsers, subscriptionTypeSeedId, durationOfSubscription, subscriptionPrice);
                var subscriptionSeedData = _subscriptionCalculationService.GetPricingSubscriptionSeedData(subscriptionTypeSeedId);

                // Cost calculations
                double supportCost = CalculateCost(subscriptionSeedData?.SupportSubscriptionPackage?.PerUnitCost, additionalSupportUnits);
                double storageCost = CalculateCost(subscriptionSeedData?.StorageSubscriptionSeed?.PricePerGigaBiteStorage, additionalStorage);
                double tokenCost = CalculateCost(subscriptionSeedData?.TokenSubscriptionSeed?.PricePerMillionToken, additionalTokens);
                double manualTokenCost = CalculateCost(subscriptionSeedData?.TokenSubscriptionSeed?.PricePerMillionToken, additionalManualTokens);

                double baseCalculatedPrice = baseResults.FirstOrDefault(x => x.SubscriptionPackage == SubscriptionPackage)?.CalculatedPriceWithoutDuration ?? 0;
                double totalBaseCost = (baseCalculatedPrice * durationOfSubscription) + supportCost + storageCost + tokenCost + manualTokenCost;

                // Tax calculation
                var taxPercentage = subscriptionSeedData?.TaxForCountry?.Find(t => t.CountryCode == countryCode)?.CountryTax ?? 0;
                double taxAmount = CalculateTax(totalBaseCost, taxPercentage);

                // Discount
                if (discountPercentage > 0)
                {
                    discount = Math.Round((totalBaseCost * discountPercentage) / 100 , 2);
                }
                
                if (discount > totalBaseCost)
                {
                    discount = 0;
                }
                // Final cost calculations
                double totalPayable = totalBaseCost - discount + taxAmount;
                double dueAmount = 0;
                var perMonthCosts = new List<PraxisKeyValue>();

                if (subscriptionPrice > 0)
                {
                    // Calculate per-month costs and due amounts
                    double perMonthCost = totalPayable / MonthsInYear;
                    double paidAmount = perMonthCost * query.PaidDuration;
                    dueAmount = totalPayable - paidAmount;
                    totalPayable = paidAmount;

                    var periodPrices = _subscriptionCalculationService.CalculatePeriodPrices(numberOfUsers, SubscriptionPackage);

                    foreach (var (period, price) in periodPrices)
                    {
                        double periodTotal = (price * MonthsInYear) + supportCost + storageCost + tokenCost + manualTokenCost;
                        double periodTax = CalculateTax(periodTotal, taxPercentage);
                        double periodPayable = periodTotal + periodTax - discount;

                        perMonthCosts.Add(new PraxisKeyValue
                        {
                            Key = period.ToString(),
                            Value = (periodPayable / MonthsInYear).ToString("F2")
                        });
                    }
                }

                // Prepare response
                response.StatusCode = 0;
                response.Results = new
                {
                    results = baseResults,
                    SupportSubscriptionCost = supportCost,
                    AdditionalStorageCost = storageCost,
                    AdditionalTokenCost = tokenCost,
                    AdditionalManualTokenCost = manualTokenCost,
                    Total = totalBaseCost,
                    TaxAmount = taxAmount,
                    TotalPayableAmount = totalPayable,
                    DueAmount = dueAmount,
                    TotalPerMonthDueCosts = perMonthCosts,
                    query.PaidDuration,
                    Discount = discount,
                    EndOfActivePeriod = _praxisClientSubscriptionService.GetSubcriptionExpiryDateTime(
                        _praxisClientSubscriptionService.GetSubcriptionStartDateTime(DateTime.UtcNow.ToLocalTime()), query.PaidDuration)
                };
                response.TotalCount = baseResults.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}", nameof(GetCalculatedSubscriptionPriceQueryHandler), ex.Message, ex.StackTrace);
                response.StatusCode = 1;
                response.ErrorMessage = ex.Message;
            }

            _logger.LogInformation("Exit {HandlerName} with response: {Response}",
                nameof(GetCalculatedSubscriptionPriceQueryHandler), JsonConvert.SerializeObject(response));

            return await Task.FromResult(response);

            // Local functions
            double CalculateCost(double? unitCost, double quantity) => (unitCost ?? 0) * quantity;

            double CalculateTax(double amount, double taxRate) => Math.Round((amount * taxRate) / (100 + taxRate), 2);
        }
    }
}
