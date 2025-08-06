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
    public class GetCurrentStatusForSupplierDataTests
    {
        private readonly Mock<ILogger<GetCurrentStatusForSupplierData>> _mockLogger;
        private readonly Mock<IRepository> _mockRepository;
        private readonly GetCurrentStatusForSupplierData _sut;

        public GetCurrentStatusForSupplierDataTests()
        {
            _mockLogger = new Mock<ILogger<GetCurrentStatusForSupplierData>>();
            _mockRepository = new Mock<IRepository>();
            _sut = new GetCurrentStatusForSupplierData(_mockLogger.Object, _mockRepository.Object);
        }

        [Fact]
        public void DataCount_SupplierAlreadyUsed_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "supplier1" };
            _mockRepository.Setup(r => r.GetItems<PraxisEquipment>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisEquipment, bool>>>() ))
                .Returns(new List<PraxisEquipment> { new PraxisEquipment { SupplierId = "supplier1" } }.AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("SUPPLIER_ALREADY_USED", result.Message);
            Assert.Contains("PRAXIS_EQUIPMENT", result.DependentModules);
        }

        [Fact]
        public void DataCount_SupplierNotUsed_ReturnsNotUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "supplier1" };
            _mockRepository.Setup(r => r.GetItems<PraxisEquipment>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisEquipment, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisEquipment>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("SUPPLIER_NOT_USE", result.Message);
            Assert.Empty(result.DependentModules);
        }
    }
} 