using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System.Threading.Tasks;

namespace XUnitTests.DomainServiceTests
{
    public class TaskSummaryServiceTests
    {
        [Theory]
        [InlineData("", "", "xyz")]
        [InlineData("12312", "abcd@gmail.com", "Created")]
        public async Task IsUpdateOrganizationAdminIds(string orgId, string userEmail, string userStatus)
        {
            //Arrange
            var repository = Mock.Of<IRepository>();
            var logger = Mock.Of<ILogger<PraxisOrganizationService>>();
            var changeLogService = Mock.Of<IChangeLogService>();

            var _sut = new PraxisOrganizationService(logger, repository, changeLogService);

            //Act
            var result = await _sut.UpdateOrganizationAdminIds(orgId, userEmail, userStatus);

            //Assert
            Assert.False(result);
        }
    }
}