using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XUnitTests.DomainServiceTests
{
    public class MockEcapMongoDbDataContextProvider : Mock<IBlocksMongoDbDataContextProvider>
    {
        private readonly Mock<IMongoDatabase> _mockDataContext;
        private readonly Mock<IMongoCollection<RiqsAbsenceType>> _mockAbsenceTypeCollection;
        private readonly Mock<IMongoCollection<RiqsAbsencePlan>> _mockAbsencePlanCollection;

        public MockEcapMongoDbDataContextProvider()
        {
            // Setup default mock for GetTenantDataContext to avoid null reference exceptions
            _mockDataContext = new Mock<IMongoDatabase>();

            // Create mock collections
            _mockAbsenceTypeCollection = new Mock<IMongoCollection<RiqsAbsenceType>>();
            _mockAbsencePlanCollection = new Mock<IMongoCollection<RiqsAbsencePlan>>();

            //// Setup InsertManyAsync for RiqsAbsenceType
            //_mockAbsenceTypeCollection
            //    .Setup(c => c.InsertManyAsync(
            //        It.IsAny<IEnumerable<RiqsAbsenceType>>(),
            //        It.IsAny<InsertManyOptions>(),
            //        default))
            //    .Returns(Task.CompletedTask);

            //// Setup UpdateManyAsync for RiqsAbsenceType
            //_mockAbsenceTypeCollection
            //    .Setup(c => c.UpdateManyAsync(
            //        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
            //        It.IsAny<UpdateDefinition<RiqsAbsenceType>>(),
            //        It.IsAny<UpdateOptions>(),
            //        default))
            //    .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            //// Setup DeleteManyAsync for RiqsAbsenceType
            //_mockAbsenceTypeCollection
            //    .Setup(c => c.DeleteManyAsync(
            //        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
            //        default))
            //    .ReturnsAsync(new DeleteResult.Acknowledged(2)); // Default to 2 deleted items

            //// Setup UpdateOneAsync for RiqsAbsencePlan
            //_mockAbsencePlanCollection
            //    .Setup(c => c.UpdateOneAsync(
            //        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
            //        It.IsAny<UpdateDefinition<RiqsAbsencePlan>>(),
            //        It.IsAny<UpdateOptions>(),
            //        default))
            //    .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            //// Setup UpdateManyAsync for RiqsAbsencePlan
            //_mockAbsencePlanCollection
            //    .Setup(c => c.UpdateManyAsync(
            //        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
            //        It.IsAny<UpdateDefinition<RiqsAbsencePlan>>(),
            //        It.IsAny<UpdateOptions>(),
            //        default))
            //    .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            //// Setup CountDocumentsAsync for RiqsAbsencePlan (used in SyncAbsenceTypeUpdatesToPlansAsync)
            //_mockAbsencePlanCollection
            //    .Setup(c => c.CountDocumentsAsync(
            //        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
            //        It.IsAny<CountOptions>(),
            //        default))
            //    .ReturnsAsync(1);

            // Setup 

            // Setup collections by name to match the actual collection names used in AbsenceOverviewService
            _mockDataContext
                .Setup(d => d.GetCollection<RiqsAbsenceType>("RiqsAbsenceTypes", null))
                .Returns(_mockAbsenceTypeCollection.Object);

            _mockDataContext
                .Setup(d => d.GetCollection<RiqsAbsencePlan>("RiqsAbsencePlans", null))
                .Returns(_mockAbsencePlanCollection.Object);

            Setup(p => p.GetTenantDataContext()).Returns(_mockDataContext.Object);
        }

        public MockEcapMongoDbDataContextProvider SetResponseOfGetTenantDataContext(
            IMongoCollection<BsonDocument> collection)
        {
            _mockDataContext
                .Setup(d => d.GetCollection<BsonDocument>(It.IsAny<string>(), null))
                .Returns(collection);
            return this;
        }

        public MockEcapMongoDbDataContextProvider SetResponseOfGetTenantDataContextProviderWithCursor(
            List<BsonDocument> results)
        {
            var mockCursor = GetCursorMock(results);
            var mockCollection = new Mock<IMongoCollection<BsonDocument>>();
            mockCollection
                .Setup(c => c.Aggregate(It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), null, default))
                .Returns(mockCursor.Object);

            _mockDataContext
                .Setup(d => d.GetCollection<BsonDocument>(It.IsAny<string>(), null))
                .Returns(mockCollection.Object);

            return this;
        }

        // Method to configure DeleteManyAsync to return custom delete count
        public MockEcapMongoDbDataContextProvider SetupDeleteManyAsync<T>(long deleteCount) where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.DeleteManyAsync(
                        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
                        default))
                    .ReturnsAsync(new DeleteResult.Acknowledged(deleteCount));
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.DeleteManyAsync(
                        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
                        default))
                    .ReturnsAsync(new DeleteResult.Acknowledged(deleteCount));
            }

            return this;
        }

        public MockEcapMongoDbDataContextProvider SetupInsertOneAsync<T>() where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.InsertOneAsync(
                        It.IsAny<RiqsAbsenceType>(),
                        It.IsAny<InsertOneOptions>(),
                        default))
                    .Returns(Task.CompletedTask);
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.InsertOneAsync(
                        It.IsAny<RiqsAbsencePlan>(),
                        It.IsAny<InsertOneOptions>(),
                        default))
                    .Returns(Task.CompletedTask);
            }

            return this;
        }

        public MockEcapMongoDbDataContextProvider SetupInsertManyAsync<T>() where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.InsertManyAsync(
                        It.IsAny<IEnumerable<RiqsAbsenceType>>(),
                        It.IsAny<InsertManyOptions>(),
                        default))
                    .Returns(Task.CompletedTask);
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.InsertManyAsync(
                        It.IsAny<IEnumerable<RiqsAbsencePlan>>(),
                        It.IsAny<InsertManyOptions>(),
                        default))
                    .Returns(Task.CompletedTask);
            }

            return this;
        }

        public MockEcapMongoDbDataContextProvider SetupUpdateOneAsync<T>(long matchedCount, long modifiedCount) where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.UpdateOneAsync(
                        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
                        It.IsAny<UpdateDefinition<RiqsAbsenceType>>(),
                        It.IsAny<UpdateOptions>(),
                        default))
                    .ReturnsAsync(new UpdateResult.Acknowledged(matchedCount, modifiedCount, null));
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.UpdateOneAsync(
                        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
                        It.IsAny<UpdateDefinition<RiqsAbsencePlan>>(),
                        It.IsAny<UpdateOptions>(),
                        default))
                    .ReturnsAsync(new UpdateResult.Acknowledged(matchedCount, modifiedCount, null));
            }

            return this;
        }

        // Method to configure UpdateManyAsync to return custom update result
        public MockEcapMongoDbDataContextProvider SetupUpdateManyAsync<T>(long matchedCount, long modifiedCount) where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.UpdateManyAsync(
                        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
                        It.IsAny<UpdateDefinition<RiqsAbsenceType>>(),
                        It.IsAny<UpdateOptions>(),
                        default))
                    .ReturnsAsync(new UpdateResult.Acknowledged(matchedCount, modifiedCount, null));
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.UpdateManyAsync(
                        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
                        It.IsAny<UpdateDefinition<RiqsAbsencePlan>>(),
                        It.IsAny<UpdateOptions>(),
                        default))
                    .ReturnsAsync(new UpdateResult.Acknowledged(matchedCount, modifiedCount, null));
            }

            return this;
        }

        public MockEcapMongoDbDataContextProvider SetupCountDocumentsAsync<T>(long count) where T : class
        {
            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.CountDocumentsAsync(
                        It.IsAny<FilterDefinition<RiqsAbsenceType>>(),
                        It.IsAny<CountOptions>(),
                        default))
                    .ReturnsAsync(count);
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.CountDocumentsAsync(
                        It.IsAny<FilterDefinition<RiqsAbsencePlan>>(),
                        It.IsAny<CountOptions>(),
                        default))
                    .ReturnsAsync(count);
            }

            return this;
        }

        public MockEcapMongoDbDataContextProvider SetupFindFilter<T>(List<T> results) where T : class
        {
            var mockFindFluent = new Mock<IFindFluent<T, T>>();
            var mockCursor = new Mock<IAsyncCursor<T>>();

            // Setup cursor behavior
            mockCursor.Setup(c => c.Current).Returns(results);
            mockCursor.SetupSequence(c => c.MoveNextAsync(default))
                      .Returns(Task.FromResult(true))
                      .Returns(Task.FromResult(false));

            // Setup find fluent chain
            mockFindFluent.Setup(f => f.ToCursorAsync(default)).ReturnsAsync(mockCursor.Object);
            mockFindFluent.Setup(f => f.ToListAsync(default)).ReturnsAsync(results);

            if (typeof(T) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsenceType>>(), null))
                    .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsenceType, RiqsAbsenceType>)!);
            }
            else if (typeof(T) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsencePlan>>(), null))
                    .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsencePlan, RiqsAbsencePlan>)!);
            }

            return this;
        }

        //public MockEcapMongoDbDataContextProvider SetupProjection<TSource, TProjection>(List<TProjection> results)
        //    where TSource : class
        //    where TProjection : class
        //{
        //    var mockFindFluent = new Mock<IFindFluent<TSource, TSource>>();
        //    var mockProjectionFluent = new Mock<IFindFluent<TSource, TProjection>>();
        //    var mockCursor = new Mock<IAsyncCursor<TProjection>>();

        //    // Setup cursor behavior
        //    mockCursor.Setup(c => c.Current).Returns(results);
        //    mockCursor.SetupSequence(c => c.MoveNextAsync(default))
        //              .Returns(Task.FromResult(true))
        //              .Returns(Task.FromResult(false));

        //    // Setup projection fluent methods - Mock the interface methods, not extension methods
        //    mockProjectionFluent.Setup(f => f.ToCursorAsync(default)).ReturnsAsync(mockCursor.Object);
        //    mockProjectionFluent.Setup(f => f.ToListAsync(default)).ReturnsAsync(results);
        //    mockProjectionFluent.Setup(f => f.FirstOrDefaultAsync(default))
        //                       .ReturnsAsync(results.FirstOrDefault());

        //    // Setup the projection chain: Find -> Project -> Results
        //    mockFindFluent.Setup(f => f.Project(It.IsAny<ProjectionDefinition<TSource, TProjection>>()))
        //                 .Returns(mockProjectionFluent.Object);

        //    if (typeof(TSource) == typeof(RiqsAbsenceType))
        //    {
        //        _mockAbsenceTypeCollection
        //            .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsenceType>>(), null))
        //            .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsenceType, RiqsAbsenceType>)!);
        //    }
        //    else if (typeof(TSource) == typeof(RiqsAbsencePlan))
        //    {
        //        _mockAbsencePlanCollection
        //            .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsencePlan>>(), null))
        //            .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsencePlan, RiqsAbsencePlan>)!);
        //    }

        //    return this;
        //}

        public MockEcapMongoDbDataContextProvider SetupProjection<TSource, TProjection>(List<TProjection> results) 
            where TSource : class
            where TProjection : class
        {
            var mockFindFluent = new Mock<IFindFluent<TSource, TSource>>();
            var mockProjectionFluent = new Mock<IFindFluent<TSource, TProjection>>();
            var mockCursor = new Mock<IAsyncCursor<TProjection>>();

            // Setup cursor behavior
            mockCursor.Setup(c => c.Current).Returns(results);
            mockCursor.SetupSequence(c => c.MoveNextAsync(default))
                      .Returns(Task.FromResult(true))
                      .Returns(Task.FromResult(false));

            // Setup projection fluent methods
            mockProjectionFluent.Setup(f => f.ToCursorAsync(default)).ReturnsAsync(mockCursor.Object);
            mockProjectionFluent.Setup(f => f.ToListAsync(default)).ReturnsAsync(results);
            mockProjectionFluent.Setup(f => f.FirstOrDefaultAsync(default)).ReturnsAsync(results.FirstOrDefault());

            // Setup the projection chain: Find -> Project -> Results
            mockFindFluent.Setup(f => f.Project(It.IsAny<ProjectionDefinition<TSource, TProjection>>()))
                          .Returns(mockProjectionFluent.Object);

            if (typeof(TSource) == typeof(RiqsAbsenceType))
            {
                _mockAbsenceTypeCollection
                    .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsenceType>>(), null))
                    .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsenceType, RiqsAbsenceType>)!);
            }
            else if (typeof(TSource) == typeof(RiqsAbsencePlan))
            {
                _mockAbsencePlanCollection
                    .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsencePlan>>(), null))
                    .Returns((mockFindFluent.Object as IFindFluent<RiqsAbsencePlan, RiqsAbsencePlan>)!);
            }

            return this;
        }

        //public MockEcapMongoDbDataContextProvider SetupProjection<TSource, TProjection>(List<TProjection> results) 
        //    where TSource : class 
        //    where TProjection : class
        //{
        //    var mockFindFluent = new Mock<IFindFluent<TSource, TProjection>>();
        //    var mockCursor = new Mock<IAsyncCursor<TProjection>>();

        //    // Setup cursor behavior
        //    mockCursor.Setup(c => c.Current).Returns(results);
        //    mockCursor.SetupSequence(c => c.MoveNextAsync(default))
        //              .Returns(Task.FromResult(true))
        //              .Returns(Task.FromResult(false));

        //    // Setup find fluent chain for projection
        //    mockFindFluent.Setup(f => f.ToCursorAsync(default)).ReturnsAsync(mockCursor.Object);
        //    mockFindFluent.Setup(f => f.ToListAsync(default)).ReturnsAsync(results);

        //    // Setup projection methods
        //    var mockFindFluentSource = new Mock<IFindFluent<TSource, TSource>>();
        //    mockFindFluentSource.Setup(f => f.Project(It.IsAny<ProjectionDefinition<TSource, TProjection>>()))
        //                       .Returns(mockFindFluent.Object);

        //    if (typeof(TSource) == typeof(RiqsAbsenceType))
        //    {
        //        _mockAbsenceTypeCollection
        //            .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsenceType>>(), null))
        //            .Returns((mockFindFluentSource.Object as IFindFluent<RiqsAbsenceType, RiqsAbsenceType>)!);
        //    }
        //    else if (typeof(TSource) == typeof(RiqsAbsencePlan))
        //    {
        //        _mockAbsencePlanCollection
        //            .Setup(c => c.Find(It.IsAny<FilterDefinition<RiqsAbsencePlan>>(), null))
        //            .Returns((mockFindFluentSource.Object as IFindFluent<RiqsAbsencePlan, RiqsAbsencePlan>)!);
        //    }

        //    return this;
        //}

        private static Mock<IAsyncCursor<BsonDocument>> GetCursorMock(List<BsonDocument> results)
        {
            var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();
            mockCursor.Setup(c => c.Current).Returns(new List<BsonDocument>());
            mockCursor
                .SetupSequence(c => c.MoveNextAsync(default))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));
            mockCursor
                .SetupGet(cg => cg.Current).Returns(results);
            return mockCursor;
        }
    }
}