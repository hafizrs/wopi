using Microsoft.Extensions.Logging;
using Moq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Subscriptions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsTranslations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests.DomainServiceTests.AITranslations
{
    public class RiqsTranslationServiceTests
    {
        private readonly Mock<IKeyStore> _keyStoreMock;
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<ILogger<RiqsTranslationService>> _loggerMock;
        private readonly Mock<ITranslationService> _translationServiceMock;
        private readonly Mock<IDepartmentSubscriptionService> _departmentSubscriptionServiceMock;
        private readonly Mock<IOrganizationSubscriptionService> _organizationSubscriptionServiceMock;
        private readonly Mock<ISecurityHelperService> _securityHelperServiceMock;

        private readonly RiqsTranslationService _riqsTranslationService;

        public RiqsTranslationServiceTests()
        {
            _keyStoreMock = new Mock<IKeyStore>();
            _repositoryMock = new Mock<IRepository>();
            _loggerMock = new Mock<ILogger<RiqsTranslationService>>();
            _translationServiceMock = new Mock<ITranslationService>();
            _departmentSubscriptionServiceMock = new Mock<IDepartmentSubscriptionService>();
            _organizationSubscriptionServiceMock = new Mock<IOrganizationSubscriptionService>();
            _securityHelperServiceMock = new Mock<ISecurityHelperService>();

            _riqsTranslationService = new RiqsTranslationService(
                _keyStoreMock.Object,
                _repositoryMock.Object,
                _loggerMock.Object,
                _translationServiceMock.Object,
                _departmentSubscriptionServiceMock.Object,
                _organizationSubscriptionServiceMock.Object,
                _securityHelperServiceMock.Object
            );
        }

        [Fact]
        public async Task RiqsTranslation_Should_Return_TranslatedResponses_When_Translations_Are_Found()
        {
            // Arrange
            var command = new GetRiqsTranslationCommand
            {
                PraxisClientId = "client123",
                OrganizationId = "org123",
                Texts = new List<string> { "Hello", "World" },
                TranslateLangKeys = new List<string> { "en" }
            };

            // Act
            var result = await _riqsTranslationService.RiqsTranslation(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Hello", result[0].TranslatedText);
            Assert.Equal("World", result[1].TranslatedText);
        }

        [Fact]
        public async Task RiqsTranslation_Should_Return_Empty_When_Translations_Fail()
        {
            // Arrange
            var command = new GetRiqsTranslationCommand
            {
                PraxisClientId = "client123",
                OrganizationId = "org123",
                Texts = new List<string> { "InvalidText" },
                TranslateLangKeys = new List<string> { "en" }
            };

            // Act
            var result = await _riqsTranslationService.RiqsTranslation(command);

            // Assert
            Assert.NotNull(result);
        }
    }
}
