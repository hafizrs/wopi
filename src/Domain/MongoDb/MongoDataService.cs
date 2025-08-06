using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformSpecific;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Helpers;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb.Infrastructure;
using Selise.Ecap.SC.Wopi.Contracts.MongoDb;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Selise.Ecap.SC.Wopi.Domain.MongoDb
{
    public class MongoDataService : IMongoDataService
    {
        private readonly IRepository repo;
        private readonly IMongoClientRepository repository;
        private readonly ILogger<MongoDataService> ecapLogger;
        private readonly ISecurityContextProvider securityDataProvider;

        public MongoDataService(
            IRepository repo, 
            IMongoClientRepository repository, 
            ILogger<MongoDataService> ecapLogger, 
            ISecurityContextProvider securityDataProvider)
        {
            this.repo = repo;
            this.ecapLogger = ecapLogger;
            this.repository = repository;
            this.securityDataProvider = securityDataProvider;
        }

        public IRepository Repository()
        {
            return repo;
        }

        public bool Insert<T>(object payload, bool useImpersonation = false)
        {
            string name = typeof(T).Name;
            try
            {
                if (!useImpersonation && !HasWriteAccess(name))
                {
                    return false;
                }

                repository.Insert(name, payload);
                return true;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION Insert :: entityName-> " + name + " ", ex);
            }

            return false;
        }

        public bool Update<T>(string itemId, Dictionary<string, object> updates, bool useImpersonation = false)
        {
            string name = typeof(T).Name;
            try
            {
                if (!useImpersonation && !HasUpdateAccess<T>(itemId))
                {
                    return false;
                }

                repository.Update(name, itemId, updates);
                return true;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION MongoDataService - Update :: entityName-> " + name + " itemId -> " + itemId + " ", ex);
            }

            return false;
        }

        public bool Vary(VaryDto varyDto)
        {
            try
            {
                bool flag = false;
                IMongoCollection<BsonDocument> collection = repository.GetCollection(varyDto.EntityName);
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", varyDto.ItemId);
                int value = 0;
                if (varyDto.Operation.Equals(OperationType.Decrease))
                {
                    flag = true;
                    value = -1 * Math.Abs(varyDto.Value);
                }
                else if (varyDto.Operation.Equals(OperationType.Increase))
                {
                    flag = true;
                    value = Math.Abs(varyDto.Value);
                }

                if (flag)
                {
                    UpdateDefinition<BsonDocument> updateDefinition = Builders<BsonDocument>.Update.Inc(varyDto.Field, value);
                    BsonDocument bsonDocument = collection.FindOneAndUpdate(filter, updateDefinition);
                    ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] INFO " + $"MongoDataService - Vary :: varyDto -> {varyDto} " + LogHelpers.JsonToString("update", updateDefinition));
                    return true;
                }
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION " + $"MongoDataService - Vary :: varyDto -> {varyDto}", ex);
            }

            return false;
        }

        public EntityBase GetEntityDetials<T>(string itemId, bool useImpersonation = true)
        {
            return repository.GetEntityDetials(typeof(T), typeof(T).Name, itemId, null, useImpersonation);
        }

        public EntityBase GetEntityDetials(Type type, string entityName, string itemId, bool useImpersonation = true)
        {
            return repository.GetEntityDetials(type, entityName, itemId, useImpersonation);
        }

        public EntityBase GetEntityDetials<T>(string itemId, List<string> tagNames, bool useImpersonation = true)
        {
            return repository.GetEntityDetials(typeof(T), typeof(T).Name, itemId, tagNames, useImpersonation);
        }

        public EntityBase GetEntityDetials(Type type, string entityName, string itemId, List<string> tagNames, bool useImpersonation = true)
        {
            return repository.GetEntityDetials(type, entityName, itemId, tagNames, useImpersonation);
        }

        public T GetById<T>(string itemId, List<string> fields, bool useImpersonation = false)
        {
            FilterDefinitionBuilder<T> filter = Builders<T>.Filter;
            FilterDefinition<T> filter2 = filter.Eq("_id", itemId);
            EntityDbQueryResponse<T> listByFilter = GetListByFilter(filter2, 0, 100, fields, "CreateDate", descending: true, useImpersonation);
            if (listByFilter != null && listByFilter.Results.Count > 0)
            {
                return listByFilter.Results[0];
            }

            return default(T);
        }

        public EntityDbQueryResponse<T> GetList<T>(List<string> fields, int pageNum = 0, int itemsPerPage = 100, bool useImpersonation = false)
        {
            return GetListByFilter<T>(null, pageNum, itemsPerPage, fields, "CreateDate", descending: true, useImpersonation);
        }

        public EntityDbQueryResponse<T> GetList<T>(FilterDefinition<T> filter, int pageNum = 0, int itemsPerPage = 100, bool useImpersonation = false)
        {
            return GetListByFilter(filter, pageNum, itemsPerPage, null, "CreateDate", descending: true, useImpersonation);
        }

        public EntityDbQueryResponse<T> GetListByFilter<T>(FilterDefinition<T> filter, int pageNumber = 0, int itemsPerPage = 100, List<string> fields = null, string orderBy = "CreateDate", bool descending = true, bool useImpersonation = true)
        {
            string name = typeof(T).Name;
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            string tenantId = securityContext.TenantId;
            IEnumerable<string> enumerable = securityContext.Roles;
            if (string.IsNullOrEmpty(tenantId))
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] FAIL GetListByFilter (Invalid DB Context) :: entityName-> " + name);
                throw new Exception("Invalid DB Context");
            }

            try
            {
                if (useImpersonation)
                {
                    if (enumerable != null)
                    {
                        enumerable.ToList().Add("admin");
                    }
                    else
                    {
                        enumerable = new List<string> { "admin" };
                    }
                }

                if (itemsPerPage > 100)
                {
                    itemsPerPage = 100;
                }

                int value = Math.Max(0, pageNumber * itemsPerPage);
                List<SortObjects> sortBy = new List<SortObjects>
                {
                    new SortObjects
                    {
                        PropName = orderBy,
                        SortOrder = (descending ? SortOrder.Descending : SortOrder.Ascending)
                    }
                };
                BsonDocument bsonDocument = FilterBuilder.SortBuilder(sortBy);
                FilterDefinition<T> filter2 = null;
                FilterDefinitionBuilder<T> filter3 = Builders<T>.Filter;
                if (!string.IsNullOrEmpty(userId.ToString()) && enumerable != null)
                {
                    filter2 = filter3.In("IdsAllowedToRead", new string[1] { userId.ToString() }) | filter3.In("RolesAllowedToRead", enumerable);
                }
                else if (!string.IsNullOrEmpty(userId.ToString()))
                {
                    filter2 = filter3.In("IdsAllowedToRead", new string[1] { userId.ToString() });
                }
                else if (enumerable != null)
                {
                    filter2 = filter3.In("RolesAllowedToRead", enumerable);
                }

                filter2 &= filter;
                ProjectionDefinition<T> projectionDefinition = null;
                if (fields != null && fields.Count > 0)
                {
                    ProjectionDefinitionBuilder<T> projectionBuilder = Builders<T>.Projection;
                    projectionDefinition = projectionBuilder.Combine(fields.Select((string field) => projectionBuilder.Include(field)));
                }

                long totalRecordCount = repository.GetCollection<T>().Find(filter2).CountDocuments();
                List<T> results;
                if (projectionDefinition != null)
                {
                    List<BsonDocument> obj = repository.GetCollection<T>().Find(filter2).Project(projectionDefinition)
                        .Limit(itemsPerPage)
                        .Skip(value)
                        .Sort(bsonDocument)
                        .ToList();
                    results = BsonSerializer.Deserialize<List<T>>(obj.ToJson());
                }
                else
                {
                    results = repository.GetCollection<T>().Find(filter2).Limit(itemsPerPage)
                        .Skip(value)
                        .Sort(bsonDocument)
                        .ToList();
                }

                return new EntityDbQueryResponse<T>
                {
                    StatusCode = 0,
                    Results = results,
                    TotalRecordCount = totalRecordCount,
                    ErrorMessages = new List<string>()
                };
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION GetListByFilter :: entityName-> " + name, ex);
            }

            return new EntityDbQueryResponse<T>
            {
                StatusCode = 0,
                Results = new List<T>(),
                TotalRecordCount = 0L,
                ErrorMessages = new List<string>()
            };
        }

        public EntityDbQueryResponse<T> GetListBySql<T>(string filterQuery, bool useImpersonation = true)
        {
            return GetListBySqlFilter<T>(filterQuery, 0, 100, useImpersonation);
        }

        public EntityDbQueryResponse<T> GetListBySql<T>(string filterQuery, int pageNumber = 0, int pageLimit = 100, bool useImpersonation = true)
        {
            return GetListBySqlFilter<T>(filterQuery, pageNumber, pageLimit, useImpersonation, userCustomPaging: true);
        }

        private EntityDbQueryResponse<T> GetListBySqlFilter<T>(string filterQuery, int pageNumber = 0, int pageLimit = 100, bool useImpersonation = true, bool userCustomPaging = false)
        {
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string tenantId = securityContext.TenantId;
            if (string.IsNullOrEmpty(tenantId))
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] FAIL GetListBySqlFilter (Invalid DB Context) ");
                throw new Exception("Invalid DB Context");
            }

            string errorMessage = string.Empty;
            List<string> projections = new List<string>();
            try
            {
                if (useImpersonation)
                {
                    securityContext.Roles.ToList().Add("admin");
                }

                ParsedSQLQuery parsedSQLQuery = GetFilteredSQLQuery.GetParsedSQLQuery<T>(filterQuery);
                parsedSQLQuery.AddRowLevelSecurityParameters(securityContext.UserId.ToString(), securityContext.Roles.ToArray());
                projections = parsedSQLQuery.Fields.ToList();
                Type entityType = GetFilteredSQLQuery.GetEntityType<T>();
                if (FilterBuilder.TrySQLParsedFilterBuild(entityType, parsedSQLQuery, out errorMessage, out var filter) && parsedSQLQuery.Fields != null && parsedSQLQuery.Fields.Any())
                {
                    BsonDocument bsonDocument = FilterBuilder.BuildProjectDocument(parsedSQLQuery.Fields.ToList());
                    BsonDocument bsonDocument2 = FilterBuilder.SortBuilder(parsedSQLQuery.SortBy.ToList());
                    if (!userCustomPaging)
                    {
                        pageNumber = 0;
                        pageLimit = 100;
                        if (parsedSQLQuery.PageLimit > 0)
                        {
                            pageLimit = parsedSQLQuery.PageLimit.Value;
                        }

                        if (parsedSQLQuery.PageNumber > -1)
                        {
                            pageNumber = parsedSQLQuery.PageNumber.Value;
                        }
                    }

                    int value = Math.Max(0, pageNumber * pageLimit);
                    filter = filter ?? new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument());
                    long totalRecordCount = repository.GetCollection(parsedSQLQuery.EntityName).Find(filter).CountDocuments();
                    List<BsonDocument> obj = repository.GetCollection(parsedSQLQuery.EntityName).Find(filter).Sort(bsonDocument2)
                        .Limit(pageLimit)
                        .Skip(value)
                        .Project(bsonDocument)
                        .ToList();
                    List<T> results = BsonSerializer.Deserialize<List<T>>(obj.ToJson());
                    return new EntityDbQueryResponse<T>
                    {
                        StatusCode = 0,
                        Results = results,
                        TotalRecordCount = totalRecordCount,
                        Projections = projections,
                        ErrorMessages = new List<string> { errorMessage }
                    };
                }
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION  GetListBySqlFilter", ex);
            }

            return new EntityDbQueryResponse<T>
            {
                Results = null,
                StatusCode = -1,
                TotalRecordCount = 0L,
                Projections = projections,
                ErrorMessages = new List<string> { errorMessage }
            };
        }

        public bool Connect(ConnectionQuery connectionQuery, bool useImpersonation = false)
        {
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            string tenantId = securityContext.TenantId;
            try
            {
                if (!useImpersonation && !HasWriteAccess("Connection"))
                {
                    return false;
                }

                EntityAccessPermission entityDefaultPermissionSettings = GetEntityDefaultPermissionSettings("Connection");
                if (entityDefaultPermissionSettings == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(repo.GetItem((Connection c) => (c.ChildEntityID.Equals(connectionQuery.ChildEntityId) && c.ChildEntityName.Equals(connectionQuery.ChildEntityName) && c.ParentEntityID.Equals(connectionQuery.ParentEntityId) && c.ParentEntityName.Equals(connectionQuery.ParentEntityName)) || c.ItemId.Equals(connectionQuery.ItemId))?.ItemId))
                {
                    return true;
                }

                Connection primaryEntity = new Connection
                {
                    TenantId = tenantId.ToString(),
                    ChildEntityID = connectionQuery.ChildEntityId,
                    ChildEntityName = connectionQuery.ChildEntityName,
                    ParentEntityID = connectionQuery.ParentEntityId,
                    ParentEntityName = connectionQuery.ParentEntityName,
                    Tags = connectionQuery.Tags,
                    ItemId = connectionQuery.ItemId,
                    CreatedBy = userId.ToString(),
                    CreateDate = DateTime.UtcNow,
                    LastUpdateDate = DateTime.UtcNow,
                    EmbededInfo = connectionQuery.EmbededInfo,
                    IdsAllowedToRead = (from s in entityDefaultPermissionSettings.IdsAllowedToRead?.ToArray()
                                        select s.Replace("OWNER", userId.ToString())).ToArray(),
                    IdsAllowedToWrite = (from s in entityDefaultPermissionSettings.IdsAllowedToWrite?.ToArray()
                                         select s.Replace("OWNER", userId.ToString())).ToArray(),
                    IdsAllowedToUpdate = (from s in entityDefaultPermissionSettings.IdsAllowedToUpdate?.ToArray()
                                          select s.Replace("OWNER", userId.ToString())).ToArray(),
                    IdsAllowedToDelete = (from s in entityDefaultPermissionSettings.IdsAllowedToDelete?.ToArray()
                                          select s.Replace("OWNER", userId.ToString())).ToArray(),
                    RolesAllowedToRead = entityDefaultPermissionSettings.RolesAllowedToWrite?.ToArray(),
                    RolesAllowedToWrite = entityDefaultPermissionSettings.RolesAllowedToWrite?.ToArray(),
                    RolesAllowedToUpdate = entityDefaultPermissionSettings.RolesAllowedToUpdate?.ToArray(),
                    RolesAllowedToDelete = entityDefaultPermissionSettings.RolesAllowedToDelete?.ToArray()
                };
                repository.Insert("Connection", primaryEntity);
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION  Connect", ex);
                return false;
            }

            return true;
        }

        public bool Disconnect(string itemId, bool useImpersonation = false)
        {
            try
            {
                if (!useImpersonation && !HasDeleteAccess<Connection>(itemId))
                {
                    return false;
                }

                Connection entityPayload = new Connection
                {
                    ItemId = itemId
                };
                repository.Delete("Connection", entityPayload);
                return true;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION Disconnect :: entityName-> Connection ", ex);
            }

            return false;
        }

        public ConnectionDataList GetConnections(ConnectionQuery connectionQuery, int pageNum = 0, int itemsPerPage = 999, bool useImpersonation = false)
        {
            return GetConnections(connectionQuery, null, pageNum, itemsPerPage, useImpersonation);
        }

        public ConnectionDataList GetConnections(ConnectionQuery connectionQuery, FilterDefinition<BsonDocument> filter = null, int pageNum = 0, int itemsPerPage = 999, bool useImpersonation = false)
        {
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            string tenantId = securityContext.TenantId;
            IEnumerable<string> enumerable = securityContext.Roles;
            if (string.IsNullOrEmpty(tenantId))
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] FAIL  GetListByFilter (Invalid DB Context)");
                throw new Exception("Invalid DB Context");
            }

            long totalRecordCount = 0L;
            List<ConnectionResult> list = new List<ConnectionResult>();
            try
            {
                if (useImpersonation)
                {
                    if (enumerable != null)
                    {
                        enumerable.ToList().Add("admin");
                    }
                    else
                    {
                        enumerable = new List<string> { "admin" };
                    }
                }

                if (string.IsNullOrEmpty(connectionQuery.ParentEntityName) && string.IsNullOrEmpty(connectionQuery.ParentEntityId) && string.IsNullOrEmpty(connectionQuery.ChildEntityName) && string.IsNullOrEmpty(connectionQuery.ChildEntityId) && string.IsNullOrEmpty(connectionQuery.UserId) && connectionQuery.Roles == null && connectionQuery.Tags == null)
                {
                    return new ConnectionDataList
                    {
                        Results = new List<ConnectionResult>(),
                        TotalRecordCount = 0L
                    };
                }

                FilterDefinitionBuilder<BsonDocument> filter2 = Builders<BsonDocument>.Filter;
                if (!string.IsNullOrEmpty(connectionQuery.ParentEntityName))
                {
                    FilterDefinition<BsonDocument> filterDefinition = filter2.Eq("ParentEntityName", connectionQuery.ParentEntityName);
                    if (filter != null)
                    {
                        filter &= filterDefinition;
                    }
                    else
                    {
                        filter = filterDefinition;
                    }
                }

                if (!string.IsNullOrEmpty(connectionQuery.ParentEntityId))
                {
                    FilterDefinition<BsonDocument> filterDefinition2 = filter2.Eq("ParentEntityID", connectionQuery.ParentEntityId);
                    if (filter != null)
                    {
                        filter &= filterDefinition2;
                    }
                    else
                    {
                        filter = filterDefinition2;
                    }
                }

                if (connectionQuery.ParentEntityIds != null && connectionQuery.ParentEntityIds.Length != 0)
                {
                    FilterDefinition<BsonDocument> filterDefinition3 = filter2.In("ParentEntityID", connectionQuery.ParentEntityIds);
                    if (filter != null)
                    {
                        filter &= filterDefinition3;
                    }
                    else
                    {
                        filter = filterDefinition3;
                    }
                }

                if (!string.IsNullOrEmpty(connectionQuery.ChildEntityName))
                {
                    FilterDefinition<BsonDocument> filterDefinition4 = filter2.Eq("ChildEntityName", connectionQuery.ChildEntityName);
                    if (filter != null)
                    {
                        filter &= filterDefinition4;
                    }
                    else
                    {
                        filter = filterDefinition4;
                    }
                }

                if (!string.IsNullOrEmpty(connectionQuery.ChildEntityId))
                {
                    FilterDefinition<BsonDocument> filterDefinition5 = filter2.Eq("ChildEntityID", connectionQuery.ChildEntityId);
                    if (filter != null)
                    {
                        filter &= filterDefinition5;
                    }
                    else
                    {
                        filter = filterDefinition5;
                    }
                }

                if (connectionQuery.ChildEntityIds != null && connectionQuery.ChildEntityIds.Length != 0)
                {
                    FilterDefinition<BsonDocument> filterDefinition6 = filter2.In("ChildEntityID", connectionQuery.ChildEntityIds);
                    if (filter != null)
                    {
                        filter &= filterDefinition6;
                    }
                    else
                    {
                        filter = filterDefinition6;
                    }
                }

                if (connectionQuery.Tags != null && connectionQuery.Tags.Length != 0)
                {
                    FilterDefinition<BsonDocument> filterDefinition7 = filter2.In("Tags", connectionQuery.Tags);
                    if (filter != null)
                    {
                        filter &= filterDefinition7;
                    }
                    else
                    {
                        filter = filterDefinition7;
                    }
                }

                if (!string.IsNullOrEmpty(userId.ToString()) && enumerable != null)
                {
                    FilterDefinition<BsonDocument> filterDefinition8 = filter2.In("IdsAllowedToRead", new string[1] { userId.ToString() }) | filter2.In("RolesAllowedToRead", enumerable);
                    if (filter != null)
                    {
                        filter &= filterDefinition8;
                    }
                    else
                    {
                        filter = filterDefinition8;
                    }
                }
                else if (!string.IsNullOrEmpty(userId.ToString()))
                {
                    FilterDefinition<BsonDocument> filterDefinition9 = filter2.In("IdsAllowedToRead", new string[1] { userId.ToString() });
                    if (filter != null)
                    {
                        filter &= filterDefinition9;
                    }
                    else
                    {
                        filter = filterDefinition9;
                    }
                }
                else if (enumerable != null)
                {
                    FilterDefinition<BsonDocument> filterDefinition10 = filter2.In("RolesAllowedToRead", enumerable);
                    if (filter != null)
                    {
                        filter &= filterDefinition10;
                    }
                    else
                    {
                        filter = filterDefinition10;
                    }
                }

                int value = Math.Max(0, pageNum * itemsPerPage);
                if (filter != null)
                {
                    List<SortObjects> sortBy = new List<SortObjects>
                    {
                        new SortObjects
                        {
                            PropName = "CreateDate",
                            SortOrder = SortOrder.Descending
                        }
                    };
                    BsonDocument bsonDocument = FilterBuilder.SortBuilder(sortBy);
                    totalRecordCount = repository.GetCollection("Connection").CountDocuments(filter);
                    IFindFluent<BsonDocument, BsonDocument> findFluent = repository.GetCollection("Connection").Find(filter);
                    IFindFluent<BsonDocument, BsonDocument> source = findFluent.Sort(bsonDocument).Limit(itemsPerPage).Skip(value);
                    List<BsonDocument> list2 = source.ToList();
                    foreach (BsonDocument item2 in list2)
                    {
                        Connection connection = (Connection)BsonSerializer.Deserialize(item2, typeof(Connection));
                        EntityBase parent = (connectionQuery.ReadPrant ? repository.GetEntityDetials(typeof(Connection), connection.ParentEntityName, connection.ParentEntityID) : null);
                        EntityBase child = (connectionQuery.ReadChild ? repository.GetEntityDetials(typeof(Connection), connection.ChildEntityName, connection.ChildEntityID) : null);
                        ConnectionResult item = new ConnectionResult
                        {
                            Connection = (connectionQuery.ReadConnection ? connection : null),
                            Parent = parent,
                            Child = child
                        };
                        list.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION GetConnections :: entityName-> Connection ", ex);
            }

            return new ConnectionDataList
            {
                Results = list,
                TotalRecordCount = totalRecordCount
            };
        }

        private EntityAccessPermission GetEntityDefaultPermissionSettings(string entityName)
        {
            try
            {
                EntityDefaultPermissionSettings item = repo.GetItem((EntityDefaultPermissionSettings e) => e.EntityName.Equals(entityName));
                if (item == null)
                {
                    return new EntityAccessPermission();
                }

                EntityAccessPermission ecapAccessPermission = new EntityAccessPermission
                {
                    IdsAllowedToRead = item.IdsAllowedToRead.ToList(),
                    IdsAllowedToUpdate = item.IdsAllowedToUpdate.ToList(),
                    IdsAllowedToDelete = item.IdsAllowedToDelete.ToList(),
                    RolesAllowedToWrite = item.RolesAllowedToWrite.ToList(),
                    RolesAllowedToRead = item.RolesAllowedToRead.ToList(),
                    RolesAllowedToUpdate = item.RolesAllowedToUpdate.ToList(),
                    RolesAllowedToDelete = item.RolesAllowedToDelete.ToList()
                };
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] INFO GetEntityDefaultPermissionSettings :: entityName -> " + entityName + " " + LogHelpers.JsonToString("ecapAccessPermission", ecapAccessPermission));
                return ecapAccessPermission;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION GetEntityDefaultPermissionSettings :: entityName -> " + entityName + " ", ex);
            }

            return new EntityAccessPermission();
        }

        private EntityAccessPermission GetEntityPermissionById<T>(string entityItemId)
        {
            string name = typeof(T).Name;
            try
            {
                EntityBase entityDetials = repository.GetEntityDetials<T>(entityItemId);
                EntityAccessPermission ecapAccessPermission = new EntityAccessPermission
                {
                    IdsAllowedToRead = entityDetials.IdsAllowedToRead.ToList(),
                    IdsAllowedToUpdate = entityDetials.IdsAllowedToUpdate.ToList(),
                    IdsAllowedToDelete = entityDetials.IdsAllowedToDelete.ToList(),
                    RolesAllowedToWrite = entityDetials.RolesAllowedToWrite.ToList(),
                    RolesAllowedToRead = entityDetials.RolesAllowedToRead.ToList(),
                    RolesAllowedToUpdate = entityDetials.RolesAllowedToUpdate.ToList(),
                    RolesAllowedToDelete = entityDetials.RolesAllowedToDelete.ToList()
                };
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] INFO GetEntityPermissionById :: entityName -> " + name + " " + LogHelpers.JsonToString("ecapAccessPermission", ecapAccessPermission));
                return ecapAccessPermission;
            }
            catch (Exception ex)
            {
                ecapLogger.LogInformation("[Log Form Selise.SC.Ecap.MongoDb lib] EXCEPTION GetEntityPermissionById :: entityName -> " + name + " ", ex);
            }

            return new EntityAccessPermission();
        }

        private bool HasWriteAccess(string entityName)
        {
            IEnumerable<string> roles = securityDataProvider.GetSecurityContext().Roles;
            EntityAccessPermission entityDefaultPermissionSettings = GetEntityDefaultPermissionSettings(entityName);
            if (roles == null || roles.Count() <= 0)
            {
                return false;
            }

            if (entityDefaultPermissionSettings.RolesAllowedToWrite == null || entityDefaultPermissionSettings.RolesAllowedToWrite.Count <= 0)
            {
                return false;
            }

            using (IEnumerator<string> enumerator = roles.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    string role = enumerator.Current;
                    return entityDefaultPermissionSettings.RolesAllowedToWrite.Exists((string e) => e.Equals(role));
                }
            }

            return false;
        }

        private bool HasReadAccess<T>(string entityItemId)
        {
            string name = typeof(T).Name;
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            IEnumerable<string> roles = securityContext.Roles;
            EntityAccessPermission entityPermissionById = GetEntityPermissionById<T>(entityItemId);
            if (roles == null || roles.Count() <= 0)
            {
                return false;
            }

            if (entityPermissionById.RolesAllowedToRead != null && entityPermissionById.RolesAllowedToRead.Count > 0)
            {
                using IEnumerator<string> enumerator = roles.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    string role = enumerator.Current;
                    return entityPermissionById.RolesAllowedToWrite.Exists((string e) => e.Equals(role));
                }
            }

            if (entityPermissionById.IdsAllowedToRead != null && entityPermissionById.IdsAllowedToRead.Count > 0)
            {
                return entityPermissionById.IdsAllowedToRead.Exists((string id) => id.Equals(userId.ToString()));
            }

            return false;
        }

        private bool HasUpdateAccess<T>(string entityItemId)
        {
            string name = typeof(T).Name;
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            IEnumerable<string> roles = securityContext.Roles;
            EntityAccessPermission entityPermissionById = GetEntityPermissionById<T>(entityItemId);
            if (roles == null || roles.Count() <= 0)
            {
                return false;
            }

            if (entityPermissionById.RolesAllowedToUpdate != null && entityPermissionById.RolesAllowedToUpdate.Count > 0)
            {
                using IEnumerator<string> enumerator = roles.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    string role = enumerator.Current;
                    return entityPermissionById.RolesAllowedToUpdate.Exists((string e) => e.Equals(role));
                }
            }

            if (entityPermissionById.IdsAllowedToUpdate != null && entityPermissionById.IdsAllowedToUpdate.Count > 0)
            {
                return entityPermissionById.IdsAllowedToUpdate.Exists((string id) => id.Equals(userId.ToString()));
            }

            return false;
        }

        private bool HasDeleteAccess<T>(string entityItemId)
        {
            string name = typeof(T).Name;
            SecurityContext securityContext = securityDataProvider.GetSecurityContext();
            string userId = securityContext.UserId;
            IEnumerable<string> roles = securityContext.Roles;
            EntityAccessPermission entityPermissionById = GetEntityPermissionById<T>(entityItemId);
            if (roles == null || roles.Count() <= 0)
            {
                return false;
            }

            if (entityPermissionById.RolesAllowedToDelete != null && entityPermissionById.RolesAllowedToDelete.Count > 0)
            {
                using IEnumerator<string> enumerator = roles.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    string role = enumerator.Current;
                    return entityPermissionById.RolesAllowedToDelete.Exists((string e) => e.Equals(role));
                }
            }

            if (entityPermissionById.IdsAllowedToDelete != null && entityPermissionById.IdsAllowedToDelete.Count > 0)
            {
                return entityPermissionById.IdsAllowedToDelete.Exists((string id) => id.Equals(userId.ToString()));
            }

            return false;
        }
    }
}
