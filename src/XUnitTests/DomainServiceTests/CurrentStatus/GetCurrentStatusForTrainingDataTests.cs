using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;

namespace XUnitTests.DomainServiceTests.CurrentStatus
{
    public class GetCurrentStatusForTrainingDataTests
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly Mock<ILogger<GetCurrentStatusForTrainingData>> _mockLogger;
        private readonly GetCurrentStatusForTrainingData _sut;

        public GetCurrentStatusForTrainingDataTests()
        {
            _mockRepository = new Mock<IRepository>();
            _mockLogger = new Mock<ILogger<GetCurrentStatusForTrainingData>>();
            _sut = new GetCurrentStatusForTrainingData(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void DataCount_TrainingAlreadyUsed_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "training1" };
            _mockRepository.Setup(r => r.GetItems<PraxisOpenItemConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisOpenItemConfig, bool>>>() ))
                .Returns(new List<PraxisOpenItemConfig> { new PraxisOpenItemConfig { TaskReferenceId = "training1" } }.AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("TRAINING_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_TRAINING", result.DependentModules);
        }

        [Fact]
        public void DataCount_TrainingNotUsed_ReturnsNotUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "training1" };
            _mockRepository.Setup(r => r.GetItems<PraxisOpenItemConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisOpenItemConfig, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisOpenItemConfig>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("TRAINING_NOT_USE", result.Message);
            Assert.Empty(result.DependentModules);
        }
    }
} 