using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Training;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;

namespace XUnitTests.DomainServiceTests.CurrentStatus
{
    public class GetCurrentStatusForFormCreatorDataTests
    {
        private readonly Mock<ILogger<GetCurrentStatusForFormCreatorData>> _mockLogger;
        private readonly Mock<IRepository> _mockRepository;
        private readonly GetCurrentStatusForFormCreatorData _sut;

        public GetCurrentStatusForFormCreatorDataTests()
        {
            _mockLogger = new Mock<ILogger<GetCurrentStatusForFormCreatorData>>();
            _mockRepository = new Mock<IRepository>();
            _sut = new GetCurrentStatusForFormCreatorData(_mockLogger.Object, _mockRepository.Object);
        }

        [Fact]
        public void DataCount_FormUsedInTraining_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "form1" };
            _mockRepository.Setup(r => r.GetItems<PraxisTraining>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTraining, bool>>>() ))
                .Returns(new List<PraxisTraining> { new PraxisTraining { ItemId = "t1", Title = "Training1" } }.AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTaskConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTaskConfig, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTaskConfig>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTask>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTask, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTask>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisProcessGuide>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisProcessGuide, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisProcessGuide>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("FORM_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_TRAINING", result.DependentModules);
            Assert.NotNull(result.Values);
        }

        [Fact]
        public void DataCount_FormUsedInTaskConfig_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "form1" };
            _mockRepository.Setup(r => r.GetItems<PraxisTraining>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTraining, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTraining>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTaskConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTaskConfig, bool>>>() ))
                .Returns(new List<PraxisTaskConfig> { new PraxisTaskConfig { ItemId = "tc1", FormIds = new List<string> { "form1" } } }.AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTask>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTask, bool>>>() ))
                .Returns(new List<PraxisTask> { new PraxisTask { ItemId = "task1" } }.AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisProcessGuide>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisProcessGuide, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisProcessGuide>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("FORM_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_TASK_CONFIG", result.DependentModules);
            Assert.NotNull(result.Values);
        }

        [Fact]
        public void DataCount_FormUsedInProcessGuide_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "form1" };
            _mockRepository.Setup(r => r.GetItems<PraxisTraining>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTraining, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTraining>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTaskConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTaskConfig, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTaskConfig>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTask>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTask, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTask>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisProcessGuide>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisProcessGuide, bool>>>() ))
                .Returns(new List<PraxisProcessGuide> { new PraxisProcessGuide { ItemId = "pg1", FormId = "form1", FormName = "Guide1" } }.AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("FORM_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_PROCESS_GUIDE", result.DependentModules);
            Assert.NotNull(result.Values);
        }

        [Fact]
        public void DataCount_FormNotUsed_ReturnsNotUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "form1" };
            _mockRepository.Setup(r => r.GetItems<PraxisTraining>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTraining, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTraining>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTaskConfig>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTaskConfig, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTaskConfig>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisTask>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisTask, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisTask>().AsQueryable());
            _mockRepository.Setup(r => r.GetItems<PraxisProcessGuide>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisProcessGuide, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisProcessGuide>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("FORM_NOT_USE", result.Message);
            Assert.Empty(result.DependentModules);
        }
    }
} 