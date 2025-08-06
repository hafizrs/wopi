using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Subscriptions;

public static class SubscriptionsServiceCollectionExtensions
{
    public static void AddSubscriptionsServices(this IServiceCollection services)
    {
        services.AddSingleton<IPraxisRenewSubscriptionService, PraxisRenewSubscriptionService>();
        services.AddSingleton<IDepartmentSubscriptionService, DepartmentSubscriptionService>();
        services.AddSingleton<IOrganizationSubscriptionService, OrganizationSubscriptionService>();
        services.AddSingleton<IPraxisClientSubscriptionService, PraxisClientSubscriptionService>();
        services.AddSingleton<ISubscriptionCalculationService, SubscriptionCalculationService>();
        services.AddSingleton<ISubscriptionUpdateEstimatedBillGenerationService, SubscriptionUpdateEstimatedBillGenerationService>();
        services.AddSingleton<ISubscriptionRenewalEstimatedBillGenerationService, SubscriptionRenewalEstimatedBillGenerationService>();
        services.AddSingleton<IProcessPaymentInvoiceService, ProcessPaymentInvoiceService>();
        services.AddSingleton<IPraxisClientCustomSubscriptionService, PraxisClientCustomSubscriptionService>();
        services.AddSingleton<IUpdateClientSubscriptionInformation, UpdateClientSubscriptionInformationService>();
        services.AddSingleton<IProcessClientData, ProcessClientDataService>();
        services.AddSingleton<IInvoiceGeneratorService, InvoiceGeneratorService>();
        services.AddSingleton<ISubscriptionPriceConfigService, SubscriptionPriceConfigService>();
        services.AddSingleton<IPricingSeedDataService, PricingSeedDataService>();
        services.AddSingleton<ISubscriptionPricingCustomPackageService, SubscriptionPricingCustomPackageService>();
        services.AddSingleton<ISubscriptionInstallmentPaymentCalculationService, SubscriptionInstallmentPaymentCalculationService>();
        services.AddSingleton<IUpdateClientPaymentService, UpdateClientPaymentService>();
    }
}