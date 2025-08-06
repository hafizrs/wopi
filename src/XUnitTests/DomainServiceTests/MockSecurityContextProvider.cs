using System;
using System.Collections.Generic;
using Moq;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace XUnitTests.DomainServiceTests
{
    public class MockSecurityContextProvider : Mock<ISecurityContextProvider>
    {
        public MockSecurityContextProvider SetResponseOfSecurityContextProvider()
        {
            Setup(s => s.GetSecurityContext())
                .Returns(new SecurityContext());
            return this;
        }
        
        public MockSecurityContextProvider SetResponseOfSecurityContextProviderWirhRolesAndUserId(List<string> roles, string userId)
        {
            Setup(s => s.GetSecurityContext())
                .Returns(new SecurityContext("", roles, "", "", true, true, "", userId, "", "", "",
                    "", "", "", "", null, "", false, "", false, DateTime.UtcNow, "", "",""));
            return this;
        }
    }
}