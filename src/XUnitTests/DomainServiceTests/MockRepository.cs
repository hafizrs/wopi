using Moq;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace XUnitTests.DomainServiceTests
{
    public class MockRepository : Mock<IRepository>
    {
        public MockRepository SetupGetItem<T>(T item) where T : class
        {
            Setup(r => r.GetItem(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns(item);
            return this;
        }

        public MockRepository SetupGetItemAsync<T>(T item) where T : class
        {
            Setup(r => r.GetItemAsync<T>(It.IsAny<Expression<Func<T, bool>>>()))
                .ReturnsAsync(item);
            return this;
        }

        public MockRepository SetupGetItems<T>(List<T> items) where T : class
        {
            Setup(r => r.GetItems<T>(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns(items.AsQueryable());
            return this;
        }
    }
}