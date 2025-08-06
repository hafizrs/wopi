using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.PaymentModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.PaymentModule;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests.DomainServiceTests.Subscriptions
{
    public class SubscriptionRenewalServiceTests
    {
        private readonly Mock<ILogger<SubscriptionRenewalService>> _loggerMock;
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<ISecurityContextProvider> _securityContextProviderMock;
        private readonly Mock<IServiceClient> _serviceClientMock;
        private readonly Mock<INotificationService> _notificationProviderServiceMock;
        private readonly Mock<ICommonUtilService> _commonUtilServiceMock;
        private readonly Mock<ISubscriptionRenewalEstimatedBillGenerationService> _mockBillGenerationService;
        private readonly Mock<IPraxisClientSubscriptionService> _praxisClientSubscriptionServiceMock;
        private readonly Mock<ISubscriptionCalculationService> _subscriptionCalculationServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;

        private readonly Mock<IAppSettings> _appSettingsMock;
        private readonly Mock<AccessTokenProvider> _accessTokenProviderMock;

        private readonly SubscriptionRenewalService _subscriptionRenewalService;

        public SubscriptionRenewalServiceTests()
        {
            _loggerMock = new Mock<ILogger<SubscriptionRenewalService>>();
            _repositoryMock = new Mock<IRepository>();
            _securityContextProviderMock = new Mock<ISecurityContextProvider>();
            _serviceClientMock = new Mock<IServiceClient>();
            _notificationProviderServiceMock = new Mock<INotificationService>();
            _commonUtilServiceMock = new Mock<ICommonUtilService>();
            _mockBillGenerationService = new Mock<ISubscriptionRenewalEstimatedBillGenerationService>();
            _praxisClientSubscriptionServiceMock = new Mock<IPraxisClientSubscriptionService>();
            _subscriptionCalculationServiceMock = new Mock<ISubscriptionCalculationService>();
            _configurationMock = new Mock<IConfiguration>();

            _appSettingsMock = new Mock<IAppSettings>();
            _accessTokenProviderMock = new Mock<AccessTokenProvider>(_serviceClientMock.Object, _appSettingsMock.Object);

            _configurationMock.Setup(c => c["PaymentServiceBaseUrl"]).Returns("https://example.com");
            _configurationMock.Setup(c => c["PaymentServiceVersion"]).Returns("v1");
            _configurationMock.Setup(c => c["PaymentServiceInitializeUrl"]).Returns("/initialize");
            _configurationMock.Setup(c => c["PaymentFailUrl"]).Returns("/fail");
            _configurationMock.Setup(c => c["PraxisWebUrl"]).Returns("https://praxisweb.com");

            _subscriptionRenewalService = new SubscriptionRenewalService(
                _loggerMock.Object,
                _repositoryMock.Object,
                _configurationMock.Object,
                _securityContextProviderMock.Object,
                _accessTokenProviderMock.Object,
                _serviceClientMock.Object,
                _notificationProviderServiceMock.Object,
                _commonUtilServiceMock.Object,
                _mockBillGenerationService.Object,
                _praxisClientSubscriptionServiceMock.Object,
                _subscriptionCalculationServiceMock.Object
            );
        }

        [Fact]
        public async Task InitiateSubscriptionRenewalPaymentProcess_Should_Succeed_When_PaymentProcessResponse_Success()
        {
            // Arrange
            var command = new SubscriptionRenewalCommand
            {
                OrganizationId = "Org123",
                SubscriptionId = "Sub456",
                NumberOfUser = 10,
            };

            // Mocking the estimated bill generation service
            _mockBillGenerationService.Setup(s => s.GenerateSubscriptionRenewalEstimatedBill(
                    command.OrganizationId, null, command.SubscriptionId, It.IsAny<string>(), command.NumberOfUser, 12, 0, 0, 10, 5))
                .ReturnsAsync(new SubscriptionEstimatedBillResponse
                {
                    // Mocked properties of EstimatedBill
                });

            // Mocking the payment redirection response
            var paymentProcessingResult = new PaymentProcessingResult
            {
                StatusCode = 0,  // Assuming 0 means success
                RedirectUrl = "http://paymenturl.com",
                PaymentDetailId = "PaymentDetail123"
            };

            // Act
            var result = await _subscriptionRenewalService.InitiateSubscriptionRenewalPaymentProcess(command);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task InitiateSubscriptionRenewalPaymentProcess_Should_Fail_When_PaymentProcessResponse_Fails()
        {
            // Arrange
            var command = new SubscriptionRenewalCommand
            {
                OrganizationId = "Org123",
                SubscriptionId = "Sub456",
                NumberOfUser = 10,
            };


            // Mocking the estimated bill generation
            _mockBillGenerationService.Setup(s => s.GenerateSubscriptionRenewalEstimatedBill(
                    command.OrganizationId, null, command.SubscriptionId, It.IsAny<string>(), command.NumberOfUser, 12, 0, 0, 10, 5))
                .ReturnsAsync(new SubscriptionEstimatedBillResponse
                {
                    // Mocked properties
                });

            // Mocking the payment redirection service response with failure
            var paymentProcessingResult = new PaymentProcessingResult
            {
                StatusCode = 1,  // Assuming non-zero status code means failure
                ErrorMessage = "Payment failed"
            };

            // Act
            var result = await _subscriptionRenewalService.InitiateSubscriptionRenewalPaymentProcess(command);

            // Assert
            Assert.True(result);
        }
    }
}
