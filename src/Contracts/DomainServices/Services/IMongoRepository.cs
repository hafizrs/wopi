using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IMongoRepository
    {
        IMongoCollection<T> GetCollection<T>();

        T GetItem<T>(Expression<Func<T, bool>> dataFilters);

        IQueryable<T> GetItems<T>();

        IQueryable<T> GetItems<T>(Expression<Func<T, bool>> dataFilters);

        void Save<T>(T data);

        void Save<T>(List<T> datas);

        void Update<T>(Expression<Func<T, bool>> dataFilters, T data);

        void UpdateMany<T>(Expression<Func<T, bool>> dataFilters, object data);

        void Delete<T>(Expression<Func<T, bool>> dataFilters);
    }
}
