using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.QueryHandlers
{
    public class GetFileQueryHandler : IQueryHandler<GetFileQuery, QueryHandlerResponse>
    {
        private readonly ISSOFileInfoService _ssoFileInfoService;

        public GetFileQueryHandler(ISSOFileInfoService ssoFileInfoService)
        {
            _ssoFileInfoService = ssoFileInfoService;
        }

        public QueryHandlerResponse Handle(GetFileQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task<QueryHandlerResponse> HandleAsync(GetFileQuery query)
        {
            
            if (string.IsNullOrWhiteSpace(query.SharePointSite))
                throw new ArgumentException("SharePoint site must not be empty.", nameof(query.SharePointSite));
            if (string.IsNullOrWhiteSpace(query.FilePath))
                throw new ArgumentException("File path must not be empty.", nameof(query.FilePath));

            try
            {
                
                var fileInfo = await _ssoFileInfoService.GetSSOFileInfo(query.SharePointSite, query.FilePath);

                if (string.IsNullOrEmpty(fileInfo))
                {
                    return new QueryHandlerResponse
                    {
                        StatusCode = 404,
                        ErrorMessage = "File not found or no information available.",
                        Data = null,
                        Results = null,
                        TotalCount = 0
                    };
                }

                
                return new QueryHandlerResponse
                {
                    StatusCode = 200,
                    ErrorMessage = null,
                    Data = fileInfo,
                    Results = fileInfo, 
                    TotalCount = 1 
                };
            }
            catch (Exception ex)
            {
                
                return new QueryHandlerResponse
                {
                    StatusCode = 500,
                    ErrorMessage = $"An error occurred: {ex.Message}",
                    Data = null,
                    Results = null,
                    TotalCount = 0
                };
            }
        }

    }
}
