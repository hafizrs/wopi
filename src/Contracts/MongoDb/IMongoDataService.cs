using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb
{
    public interface IMongoDataService
    {
        IRepository Repository();
        bool Insert<T>(object payload, bool useImpersonation = false);
        bool Update<T>(string itemId, Dictionary<string, object> updates, bool useImpersonation = false);
        bool Vary(VaryDto varyDto);
        EntityBase GetEntityDetials<T>(string itemId, bool useImpersonation = true);
        EntityBase GetEntityDetials(Type type, string entityName, string itemId, bool useImpersonation = true);
        EntityBase GetEntityDetials<T>(string itemId, List<string> tagNames, bool useImpersonation = true);
        EntityBase GetEntityDetials(Type type, string entityName, string itemId, List<string> tagNames, bool useImpersonation = true);
        T GetById<T>(string itemId, List<string> fields, bool useImpersonation = false);
        EntityDbQueryResponse<T> GetList<T>(List<string> fields, int pageNum = 0, int itemsPerPage = 100, bool useImpersonation = false);
        EntityDbQueryResponse<T> GetList<T>(FilterDefinition<T> filter, int pageNum = 0, int itemsPerPage = 100, bool useImpersonation = false);
        EntityDbQueryResponse<T> GetListByFilter<T>(FilterDefinition<T> filter, int pageNumber = 0, int itemsPerPage = 100, List<string> fields = null, string orderBy = "CreateDate", bool descending = true, bool useImpersonation = true);
        EntityDbQueryResponse<T> GetListBySql<T>(string filterQuery, bool useImpersonation = true);
        EntityDbQueryResponse<T> GetListBySql<T>(string filterQuery, int pageNumber = 0, int pageLimit = 100, bool useImpersonation = true);
        bool Connect(ConnectionQuery connectionQuery, bool useImpersonation = false);
        bool Disconnect(string itemId, bool useImpersonation = false);
        ConnectionDataList GetConnections(ConnectionQuery connectionQuery, int pageNum = 0, int itemsPerPage = 999, bool useImpersonation = false);
        ConnectionDataList GetConnections(ConnectionQuery connectionQuery, FilterDefinition<BsonDocument> filter = null, int pageNum = 0, int itemsPerPage = 999, bool useImpersonation = false);
    }
}
