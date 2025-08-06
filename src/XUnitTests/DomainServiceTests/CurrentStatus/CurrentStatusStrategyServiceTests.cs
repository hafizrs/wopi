using Xunit;
using Moq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CurrentStatus;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;

namespace XUnitTests.DomainServiceTests
{
    public class CurrentStatusStrategyServiceTests
    {
        private readonly Mock<GetCurrentStatusForCategoryData> _mockCategory;
        private readonly Mock<GetCurrentStatusForFormCreatorData> _mockFormCreator;
        private readonly Mock<GetCurrentStatusForSubCategoryData> _mockSubCategory;
        private readonly Mock<GetCurrentStatusForTrainingData> _mockTraining;
        private readonly Mock<GetCurrentStatusForSupplierData> _mockSupplier;
        private readonly Mock<GetCurrentStatusForUserAdditionalInfoTitle> _mockAdditionalTitle;
        private readonly Mock<GetCurrentStatusForPraxisRoomData> _mockRoom;
        private readonly CurrentStatusStrategyService _sut;

        public CurrentStatusStrategyServiceTests()
        {
            // Use MockBehavior.Strict to ensure no unexpected calls
            _mockCategory = new Mock<GetCurrentStatusForCategoryData>(MockBehavior.Strict, null, null);
            _mockFormCreator = new Mock<GetCurrentStatusForFormCreatorData>(MockBehavior.Strict, null, null);
            _mockSubCategory = new Mock<GetCurrentStatusForSubCategoryData>(MockBehavior.Strict, null, null);
            _mockTraining = new Mock<GetCurrentStatusForTrainingData>(MockBehavior.Strict, null, null);
            _mockSupplier = new Mock<GetCurrentStatusForSupplierData>(MockBehavior.Strict, null, null);
            _mockAdditionalTitle = new Mock<GetCurrentStatusForUserAdditionalInfoTitle>(MockBehavior.Strict, null, null);
            _mockRoom = new Mock<GetCurrentStatusForPraxisRoomData>(MockBehavior.Strict, null, null);

            _sut = new CurrentStatusStrategyService(
                _mockCategory.Object,
                _mockFormCreator.Object,
                _mockSubCategory.Object,
                _mockTraining.Object,
                _mockSupplier.Object,
                _mockAdditionalTitle.Object,
                _mockRoom.Object
            );
        }

        [Theory]
        [InlineData(nameof(PraxisClientCategory), "Category")]
        [InlineData(nameof(PraxisForm), "FormCreator")]
        [InlineData(nameof(PraxisTraining), "Training")]
        [InlineData("PraxisClientSubCategory", "SubCategory")]
        [InlineData(nameof(PraxisClient), "Supplier")]
        [InlineData(nameof(PraxisUserAdditionalInfo), "AdditionalTitle")]
        [InlineData(nameof(PraxisRoom), "Room")]
        public void GetType_Returns_Correct_Strategy(string entityName, string expected)
        {
            // Act
            var result = _sut.GetType(entityName);

            // Assert
            switch (expected)
            {
                case "Category":
                    Assert.Same(_mockCategory.Object, result);
                    break;
                case "FormCreator":
                    Assert.Same(_mockFormCreator.Object, result);
                    break;
                case "SubCategory":
                    Assert.Same(_mockSubCategory.Object, result);
                    break;
                case "Training":
                    Assert.Same(_mockTraining.Object, result);
                    break;
                case "Supplier":
                    Assert.Same(_mockSupplier.Object, result);
                    break;
                case "AdditionalTitle":
                    Assert.Same(_mockAdditionalTitle.Object, result);
                    break;
                case "Room":
                    Assert.Same(_mockRoom.Object, result);
                    break;
            }
        }

        [Fact]
        public void GetType_UnknownEntity_ReturnsNull()
        {
            // Act
            var result = _sut.GetType("UnknownEntity");

            // Assert
            Assert.Null(result);
        }
    }
} 