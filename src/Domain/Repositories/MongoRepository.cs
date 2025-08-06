using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices;

namespace Selise.Ecap.SC.Wopi.Domain.Repositories
{
    public class MongoRepository : IMongoRepository
    {
        private readonly IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider;

        public MongoRepository(IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider)
        {
            this.ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            return ecapMongoDbDataContextProvider.GetTenantDataContext().GetCollection<T>($"{typeof(T).Name}s");
        }

        private IDictionary<string, string> GetValues(object obj)
        {
            return obj
                .GetType()
                .GetProperties()
                .ToDictionary(p =>
                        p.Name,
                    p => p.GetValue(obj)?.ToString()
                );
        }

        public T GetItem<T>(Expression<Func<T, bool>> dataFilters)
        {
            return GetCollection<T>().AsQueryable().FirstOrDefault(dataFilters);
        }

        public async Task<T> GetItemAsync<T>(Expression<Func<T, bool>> dataFilters)
        {
            return await Task.Run(() => GetCollection<T>().AsQueryable().FirstOrDefault(dataFilters));
        }

        public IQueryable<T> GetItems<T>()
        {
            return GetCollection<T>().AsQueryable();
        }

        public IQueryable<T> GetItems<T>(Expression<Func<T, bool>> dataFilters)
        {
            return GetCollection<T>().AsQueryable().Where(dataFilters);
        }

        public async Task<IQueryable<T>> GetItemsAsync<T>()
        {
            return await Task.Run(() => GetCollection<T>().AsQueryable());
        }

        public async Task<IQueryable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> dataFilters)
        {
            return await Task.Run(() => GetCollection<T>().AsQueryable().Where(dataFilters));
        }

        public void Save<T>(T data)
        {
            GetCollection<T>().InsertOne(data);
        }

        public void Save<T>(List<T> datas)
        {
            GetCollection<T>().InsertMany(datas);
        }

        public void Update<T>(Expression<Func<T, bool>> dataFilters, T data)
        {
            GetCollection<T>().ReplaceOne(dataFilters, data);
        }

        public void UpdateMany<T>(Expression<Func<T, bool>> dataFilters, object data)
        {
            int counter = 0;

            IDictionary<string, string> fieldValuePairs = this.GetValues(data);
            UpdateDefinition<T> update = null;
            foreach (var fieldValuePair in fieldValuePairs)
            {
                if (counter == 0)
                {
                    update = Builders<T>.Update.Set(fieldValuePair.Key, fieldValuePair.Value);
                }
                else
                {

                    update = update.Set(fieldValuePair.Key, fieldValuePair.Value);
                }

                counter++;
            }

            GetCollection<T>().UpdateMany(dataFilters, update);
        }

        public void Delete<T>(Expression<Func<T, bool>> dataFilters)
        {
            GetCollection<T>().DeleteMany(dataFilters);
        }
    }
}
