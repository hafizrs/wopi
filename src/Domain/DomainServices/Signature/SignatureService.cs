using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.Signature;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Signature;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Utils;
using File = Selise.Ecap.Entities.PrimaryEntities.StorageService.File;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Signature
{
    public class SignatureService : ISignatureService
    {
        private readonly ILogger<SignatureService> _logger;
        private readonly IServiceClient _serviceClient;
        private readonly ITokenService _tokenService;
        private readonly StorageDataService _blocksSorageService;
        private readonly StorageDataService _storageService;
        private readonly PraxisFileService _blocksFileService;
        private readonly PraxisFileService _praxisFileService;
        private readonly IConfiguration _configuration;
        public SignatureService(
            ILogger<SignatureService> logger,
            IConfiguration configuration,
            IServiceClient serviceClient,
            ITokenService tokenService,
            StorageServiceFactory storageServiceFactory,
            PraxisFileServiceFactory praxisFileServiceFactory
        )
        {
            _logger = logger;
            _serviceClient = serviceClient;
            _tokenService = tokenService;

            _configuration = configuration;
            //  _eSignBaseUrl = _configuration["EsignBaseUrl"];
            //   _eSignVersion = _configuration["EsignVersion"];
            // _eSignOrigin = _configuration["EsignWebUrl"];
            // _eSignClientId = _configuration["EsignClientId"];
            // _eSignClientSecret = _configuration["EsignClientSecret"];
            _blocksSorageService = storageServiceFactory.Create(true);
            _storageService = storageServiceFactory.Create(false);
            _blocksFileService = praxisFileServiceFactory.Create(true);
            _praxisFileService = praxisFileServiceFactory.Create(false);
        }

        public async Task<bool> CreateSignatureRequest(SignatureRequestCommand command)
        {
            _logger.LogInformation("Enter CreateSignatureRequest with payload: {Payload}",
                JsonConvert.SerializeObject(command));

            var file = await _praxisFileService.GetFileInfoFromStorage(command.FileIds[0]);
            if (file == null)
            {
                _logger.LogWarning("File not found with id {Id}", command.FileIds[0]);
                return false;
            }

            var fileStream = _storageService.GetFileContentStream(file.Url);
            if (fileStream == null)
            {
                _logger.LogWarning("File stream not found with url id {Id}", file.Url);
                return false;
            }

            var bytes = ((MemoryStream)fileStream).ToArray();
            fileStream.Close();
            await fileStream.DisposeAsync();

            var accessToken = await GetExternalAccessToke();
            _logger.LogInformation("External Access token retrieved for signature request: {AccessToken}", accessToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning("External access token is null or empty. Can't Create Signature Request.");
                return false;
            }

            //upload this file to current token user tenant
            var fileUploaded = await UploadFileToStorage(file, bytes, accessToken, _blocksSorageService);

            if (!fileUploaded)
            {
                _logger.LogWarning("File upload failed for file with id {Id} to external token holder's tanent", command.FileIds[0]);
                return false;
            }

            try
            {
                var httpResponse = await _serviceClient.SendToHttpAsync<CommandResponse>(
                    HttpMethod.Post,
                    _configuration["EsignBaseUrl"],
                    _configuration["EsignVersion"],
                    "Selisign/ExternalApp/PrepareContractInternal",
                    command,
                    accessToken
                );

                if (httpResponse is { StatusCode: 1 })
                {
                    _logger.LogWarning("CreateSignatureRequest failed with error -> {Error}",
                        httpResponse.ErrorMessages);
                }
                else
                {
                    _logger.LogInformation("CreateSignatureRequest success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured in ESignService in CreateSignatureRequest with error -> {ExMessage} trace -> {ExStackTrace}",
                    ex.Message, ex.StackTrace);
            }

            return false;
        }

        public async Task<bool> StoreSignedFileToLocal(
            string localFileStorageId,
            string signedFileId
        )
        {
            var accessToken = await GetExternalAccessToke();
            if (string.IsNullOrWhiteSpace(accessToken))
                return false;
            var file = await _blocksFileService.GetFileInfoFromStorage(signedFileId, accessToken);
            if (file == null)
            {
                _logger.LogInformation("File not found with id {Id}", signedFileId);
                return false;
            }


            var fileStream = await _blocksSorageService.GetFileStream(file, accessToken);
            if (fileStream == null)
            {
                _logger.LogInformation("File stream found with url id {Url}", file.Url);
                return false;
            }

            var bytes = ((MemoryStream)fileStream).ToArray();
            fileStream.Close();
            fileStream.Dispose();
            //token
            var adminToken = await _tokenService.GetAdminToken();

            //update signature fileId with local fileId
            file.ItemId = localFileStorageId;
            //delete file before upload
            await _praxisFileService.DeleteFilesFromStorage(new List<string>()
            {
                localFileStorageId
            }, adminToken);

            var fileUploaded = await UploadFileToStorage(file, bytes, adminToken, _storageService);

            return fileUploaded;
        }

        private async Task<bool> UploadFileToStorage(
            File file,
            byte[] bytes,
            string accessToken,
            StorageDataService storageService
        )
        {
            try
            {

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return await storageService.UploadFileAsync(file.ItemId, file.Name,
                        bytes, null, null, null);
                }

                return await storageService.UploadFileAsync(accessToken, file.ItemId, file.Name,
                    bytes, null, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {Name} in UploadFileToStorage with error -> {Message} trace -> {StackTrace}", GetType().Name,
                    ex.Message, ex.StackTrace);
            }

            return false;
        }

        private async Task<string> GetExternalAccessToke()
        {
            return await _tokenService.GetExternalToken(_configuration["EsignClientId"], _configuration["EsignClientSecret"], _configuration["EsignWebUrl"]);
        }
    }
}
