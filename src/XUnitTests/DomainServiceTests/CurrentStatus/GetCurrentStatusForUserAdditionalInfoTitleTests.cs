using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CurrentStatus;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace XUnitTests.DomainServiceTests.CurrentStatus
{
    public class GetCurrentStatusForUserAdditionalInfoTitleTests
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly Mock<ILogger<GetCurrentStatusForUserAdditionalInfoTitle>> _mockLogger;
        private readonly GetCurrentStatusForUserAdditionalInfoTitle _sut;

        public GetCurrentStatusForUserAdditionalInfoTitleTests()
        {
            _mockRepository = new Mock<IRepository>();
            _mockLogger = new Mock<ILogger<GetCurrentStatusForUserAdditionalInfoTitle>>();
            _sut = new GetCurrentStatusForUserAdditionalInfoTitle(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void DataCount_AdditionalInfoAlreadyUsed_ReturnsAlreadyUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "info1" };
            _mockRepository.Setup(r => r.GetItems<PraxisUser>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisUser, bool>>>() ))
                .Returns(new List<PraxisUser> { new PraxisUser { AdditionalInfo = new List<PraxisUserAdditionalInfo> { new PraxisUserAdditionalInfo { ItemId = "info1" } } } }.AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(412, result.StatusCode);
            Assert.Equal("ADDITIONAL_INFO_ALREADY_USED", result.Message);
            Assert.Contains("USER", result.DependentModules);
        }

        [Fact]
        public void DataCount_AdditionalInfoNotUsed_ReturnsNotUsed()
        {
            // Arrange
            var query = new GetCurrentStatusQuery { ItemId = "info1" };
            _mockRepository.Setup(r => r.GetItems<PraxisUser>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PraxisUser, bool>>>() ))
                .Returns(Enumerable.Empty<PraxisUser>().AsQueryable());

            // Act
            var result = _sut.DataCount(query);

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("ADDITIONAL_INFO_NOT_USE", result.Message);
            Assert.Empty(result.DependentModules);
        }
    }
} 