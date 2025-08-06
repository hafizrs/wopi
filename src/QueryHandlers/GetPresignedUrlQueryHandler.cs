using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetPresignedUrlQueryHandler : IQueryHandler<GetPresignedUrlQuery, QueryHandlerResponse>
    {
        private readonly IStorageDataService _storageDataService;
        private readonly ILogger<GetPresignedUrlQueryHandler> _logger;

        public GetPresignedUrlQueryHandler(
            ILogger<GetPresignedUrlQueryHandler> logger,
            IStorageDataService storageDataService
        )
        {
            _logger = logger;
            _storageDataService = storageDataService;
        }

        public QueryHandlerResponse Handle(GetPresignedUrlQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetPresignedUrlQuery query)
        {
            _logger.LogInformation("Enter {HandlerName} with query: {Query}.", nameof(GetPresignedUrlQueryHandler),
                JsonConvert.SerializeObject(query));
            var response = new QueryHandlerResponse();
            try
            {
                var getPreSignedUrlForUploadResponseList = new List<GetPreSignedUrlForUploadResponse>();
                if (query.FileInfoList.Count > 0)
                {
                    foreach (var fileInfo in query.FileInfoList)
                    {
                        var metaData = new Dictionary<string, MetaValue>
                        {
                            { "Title", new MetaValue { Type = "String", Value = fileInfo.FileName } },
                            { "OriginalName", new MetaValue { Type = "String", Value = fileInfo.FileName } }
                        };

                        var metaDataObj = JsonConvert.SerializeObject(metaData);

                        var fileTagsJson = JsonConvert.SerializeObject(new string[] { "File" });

                        var preSignedUrlForUploadQueryModel = new PreSignedUrlForUploadQueryModel
                        {
                            ItemId = fileInfo.FileId,
                            Name = fileInfo.FileName,
                            Tags = fileTagsJson,
                            MetaData = metaDataObj,
                            ParentDirectoryId = ""
                        };
                        var getPresignedUrlResponse = await _storageDataService.GetPreSignedUrlForUploadQueryModel(
                            preSignedUrlForUploadQueryModel, true
                        );
                        getPreSignedUrlForUploadResponseList.Add(getPresignedUrlResponse);
                        _logger.LogInformation("file id -> {FileId}: GetPreSignedUrlResponse -> {Response}",
                            fileInfo.FileId, JsonConvert.SerializeObject(getPresignedUrlResponse));
                    }

                    if (getPreSignedUrlForUploadResponseList.Count > 0)
                    {
                        response.Results =
                            getPreSignedUrlForUploadResponseList.Select(x => new { x.FileId, x.UploadUrl });
                        response.StatusCode = 0;
                        response.ErrorMessage = string.Empty;
                        response.TotalCount = getPreSignedUrlForUploadResponseList.Count;
                        return response;
                    }
                }

                response.ErrorMessage = "No file info list found";
                response.StatusCode = 1;
                response.TotalCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {HandlerName} Error Message: {Message} .Error Details: {StackTrace}.",
                    nameof(GetPresignedUrlQueryHandler), ex.Message, ex.StackTrace);
                response.ErrorMessage = "no presigned url found";
                response.StatusCode = 1;
                response.TotalCount = 0;
                return response;
            }

            _logger.LogInformation("Handled By {HandlerName} with response: {Response}.",
                nameof(GetPresignedUrlQueryHandler), JsonConvert.SerializeObject(response));

            return response;
        }
    }
}