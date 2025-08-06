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

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.ExternalUserModule;

public class ExternalTokenQueryHandler : AbstractQueryHandler<ExternalTokenQuery>
{
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly ILogger<ExternalTokenQueryHandler> _logger;
    private readonly ITokenService _tokenService;
    private readonly IBlocksMongoDbDataContextProvider _ecapMongoDbDataContextProvider;
    public ExternalTokenQueryHandler(
        IBlocksMongoDbDataContextProvider ecapMongoDbDataContextProvider,
        ILogger<ExternalTokenQueryHandler> logger,
        ISecurityContextProvider securityContextProvider,
        ITokenService tokenService)
    {
        _ecapMongoDbDataContextProvider = ecapMongoDbDataContextProvider;
        _logger = logger;
        _tokenService = tokenService;
        _securityContextProvider = securityContextProvider;
    }

    public override async Task<QueryHandlerResponse> HandleAsync(ExternalTokenQuery query)
    {
        QueryHandlerResponse response = new QueryHandlerResponse();

        _logger.LogInformation("Enter {HandlerName} with query: {QueryName}",
            nameof(ExternalTokenQueryHandler), JsonConvert.SerializeObject(query));

        try
        {
            var externalUser = await GetExternlUser(query.ExternalUserItemId);

            if (externalUser == null) return CreateErrorRespose("External user not found");
            var result = await _tokenService.GetExternalToken(
               externalUser.ClientSecretId,
               externalUser.ClientSecret
                );
            
            if(result ==null ) return CreateErrorRespose("You do not have sufficient permission to read data.");

            var responseData = new ExternalUserTokenResponse()
            {
                refresh_token = result.refresh_token,
                access_token = result.access_token,
                scope=result.scope,
                expires_in= result.expires_in,
                ip_address=result.ip_address,
                token_type=result.token_type
            };
                

            response.Data = responseData;
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
