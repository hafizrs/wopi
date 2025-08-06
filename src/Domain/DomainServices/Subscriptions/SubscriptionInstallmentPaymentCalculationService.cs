using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.PricingModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.Entities.PrimaryEntities.SLPC;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using MongoDB.Driver;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class SubscriptionInstallmentPaymentCalculationService : ISubscriptionInstallmentPaymentCalculationService
    {
        private readonly IRepository repositoryService;
        private readonly ILogger<SubscriptionInstallmentPaymentCalculationService> logger;

        public SubscriptionInstallmentPaymentCalculationService(
            IRepository repositoryService,
            ILogger<SubscriptionInstallmentPaymentCalculationService> logger)
        {
            this.repositoryService = repositoryService;
            this.logger = logger;
        }

        public async Task<List<CalculatedInstallmentPaymentModel>> GetCalculatedSubscriptionInstallmentPayment(int durationOfSubscription, double totalAmount) 
        {
            var responseList = new List<CalculatedInstallmentPaymentModel>();

            if (totalAmount <= 0 || durationOfSubscription <= 0)
            {
                return responseList;
            }

            int[] breakpoints = durationOfSubscription switch
            {
                <= 3 => new[] { durationOfSubscription },
                <= 6 => new[] { 3, durationOfSubscription },
                _ => new[] { 3, 6, durationOfSubscription }
            };

            foreach (var duration in breakpoints)
            {
                responseList.Add(CreateInstallmentModel(duration, totalAmount, durationOfSubscription));
            }

            return await Task.FromResult(responseList);
        }

        private CalculatedInstallmentPaymentModel CreateInstallmentModel(int duration, double totalAmount, int durationOfSubscription)
        {
            return new CalculatedInstallmentPaymentModel
            {
                Duration = duration,
                Amount = Math.Round(((double)duration / durationOfSubscription) * totalAmount, 2)
            };
        }
    }
}