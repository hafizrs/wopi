using Microsoft.Extensions.Logging;
using Moq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Linq.Expressions;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace XUnitTests.DomainServiceTests
{
    public class DeleteDmsArtifactUsageReferenceServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<ILogger<DeleteDmsArtifactUsageReferenceService>> _loggerMock;
        private readonly DeleteDmsArtifactUsageReferenceService _service;

        public DeleteDmsArtifactUsageReferenceServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _loggerMock = new Mock<ILogger<DeleteDmsArtifactUsageReferenceService>>();
            _service = new DeleteDmsArtifactUsageReferenceService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task DeleteDataForClient_RemovesArtifact_WhenSingleClient()
        {
            // Arrange
            string clientId = "3684ad9b-562c-4e7c-862e-24e460670c8d";
            var artifactUsageReference = new DmsArtifactUsageReference
            {
                ClientInfos = new List<FormSpecificClientInfo> 
                { 
                    new FormSpecificClientInfo { ClientId = clientId }
                    //new FormSpecificClientInfo { ClientId = "55d772bd-cd19-494c-8dd2-dedfa82cc7e0" }
                },
                ItemId = Guid.NewGuid().ToString(),
                ObjectArtifactId = "55d772bd-cd19-494c-8dd2-dedfa82cc7e0",
            };

            var objectArtifact = new ObjectArtifact
            {
                MetaData = new Dictionary<string, MetaValuePair>
                {
                    { "ArtifactUsageReferenceCounter", new MetaValuePair { Type = "string", Value = "2" } },
                    { "IsUsedInAnotherEntity", new MetaValuePair { Type = "string", Value = "1" } }
                },
                ItemId = "55d772bd-cd19-494c-8dd2-dedfa82cc7e0"
            };

            _repositoryMock
                .Setup(repo => repo.GetItemAsync<DmsArtifactUsageReference>(It.IsAny<Expression<Func<DmsArtifactUsageReference, bool>>>()))
                .ReturnsAsync(artifactUsageReference);


            _repositoryMock
                .Setup(repo => repo.DeleteAsync<DmsArtifactUsageReference>(It.IsAny<Expression<Func<DmsArtifactUsageReference, bool>>>()))
                .Returns(Task.CompletedTask);

            _repositoryMock
                .Setup(repo => repo.GetItemAsync<ObjectArtifact>(It.IsAny<Expression<Func<ObjectArtifact, bool>>>()))
                .ReturnsAsync(objectArtifact);


            _repositoryMock
                .Setup(repo => repo.DeleteAsync<ObjectArtifact>(It.IsAny<Expression<Func<ObjectArtifact, bool>>>()))
                .Returns(Task.CompletedTask);


            // Act
            await _service.DeleteDataForClient(clientId);

            // Assert
            _repositoryMock.Verify(repo => repo.DeleteAsync<DmsArtifactUsageReference>(It.IsAny<Expression<Func<DmsArtifactUsageReference, bool>>>()), Times.Once);
            //_repositoryMock.Verify(repo => repo.UpdateAsync<DmsArtifactUsageReference>(It.IsAny<Expression<Func<DmsArtifactUsageReference, bool>>>(), It.IsAny<DmsArtifactUsageReference>()), Times.Never);
        }
    }
}
