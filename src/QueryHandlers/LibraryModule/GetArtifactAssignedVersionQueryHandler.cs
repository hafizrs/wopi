using System;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers.LibraryModule;

public class GetArtifactAssignedVersionQueryHandler : IQueryHandler<GetArtifactAssignedVersionQuery, QueryHandlerResponse>
{
    private readonly ILogger<GetArtifactAssignedVersionQueryHandler> _logger;
    private readonly IDmsService _dmsService;
    private readonly IRepository _repository;
    private readonly IObjectArifactVersionService _objectArifactVersionService;
    public GetArtifactAssignedVersionQueryHandler(
        ILogger<GetArtifactAssignedVersionQueryHandler> logger, 
        IDmsService dmsService, 
        IRepository repository,
        IObjectArifactVersionService objectArifactVersionService)
    {
        _logger = logger;
        _dmsService = dmsService;
        _repository = repository;
        _objectArifactVersionService = objectArifactVersionService;
    }
    public QueryHandlerResponse Handle(GetArtifactAssignedVersionQuery query)
    {
        throw new System.NotImplementedException();
    }

    public async Task<QueryHandlerResponse> HandleAsync(GetArtifactAssignedVersionQuery query)
    {
        _logger.LogInformation("Entered into Handler: {HandlerName} with query: {Query}", 
            nameof(GetArtifactAssignedVersionQueryHandler), JsonConvert.SerializeObject(query));
        var response = new QueryHandlerResponse();
        try
        {
            string parentVersion = "1";
            string newVersion = string.Empty;

            if (string.IsNullOrEmpty(query.ParentObjectArtifactId))
            {
                newVersion = _objectArifactVersionService.GenerateParentVersionIfParentArtifactIsNullOrEmpty();
            }
            else
            {
                var artifact = await _repository.GetItemAsync<ObjectArtifact>(a => a.ItemId.Equals(query.ParentObjectArtifactId));

                var versionKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[nameof(ObjectArtifactMetaDataKeyEnum.VERSION)];

                if (artifact.MetaData.TryGetValue(versionKey, out var version))
                {
                    parentVersion = version.Value;
                }

                newVersion = _dmsService.GenerateVersionFromParentObjectArtifact(parentVersion);
            }

            response.Data = new
            {
                Version = newVersion
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Exception in Handler: {HandlerName} Error Message: {Message} Error Details: {StackTrace}",
                nameof(GetArtifactAssignedVersionQueryHandler), e.Message, e.StackTrace);
            response.ErrorMessage = $"Exception Occured: Message: {e.Message}";
            response.StatusCode = 1;
        }

        return response;
    }
}