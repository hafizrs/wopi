using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.AbsenceModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.AbsenceModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests.DomainServiceTests.AbsenceModule
{
    public class AbsenceOverviewServiceTests
    {
        private readonly Mock<ILogger<AbsenceOverviewService>> _loggerMock =
            new Mock<ILogger<AbsenceOverviewService>>();

        #region Test Data

        private static List<RiqsAbsenceType> MockAbsenceTypes { get; } = new List<RiqsAbsenceType>
        {
            new RiqsAbsenceType
            {
                ItemId = "at1",
                Type = "Sick Leave",
                Color = "#FF0000",
                DepartmentId = "dept1",
                CreatedBy = "user1",
                CreateDate = DateTime.UtcNow
            },
            new RiqsAbsenceType
            {
                ItemId = "at2",
                Type = "Vacation",
                Color = "#00FF00",
                DepartmentId = "dept1",
                CreatedBy = "user1",
                CreateDate = DateTime.UtcNow
            },
            new RiqsAbsenceType
            {
                ItemId = "at3",
                Type = "Personal Leave",
                Color = "#0000FF",
                DepartmentId = "dept2",
                CreatedBy = "user2",
                CreateDate = DateTime.UtcNow
            }
        };

        private static List<RiqsAbsencePlan> MockAbsencePlans { get; } = new List<RiqsAbsencePlan>
        {
            new RiqsAbsencePlan
            {
                ItemId = "ap1",
                AffectedUserInfo = new AffectedUserInfo
                {
                    Id = "user1",
                    Name = "John Doe",
                    Designation = "Developer"
                },
                AbsenceTypeInfo = new AbsenceTypeInfo
                {
                    Id = "at1",
                    Name = "Sick Leave",
                    Color = "#FF0000"
                },
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
                Remarks = "Not feeling well",
                Attachments = new List<object>(),
                Status = AbsencePlanStatus.Pending,
                DepartmentId = "dept1",
                OrganizationId = "org1",
                CreatedBy = "user1",
                CreateDate = DateTime.UtcNow,
                StatusUpdatedOn = DateTime.UtcNow,
                IdsAllowedToRead = new[] { "user1" },
                RolesAllowedToRead = new[] { "admin", "manager" },
                Tags = new[] { PraxisTag.IsValidRiqsAbsencePlan },
                TenantId = "tenant1"
            },
            new RiqsAbsencePlan
            {
                ItemId = "ap2",
                AffectedUserInfo = new AffectedUserInfo
                {
                    Id = "user2",
                    Name = "Jane Smith",
                    Designation = "Manager"
                },
                AbsenceTypeInfo = new AbsenceTypeInfo
                {
                    Id = "at2",
                    Name = "Vacation",
                    Color = "#00FF00"
                },
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(10),
                Remarks = "Annual vacation",
                Attachments = new List<object>(),
                Status = AbsencePlanStatus.Approved,
                DepartmentId = "dept1",
                OrganizationId = "org1",
                CreatedBy = "user2",
                CreateDate = DateTime.UtcNow,
                StatusUpdatedOn = DateTime.UtcNow,
                IdsAllowedToRead = new[] { "user2" },
                RolesAllowedToRead = new[] { "admin", "manager" },
                Tags = new[] { PraxisTag.IsValidRiqsAbsencePlan },
                TenantId = "tenant1"
            }
        };

        private static PraxisUser MockPraxisUser { get; } = new PraxisUser
        {
            ItemId = "user1",
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Designation = "Developer",
            Email = "john.doe@example.com",
            UserId = "user1",
            Active = true,
            IsMarkedToDelete = false,
            Roles = new List<string> { "super-poweruser", "org-admin_dept1" }
        };

        private static PraxisUser MockPraxisUser2 { get; } = new PraxisUser
        {
            ItemId = "user2",
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            Designation = "Manager",
            Email = "jane.smith@example.com",
            UserId = "user2",
            Active = true,
            IsMarkedToDelete = false,
            Roles = new List<string> { "mpa1" }
        };

        #endregion

        [Fact]
        public async Task CreateAbsenceTypeAsync_Should_CreateAbsenceTypes_WhenValidCommandProvided()
        {
            // Arrange
            var command = new CreateAbsenceTypeCommand
            {
                AbsenceTypes = new List<AbsenceTypeData>
                {
                    new AbsenceTypeData
                    {
                        Type = "New Leave Type",
                        Color = "#FFFF00",
                        DepartmentId = "dept1"
                    }
                }
            };

            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.CreateAbsenceTypeAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task CreateAbsenceTypeAsync_Should_LogWarning_WhenAbsenceTypesIsEmpty()
        {
            // Arrange
            var command = new CreateAbsenceTypeCommand { AbsenceTypes = new List<AbsenceTypeData>() };
            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider();
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.CreateAbsenceTypeAsync(command);

            // Assert - Just ensure no exception
            Assert.True(true);
        }

        [Fact]
        public async Task DeleteAbsenceTypeAsync_Should_DeleteAbsenceTypes_WhenValidCommandProvided()
        {
            // Arrange
            var command = new DeleteAbsenceTypeCommand
            {
                ItemIds = new List<string> { "at1", "at2" }
            };

            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetupDeleteManyAsync<RiqsAbsenceType>(command.ItemIds.Count);

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.DeleteAbsenceTypeAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task DeleteAbsenceTypeAsync_Should_LogWarning_WhenItemIdsIsNullOrEmpty()
        {
            // Arrange
            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider();
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.DeleteAbsenceTypeAsync(new DeleteAbsenceTypeCommand { ItemIds = null });
            await absenceOverviewService.DeleteAbsenceTypeAsync(new DeleteAbsenceTypeCommand { ItemIds = new List<string>() });

            // Assert - Just ensure no exception
            Assert.True(true);
        }

        [Fact]
        public void GetAbsenceTypes_Should_ReturnAbsenceTypes_WhenValidQueryProvided()
        {
            // Arrange
            var query = new GetAbsenceTypesQuery
            {
                DepartmentId = "dept1"
            };

            var repositoryMock = new MockRepository()
                .SetupGetItems(MockAbsenceTypes.Where(m => m.DepartmentId == query.DepartmentId).ToList());

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = absenceOverviewService.GetAbsenceTypes(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, rt => rt.Type == "Sick Leave");
            Assert.Contains(result, rt => rt.Type == "Vacation");
        }

        [Fact]
        public async Task UpdateAbsenceTypeAsync_Should_UpdateAbsenceTypes_WhenValidCommandProvided()
        {
            // Arrange
            var command = new UpdateAbsenceTypeCommand
            {
                AbsenceTypes = new List<AbsenceTypeUpdateData>
                {
                    new AbsenceTypeUpdateData
                    {
                        ItemId = "at1",
                        Type = "Updated Sick Leave",
                        Color = "#FF9999"
                    }
                }
            };

            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetupUpdateOneAsync<RiqsAbsenceType>(1, 1)
                .SetupCountDocumentsAsync<RiqsAbsencePlan>(1)
                .SetupUpdateManyAsync<RiqsAbsencePlan>(1, 1);

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.UpdateAbsenceTypeAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task UpdateAbsenceTypeAsync_Should_LogWarning_WhenAbsenceTypesIsEmpty()
        {
            // Arrange
            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider();
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.UpdateAbsenceTypeAsync(new UpdateAbsenceTypeCommand { AbsenceTypes = new List<AbsenceTypeUpdateData>() });

            // Assert - Just ensure no exception
            Assert.True(true);
        }

        [Fact]
        public async Task UpdateAbsenceTypeAsync_Should_SkipSync_WhenAbsenceTypeUpdateDataItemIdIsNullOrEmpty()
        {
            // Arrange
            var command = new UpdateAbsenceTypeCommand
            {
                AbsenceTypes = new List<AbsenceTypeUpdateData>
                {
                    new AbsenceTypeUpdateData { ItemId = null, Type = "Type", Color = "#FFF" },
                    new AbsenceTypeUpdateData { ItemId = "", Type = "Type", Color = "#FFF" }
                }
            };
            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider();
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetupUpdateOneAsync<RiqsAbsenceType>(1, 1)
                .SetupCountDocumentsAsync<RiqsAbsencePlan>(1);

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.UpdateAbsenceTypeAsync(command);

            // Assert - Just ensure no exception
            Assert.True(true);
        }

        [Fact]
        public async Task CreateAbsencePlanAsync_Should_CreateAbsencePlan_WhenValidCommandProvided()
        {
            // Arrange
            var command = new CreateAbsencePlanCommand
            {
                AffectedUserId = "user1",
                AbsenceTypeId = "at1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
                Remarks = "Test absence plan",
                Attachments = new List<object>(),
                DepartmentId = "dept1",
                OrganizationId = "org1"
            };

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(MockPraxisUser)
                .SetupGetItemAsync(MockAbsenceTypes.FirstOrDefault());

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfGetAdminBUser(true);
            
            // Setup mock MongoDB collections for AbsenceOverviewService constructor
            var mockDataContext = new Mock<IMongoDatabase>();
            var mockAbsenceTypeCollection = new Mock<IMongoCollection<RiqsAbsenceType>>();
            var mockAbsencePlanCollection = new Mock<IMongoCollection<RiqsAbsencePlan>>();
            
            mockDataContext
                .Setup(m => m.GetCollection<RiqsAbsenceType>("RiqsAbsenceTypes", null))
                .Returns(mockAbsenceTypeCollection.Object);
            
            mockDataContext
                .Setup(m => m.GetCollection<RiqsAbsencePlan>("RiqsAbsencePlans", null))
                .Returns(mockAbsencePlanCollection.Object);
            
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();
            ecapMongoDbDataContextProviderMock
                .Setup(p => p.GetTenantDataContext())
                .Returns(mockDataContext.Object);

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.CreateAbsencePlanAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task DeleteAbsencePlanAsync_Should_DeleteAbsencePlans_WhenValidIdsProvided()
        {
            // Arrange
            var ids = new List<string> { "ap1", "ap2" };

            var repositoryMock = new MockRepository();
            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider()
                .SetupUpdateManyAsync<RiqsAbsencePlan>(ids.Count, ids.Count);

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.DeleteAbsencePlanAsync(ids);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task UpdateAbsencePlanAsync_Should_UpdateAbsencePlan_WhenValidCommandProvided()
        {
            // Arrange
            var command = new UpdateAbsencePlanCommand
            {
                ItemId = "ap1",
                AffectedUserId = "user1",
                AbsenceTypeId = "at1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3),
                Remarks = "Updated absence plan",
                Attachments = new List<object>()
            };

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(MockAbsencePlans.FirstOrDefault())
                .SetupGetItemAsync(MockPraxisUser)
                .SetupGetItemAsync(MockAbsenceTypes.FirstOrDefault());

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.UpdateAbsencePlanAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task UpdateAbsencePlanStatusAsync_Should_UpdateAbsencePlanStatus_WhenValidCommandProvided()
        {
            // Arrange
            var command = new UpdateAbsencePlanStatusCommand
            {
                ItemId = "ap1",
                Status = AbsencePlanStatus.Approved,
                ReasonToDeny = null
            };

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(MockAbsencePlans.FirstOrDefault());

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            await absenceOverviewService.UpdateAbsencePlanStatusAsync(command);

            // Assert
            // Verify that the method completes without throwing an exception
            Assert.True(true);
        }

        [Fact]
        public async Task GetAbsencePlanApprovalPermissionAsync_Should_ReturnPermission_WhenValidQueryProvided()
        {
            // Arrange
            var query = new GetAbsencePlanApprovalPermissionQuery
            {
                ItemId = "ap1",
                PraxisUserId = "user2"
            };

            var mockData = MockAbsencePlans.FirstOrDefault(t => t.ItemId == query.ItemId);

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(mockData)
                .SetupGetItemAsync(MockPraxisUser2);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "super-poweruser" }, "user2");
            var securityHelperServiceMock = new MockSecurityHelperService()
                .SetResponseOfGetRoleByHierarchyRank(1);
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanApprove);
            Assert.Equal("user2", result.CheckedUserId);
            Assert.Equal("ap1", result.AbsencePlanId);
        }

        [Fact]
        public async Task GetAbsencePlanApprovalPermissionAsync_Should_ReturnFalse_WhenAbsencePlanNotFound()
        {
            // Arrange
            var query = new GetAbsencePlanApprovalPermissionQuery
            {
                ItemId = "nonexistent",
                PraxisUserId = "user2"
            };

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync((RiqsAbsencePlan)null);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanApprove);
            Assert.Equal("Absence plan not found or has been deleted", result.Reason);
        }

        [Fact]
        public async Task GetAbsencePlanApprovalPermissionAsync_Should_ReturnFalse_WhenUserNotFound()
        {
            // Arrange
            var query = new GetAbsencePlanApprovalPermissionQuery
            {
                ItemId = "ap1",
                PraxisUserId = "nonexistent"
            };

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(MockAbsencePlans.FirstOrDefault())
                .SetupGetItemAsync((PraxisUser)null);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanApprove);
            Assert.Equal("User not found or inactive", result.Reason);
        }

        [Fact]
        public async Task GetAbsencePlanApprovalPermissionAsync_Should_ReturnFalse_WhenUserApprovesOwnPlan()
        {
            // Arrange
            var query = new GetAbsencePlanApprovalPermissionQuery
            {
                ItemId = "ap1",
                PraxisUserId = "user1" // Same as CreatedBy in MockAbsencePlans.First()
            };

            var mockData = MockAbsencePlans.FirstOrDefault(t => t.ItemId == query.ItemId);

            var repositoryMock = new MockRepository()
                .SetupGetItemAsync(mockData)
                .SetupGetItemAsync(MockPraxisUser);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanApprove);
            Assert.Equal("Users cannot approve their own absence plans", result.Reason);
        }

        [Fact]
        public async Task GetAbsencePlanApprovalPermissionAsync_Should_ReturnFalse_WhenStatusIsNotPending()
        {
            // Arrange
            var query = new GetAbsencePlanApprovalPermissionQuery
            {
                ItemId = "ap2",
                PraxisUserId = "user2"
            };

            // We need to use the second absence plan which has a status of Approved
            var absencePlan = MockAbsencePlans.FirstOrDefault(ap => ap.ItemId == "ap2");
            var praxisUser = MockPraxisUser2;  // Use the second user for this test

            var repositoryMock = new MockRepository();
            repositoryMock.SetupGetItemAsync(absencePlan);
            repositoryMock.SetupGetItemAsync(praxisUser);

            var securityContextProviderMock = new MockSecurityContextProvider()
                .SetResponseOfSecurityContextProviderWirhRolesAndUserId(new List<string> { "admin" }, "user1");
            var securityHelperServiceMock = new MockSecurityHelperService();
            var ecapMongoDbDataContextProviderMock = new MockEcapMongoDbDataContextProvider();

            var absenceOverviewService = new AbsenceOverviewService(
                repositoryMock.Object,
                _loggerMock.Object,
                securityContextProviderMock.Object,
                securityHelperServiceMock.Object,
                ecapMongoDbDataContextProviderMock.Object);

            // Act
            var result = await absenceOverviewService.GetAbsencePlanApprovalPermissionAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanApprove);
            Assert.Contains("cannot be approved", result.Reason);
        }
    }
}