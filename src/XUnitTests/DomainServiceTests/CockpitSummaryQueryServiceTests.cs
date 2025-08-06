using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Moq;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PCX;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CockpitModule;
using Xunit;

namespace XUnitTests.DomainServiceTests
{
    public class CockpitSummaryQueryServiceTests
    {
        private readonly Mock<ILogger<CockpitSummaryQueryService>> _loggerMock =
            new Mock<ILogger<CockpitSummaryQueryService>>();
        
        #region Cockpit Summary Response Data
        
        private static List<List<string>> DepartmentIds { get; } = new List<List<string>>(){ new List<string>{ "d1", "d2" }, new List<string>{ "d3", "d4" } };
        private static List<string> OrganizationIds { get; } = new List<string> { "o1", "o2" };

        private List<RiqsTaskCockpitSummaryDto> ExpectedResponseList { get; } =
            new List<RiqsTaskCockpitSummaryDto>
            {
                new RiqsTaskCockpitSummaryDto
                {
                    DepartmentDetails = new List<PraxisDepartmentInfo>
                    {
                        new PraxisDepartmentInfo
                        {
                            DepartmentId = DepartmentIds[0][0],
                            DepartmentName = "Department 1"
                        },
                        new PraxisDepartmentInfo
                        {
                            DepartmentId = DepartmentIds[0][1],
                            DepartmentName = "Department 2"
                        }
                    },
                    OrganizationId = OrganizationIds[0],
                    OrganizationName = "Organization 1",
                    RelatedEntityId = "1",
                    RelatedEntityName = CockpitTypeNameEnum.PraxisProcessGuide,
                    Name = "Task 1",
                    StartDate = new System.DateTime(2021, 1, 1),
                    EndDate = new System.DateTime(2021, 1, 2),
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "Key1", "Value1" },
                        { "Key2", "Value2" }
                    },
                    AssignedPraxisUserIds = new List<string>
                    {
                        "B910",
                        "DFB1"
                    },
                    SubmittedBy = new List<PraxisUserSubmissionInfo>
                    {
                        new PraxisUserSubmissionInfo
                        {
                            PraxisUserId = "48BD",
                            SubmittedOn = DateTime.UtcNow
                        },
                        new PraxisUserSubmissionInfo
                        {
                            PraxisUserId = "83D0",
                            SubmittedOn = DateTime.UtcNow
                        }
                    }
                },
                new RiqsTaskCockpitSummaryDto
                {
                    DepartmentDetails = new List<PraxisDepartmentInfo>
                    {
                        new PraxisDepartmentInfo
                        {
                            DepartmentId = DepartmentIds[1][0],
                            DepartmentName = "Department 3"
                        },
                        new PraxisDepartmentInfo
                        {
                            DepartmentId = DepartmentIds[1][1],
                            DepartmentName = "Department 4"
                        }
                    },
                    OrganizationId = OrganizationIds[1],
                    OrganizationName = "Organization 2",
                    RelatedEntityId = "2",
                    RelatedEntityName = CockpitTypeNameEnum.PraxisEquipmentMaintenance,
                    Name = "Task 2",
                    StartDate = new System.DateTime(2021, 1, 3),
                    EndDate = new System.DateTime(2021, 1, 4),
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        { "Key3", "Value3" },
                        { "Key4", "Value4" }
                    },
                    AssignedPraxisUserIds = new List<string>
                    {
                        "2DC4",
                        "B826"
                    },
                    SubmittedBy = new List<PraxisUserSubmissionInfo>
                    {
                        new PraxisUserSubmissionInfo
                        {
                            PraxisUserId = "13C0",
                            SubmittedOn = DateTime.UtcNow
                        },
                        new PraxisUserSubmissionInfo
                        {
                            PraxisUserId = "E02A",
                            SubmittedOn = DateTime.UtcNow
                        }
                    }
                }
            };

        #endregion

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnAllDocuments_WhenUserIsSystemAdmin()
        {
            // Arrange 
            var query = new GetCockpitSummaryQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResults = GetExpectedMockResults();
            var expectedListResults = expectedResults
                .Select(document => BsonSerializer.Deserialize<RiqsTaskCockpitSummaryDto>(document))
                .ToList();

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetResponseOfGetTenantDataContextProviderWithCursor(expectedResults);

            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfDepartmentLevelUser(false)
                .SetResponseOfGetAdminBUser(false);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string>{ "system admin", "admin a"}, "1");
            
            var repositoryMock = new MockRepository()
                .SetupGetItem(new PraxisUser
                {
                    ItemId = "B910",
                    FirstName = "Felix",
                    LastName = "Halim",
                    Email = "felix.halim@yopmail.com"
                });

            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expectedListResults), JsonConvert.SerializeObject(result.Results));
            Assert.Equal(0, result.StatusCode);
        }

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnDocuments_WhenUserIsOrgLevel()
        {
            // Arrange 
            var query = new GetCockpitSummaryQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResults = GetExpectedResultsOfOrgLevelUser(OrganizationIds[0]);
            var expectedListResults = expectedResults
                .Select(document => BsonSerializer.Deserialize<RiqsTaskCockpitSummaryDto>(document))
                .ToList();

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetResponseOfGetTenantDataContextProviderWithCursor(expectedResults);

            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfDepartmentLevelUser(false)
                .SetResponseOfGetAdminBUser(true)
                .SetResponseOfGetOrganizationFromOrgLevelUser(OrganizationIds[0]);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin b" },
                    "1");

            var repositoryMock = new MockRepository()
                .SetupGetItem(new PraxisUser
                {
                    ItemId = "B910",
                    FirstName = "Felix",
                    LastName = "Halim",
                    Email = "felix.halim@yopmail.com"
                });

            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expectedListResults), JsonConvert.SerializeObject(result.Results));
            Assert.Equal(0, result.StatusCode);
        }

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnDocuments_WhenUserIsDepartmentLevel()
        {
            // Arrange 
            var query = new GetCockpitSummaryQuery
            {
                PageNumber = 1,
                PageSize = 10,
                IncludingCockpitType = new List<CockpitTypeNameEnum>{CockpitTypeNameEnum.PraxisProcessGuide},
                ExcludingCockpitType = new List<CockpitTypeNameEnum> {CockpitTypeNameEnum.PraxisTask}
            };

            var expectedResults = GetExpectedResultsOfDepartmentLevelUser(DepartmentIds[0][0]);
            var expectedListResults = expectedResults
                .Select(document => BsonSerializer.Deserialize<RiqsTaskCockpitSummaryDto>(document))
                .ToList();

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetResponseOfGetTenantDataContextProviderWithCursor(expectedResults);

            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfDepartmentLevelUser(true)
                .SetResponseOfGetAdminBUser(false)
                .SetResponseOfGetDepartmentIdFromDepartmentLevelUser(DepartmentIds[0][0]);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "Role1 " }, "1");

            var repositoryMock = new MockRepository()
                .SetupGetItem(new PraxisUser
                {
                    ItemId = "B910",
                    FirstName = "Felix",
                    LastName = "Halim",
                    Email = "felix.halim@yopmail.com"
                });

            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expectedListResults), JsonConvert.SerializeObject(result.Results));
            Assert.Equal(0, result.StatusCode);
        }

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnDocuments_WhenPraxisUserIdGiven()
        {
            // Arrange 
            var query = new GetCockpitSummaryQuery
            {
                PageNumber = 1,
                PageSize = 10,
                PraxisUserId = "B910"
            };

            var expectedResults = GetMockResultsAssignedPraxisUserId("B910");
            var expectedListResults = expectedResults
                .Select(document => BsonSerializer.Deserialize<RiqsTaskCockpitSummaryDto>(document))
                .ToList();

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetResponseOfGetTenantDataContextProviderWithCursor(expectedResults);

            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfDepartmentLevelUser(false)
                .SetResponseOfGetAdminBUser(false);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "Role1 " }, "1");

            var repositoryMock = new MockRepository()
                .SetupGetItem(new PraxisUser
                {
                    ItemId = "B910",
                    FirstName = "Felix",
                    LastName = "Halim",
                    Email = ""
                });
            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);
            // Act 
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expectedListResults), JsonConvert.SerializeObject(result.Results));
            Assert.Equal(0, result.StatusCode);
        }

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnFilteredDocuments_WhenIsAllUserAutoSelectedIsFalse()
        {
            // Arrange 
            var query = new GetCockpitSummaryQuery
            {
                PageNumber = 1,
                PageSize = 10,
                PraxisUserId = "B910",
                IsAllUserAutoSelected = false
            };

            var expectedResults = GetExpectedMockResults();
            var expectedListResults = expectedResults
                .Select(document => BsonSerializer.Deserialize<RiqsTaskCockpitSummaryDto>(document))
                .ToList();

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetResponseOfGetTenantDataContextProviderWithCursor(expectedResults);

            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfDepartmentLevelUser(false)
                .SetResponseOfGetAdminBUser(false);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "Role1 " }, "1");

            var repositoryMock = new MockRepository()
                .SetupGetItem(new PraxisUser
                {
                    ItemId = "B910",
                    FirstName = "Felix",
                    LastName = "Halim",
                    Email = ""
                });

            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(JsonConvert.SerializeObject(expectedListResults), JsonConvert.SerializeObject(result.Results));
            Assert.Equal(0, result.StatusCode);
        }

        [Fact]
        public async Task GetRiqsTaskCockpitSummary_Should_ReturnEmptyWithErrorMessage_WhenQueryIsInvalid()
        { 
            // Arrange 

            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var securityHelperServiceMock = new MockSecurityHelperService();

            var securityContextProviderMock = new MockSecurityContextProvider();

            var repositoryMock = new MockRepository();

            var cockpitSummaryService = new CockpitSummaryQueryService(_loggerMock.Object, repositoryMock.Object,
                securityContextProviderMock.Object, securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await cockpitSummaryService.GetRiqsTaskCockpitSummary(null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty((System.Collections.IEnumerable)result.Results);
            Assert.Equal(1, result.StatusCode);
            Assert.Equal("Invalid query", result.ErrorMessage);
        }
        
        private List<BsonDocument> GetExpectedMockResults()
        {
            return ConvertToBsonDocuments(ExpectedResponseList);
        }

        private List<BsonDocument> GetExpectedResultsOfDepartmentLevelUser(string departmentId)
        {
            var responses = ExpectedResponseList
                .Where(response =>
                    response.DepartmentDetails.Any(dept => dept.DepartmentId == departmentId))
                .ToList();
            return ConvertToBsonDocuments(responses);
        }

        private List<BsonDocument> GetExpectedResultsOfOrgLevelUser(string orgId)
        {
            var responses = ExpectedResponseList
                .Where(response => response.OrganizationId == orgId)
                .ToList();
            return ConvertToBsonDocuments(responses);
        }

        private List<BsonDocument> GetMockResultsAssignedPraxisUserId(string praxisUserId)
        {
            var responses = ExpectedResponseList
                .Where(res => res.AssignedPraxisUserIds.Any(id => id.Equals(praxisUserId)))
                .ToList();
            return ConvertToBsonDocuments(responses);
        }

        private static List<BsonDocument> ConvertToBsonDocuments(List<RiqsTaskCockpitSummaryDto> responses)
        {
            var bsonResponses =  responses.Select(response => new BsonDocument
            {
                {
                    nameof(response.DepartmentDetails), new BsonArray(response.DepartmentDetails.Select(dept =>
                        new BsonDocument
                        {
                            { nameof(dept.DepartmentId), dept.DepartmentId },
                            { nameof(dept.DepartmentName), dept.DepartmentName }
                        }))
                },
                { nameof(response.OrganizationId), response.OrganizationId },
                { nameof(response.OrganizationName), response.OrganizationName },
                { nameof(response.RelatedEntityId), response.RelatedEntityId },
                { nameof(response.RelatedEntityName), response.RelatedEntityName.ToString() }, // Convert enum to string
                { nameof(response.Name), response.Name },
                { nameof(response.StartDate), response.StartDate },
                { nameof(response.EndDate), response.EndDate },
                {
                    nameof(response.AdditionalInfo), new BsonDocument(response.AdditionalInfo)
                }, // Convert dictionary to BsonDocument
                { nameof(response.AssignedPraxisUserIds), new BsonArray(response.AssignedPraxisUserIds) },
                {
                    nameof(response.SubmittedBy), new BsonArray(response.SubmittedBy.Select(user => new BsonDocument
                    {
                        { nameof(user.PraxisUserId), user.PraxisUserId },
                        { nameof(user.SubmittedOn), user.SubmittedOn }
                    }))
                }
            }).ToList();
            return bsonResponses;
        }
    }
}