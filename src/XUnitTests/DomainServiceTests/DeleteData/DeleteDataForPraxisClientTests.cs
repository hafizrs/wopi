using Microsoft.Extensions.Logging;
using Moq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.DeleteData;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.QuickTaskModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests.DomainServiceTests.DeleteData
{
    public class DeleteDataForPraxisClientTests
    {
        private readonly Mock<ISecurityContextProvider> _mockSecurityContextProvider;
        private readonly Mock<IRepository> _mockRepository;
        private readonly Mock<ILogger<DeleteDataForPraxisClient>> _mockLogger;
        private readonly Mock<IPraxisClientService> _mockPraxisClientService;
        private readonly DeleteDataForPraxisClient _deleteDataService;

        public DeleteDataForPraxisClientTests()
        {
            _mockSecurityContextProvider = new Mock<ISecurityContextProvider>();
            _mockRepository = new Mock<IRepository>();
            _mockLogger = new Mock<ILogger<DeleteDataForPraxisClient>>();
            _mockPraxisClientService = new Mock<IPraxisClientService>();

            var mockIEquipmentService = new Mock<IPraxisEquipmentService>();
            var mockDeleteDataService = new Mock<IDeleteDataForClientInCollections>();

            var mockEquipmentService = new Mock<PraxisEquipmentService>(mockIEquipmentService.Object, mockDeleteDataService.Object);
            var mockFormService = new Mock<PraxisFormService>();
            var mockOpenItemService = new Mock<PraxisOpenItemService>();
            var mockRiskService = new Mock<PraxisRiskService>();
            var mockRoomService = new Mock<PraxisRoomService>();
            var mockTaskService = new Mock<PraxisTaskService>();
            var mockTrainingAnswerService = new Mock<PraxisTrainingAnswerService>();
            var mockTrainingService = new Mock<PraxisTrainingService>();
            var mockProcessGuideService = new Mock<PraxisProcessGuideService>();
            var mockPraxisUserService = new Mock<PraxisUserService>();
            var mockCategoryService = new Mock<PraxisClientCategoryService>();
            var mockDmsService = new Mock<IDmsService>();
            var mockUserCountMaintainService = new Mock<IUserCountMaintainService>();
            var mockPraxisShiftService = new Mock<IPraxisShiftService>();
            var mockDeleteCirsReportsService = new Mock<IDeleteCirsReportsService>();
            var mockDeleteDmsArtifactUsageReferenceService = new Mock<IDeleteDmsArtifactUsageReferenceService>();
            var mockCockpitSummaryCommandService = new Mock<ICockpitSummaryCommandService>();
            var mockQuickTaskService = new Mock<IQuickTaskService>();

            _deleteDataService = new DeleteDataForPraxisClient(
                _mockSecurityContextProvider.Object,
                _mockRepository.Object,
                _mockLogger.Object,
                mockEquipmentService.Object,
                mockFormService.Object,
                mockOpenItemService.Object,
                mockRiskService.Object,
                mockRoomService.Object,
                mockTaskService.Object,
                mockTrainingAnswerService.Object,
                mockTrainingService.Object,
                mockProcessGuideService.Object,
                mockPraxisUserService.Object,
                mockCategoryService.Object,
                mockDmsService.Object,
                _mockPraxisClientService.Object,
                mockUserCountMaintainService.Object,
                mockPraxisShiftService.Object,
                mockDeleteCirsReportsService.Object,
                mockDeleteDmsArtifactUsageReferenceService.Object,
                mockCockpitSummaryCommandService.Object,
                mockQuickTaskService.Object
            );
        }

        [Fact]
        public async Task DeleteData_ShouldDeletePraxisUserAdditionalInfo_WhenEntityNameIsPraxisUserAdditionalInfo()
        {
            // Arrange
            string entityName = nameof(PraxisUserAdditionalInfo);
            string itemId = "123";
            string additionalInfosItemId = "456";

            _mockPraxisClientService
                .Setup(service => service.DeletePraxisUserAdditionalInfo(additionalInfosItemId, itemId))
                .Returns(true);

            // Act
            var result = await _deleteDataService.DeleteData(entityName, itemId, additionalInfosItemId);

            // Assert
            Assert.True(result);
            _mockPraxisClientService.Verify(service => service.DeletePraxisUserAdditionalInfo(additionalInfosItemId, itemId), Times.Once);
        }

        [Fact]
        public async Task DeleteData_ShouldDeleteClientData_WhenAdditionalInfosItemIdIsNull()
        {
            // Arrange
            string entityName = "SomeEntity";
            string itemId = "123";

            _mockRepository
                .Setup(repo => repo.GetItemAsync<PraxisClient>(It.IsAny<Expression<Func<PraxisClient, bool>>>()))
                .ReturnsAsync(new PraxisClient { ItemId = itemId, ParentOrganizationId = "ParentOrg123" });


            // Act
            var result = await _deleteDataService.DeleteData(entityName, itemId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(repo => repo.GetItemAsync<PraxisClient>(It.IsAny<Expression<Func<PraxisClient, bool>>>()), Times.Once);
            _mockLogger.Verify(log => log.LogInformation(It.Is<string>(msg => msg.Contains(itemId))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteData_ShouldReturnFalse_WhenExceptionOccursInDeleteDataForSupplier()
        {
            // Arrange
            string entityName = "SomeEntity";
            string itemId = "123";
            string additionalInfosItemId = "456";

            _mockRepository
                .Setup(repo => repo.GetItem<PraxisClient>(It.IsAny<Expression<Func<PraxisClient, bool>>>()))
                .Throws(new Exception("Test Exception"));


            // Act
            var result = await _deleteDataService.DeleteData(entityName, itemId, additionalInfosItemId);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(log => log.LogError(It.IsAny<string>()), Times.AtLeastOnce);
        }
    }
}
