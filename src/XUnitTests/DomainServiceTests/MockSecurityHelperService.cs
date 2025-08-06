using Moq;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System.Collections.Generic;

namespace XUnitTests.DomainServiceTests
{
    public class MockSecurityHelperService : Mock<ISecurityHelperService>
    {
        public MockSecurityHelperService SetResponseOfDepartmentLevelUser(bool result)
        {
            Setup(s => s.IsADepartmentLevelUser())
                .Returns(result);
            return this;
        }

        public MockSecurityHelperService SetResponseOfGetAdminBUser(bool result)
        {
            Setup(s => s.IsAAdminBUser())
                .Returns(result);
            return this;
        }

        public MockSecurityHelperService SetResponseOfGetOrganizationFromOrgLevelUser(string result)
        {
            Setup(s => s.ExtractOrganizationFromOrgLevelUser())
                .Returns(result);
            return this;
        }

        public MockSecurityHelperService SetResponseOfGetDepartmentIdFromDepartmentLevelUser(string result)
        {
            Setup(s => s.ExtractDepartmentIdFromDepartmentLevelUser())
                .Returns(result);
            return this;
        }

        public MockSecurityHelperService SetResponseOfGetRoleByHierarchyRank(int result)
        {
            Setup(s => s.GetRoleByHierarchyRank(It.IsAny<List<string>>()))
                .Returns(result);
            return this;
        }
    }
}