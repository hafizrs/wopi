using Selise.Ecap.SC.Wopi.Contracts.EntityResponse;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Selise.Ecap.SC.Wopi.Contracts.Models;
using File = Selise.Ecap.Entities.PrimaryEntities.StorageService.File;
using System.Threading;

namespace Selise.Ecap.SC.Wopi.Contracts.DomainServices
{
    public interface IStorageDataService
    {
        void UpdateStorageBaseUrl(string _baseUrl);
        File GetFileInfo(string fileId, bool useImpersonation = false);
        Stream GetFileContentStream(string fileUrl);
        string GetResourceUrl(string entityName, string itemId, string tag, bool useImpersonation = false);
        Task<bool> UploadFileAsync(string fileId, string fileName, byte[] byteArray, string[] tags = null,
            Dictionary<string, MetaValue> metaData = null, string directoryId = "");
        Task<bool> ConvertFileByConversionPipeline(ConversionPipelinePayload payload);
        Task<GetPreSignedUrlForUploadResponse> GetPreSignedUrlForUploadQueryModel(
            PreSignedUrlForUploadQueryModel preSignedUrlForUploadQueryModel, bool useImpersonation = false);
        bool UploadFileToStorageByUrl(string uploadUrl, byte[] byteArray);
        Task<bool> UploadFileToStorageByUrlAsync(string uploadUrl, byte[] bytes, CancellationToken token = default);
        Task<GetPreSignedUrlForUploadResponse> GetPreSignedUrlForUploadQueryModel(
            PreSignedUrlForUploadQueryModel preSignedUrlForUploadQueryModel, string accessToken);
        Task<bool> UploadFileAsync(string accessToken, string fileId, string fileName, byte[] byteArray, string[] tags = null,
            Dictionary<string, MetaValue> metaData = null, string directoryId = "");
        Task<Stream> GetFileStream(File fileData, string token);
        Task<bool> DeleteFile(List<string> fileIds, string accessToken);
        Task<string> GetFileContentString(string fileUrl);
        Task<bool> UploadFileAsyncAsGzip(string fileId, string fileName, byte[] content, string contentType = null, string contentEncoding = null);
    }
}
