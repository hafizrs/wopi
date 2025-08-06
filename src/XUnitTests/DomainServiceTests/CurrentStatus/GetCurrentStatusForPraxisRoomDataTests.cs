using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;

namespace XUnitTests.DomainServiceTests.CurrentStatus
{
    public class GetCurrentStatusForPraxisRoomDataTests
    {
        private readonly Mock<ILogger<GetCurrentStatusForPraxisRoomData>> _mockLogger;
        private readonly Mock<IRepository> _mockRepository;
        private readonly GetCurrentStatusForPraxisRoomData _sut;

        public GetCurrentStatusForPraxisRoomDataTests()
        {
            _mockLogger = new Mock<ILogger<GetCurrentStatusForPraxisRoomData>>();
            _mockRepository = new Mock<IRepository>();
            _sut = new GetCurrentStatusForPraxisRoomData(_mockLogger.Object, _mockRepository.Object);
        }

        [Fact]
        public void DataCount_RoomAlreadyUsed_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "room1" };
            _mockRepository.Setup(r => r.GetItems<PraxisEquipment>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisEquipment, bool>>>() ))
                .Returns(new List<PraxisEquipment> { new PraxisEquipment { RoomId = "room1" } }.AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("ROOM_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_EQUIPMENT", result.DependentModules);
        }

        [Fact]
        public void DataCount_RoomNotUsed_ReturnsNotUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "room1" };
            _mockRepository.Setup(r => r.GetItems<PraxisEquipment>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisEquipment, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisEquipment>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("ROOM_NOT_USE", result.Message);
            Assert.Empty(result.DependentModules);
        }
    }
} 