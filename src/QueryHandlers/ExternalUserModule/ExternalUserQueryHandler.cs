using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ExternalUser;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using System.Security.Cryptography;
using System.Collections.Generic;

using Selise.Ecap.Entities.PrimaryEntities.Security;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ExternalUserModule;

public class ExternalUserQueryHandler : AbstractQueryHandler<ExternalUserQuery>
{
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly ILogger<ExternalUserQueryHandler> _logger;
    private readonly ITokenService _tokenService;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    private readonly IRepository _repository;
    public ExternalUserQueryHandler(
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        ILogger<ExternalUserQueryHandler> logger,
        ISecurityContextProvider securityContextProvider,
        ITokenService tokenService,
          IRepository repository
        )
    {
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _logger = logger;
        _tokenService = tokenService;
        _securityContextProvider = securityContextProvider;
        _repository = repository;
    }

    public override async Task<QueryHandlerResponse> HandleAsync(ExternalUserQuery query)
    {
        QueryHandlerResponse response = new QueryHandlerResponse();

        _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
            nameof(ExternalTokenQueryHandler), JsonConvert.SerializeObject(query));

        try
        {
            var externalUser = await GetExternlUser(query.ExternalUserItemId);

            if (externalUser == null) return CreateErrorRespose("External user not found");
            
            var praxisClient = await _repository.GetItemAsync<PraxisClient>(pc => pc.ItemId.Equals(externalUser.PraxisClientId));
            
            if(praxisClient==null)  return CreateErrorRespose("No Client Found");
            var clientList = new List<PraxisClientInfo>();
            var clientInfo = new PraxisClientInfo
            {
                ClientId = praxisClient.ItemId,
                ClientName = praxisClient.ClientName,
                IsPrimaryDepartment = true,
                ParentOrganizationId = praxisClient.ParentOrganizationId,
                ParentOrganizationName = praxisClient.ParentOrganizationName,
                IsCreateProcessGuideEnabled = false,
                Roles = externalUser.Roles
            };
            clientList.Add(clientInfo);
            var result = new PraxisUser()
            {
                ClientId = praxisClient.ItemId,
                ClientList = clientList,
                ClientName = praxisClient.ClientName,
                Roles= externalUser.Roles,
                UserId =null,
                ItemId = externalUser.ItemId
            };
            
            response.Data = result;
            response.StatusCode = 0;

        }
        catch (Exception ex)
        {
            _logger.LogError("Exception in {HandlerName}. Exception Message: {Message}. Exception Details: {StackTrace}",
                nameof(ExternalTokenQueryHandler), ex.Message, ex.StackTrace);
            response.StatusCode = 1;
            response.ErrorMessage = ex.Message;
        }
        return response;
    }
    private QueryHandlerResponse CreateErrorRespose(string message)
    {
        QueryHandlerResponse response = new QueryHandlerResponse();
        response.ErrorMessage = message;
        response.StatusCode = 1;
        return response;
    }

    private Task<ExternalUser> GetExternlUser(string externalUserItemId)
    {
        var dataContext = _ecapMongoDbDataContextProvider.GetTenantDataContext(_securityContextProvider.GetSecurityContext().TenantId);

        var clientCredentialsCollection = dataContext?.GetCollection<ExternalUser>("ExternalUsers");

        return clientCredentialsCollection?.Find(cc => cc.ItemId == externalUserItemId).SingleOrDefaultAsync();
    }
   

}
