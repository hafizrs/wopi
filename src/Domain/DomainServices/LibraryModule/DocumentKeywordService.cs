
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule
{
    public class DocumentKeywordService : IDocumentKeywordService
    {
        private readonly IRepository _repository;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ILogger<DocumentKeywordService> _logger;

        public DocumentKeywordService(
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ILogger<DocumentKeywordService> logger)
        {
            _repository = repository;
            _securityContextProvider = securityContextProvider;
            _logger = logger;
        }

        public async Task UpdateObjectArtifactKeywords(string objectArtifactId)
        {
            try
            {
                ObjectArtifact objectArtifact = await GetObjectArtifactById(objectArtifactId);

                // Null Check for objectArtifact and MetaData
                var keywordsKey = LibraryModuleConstants.ObjectArtifactMetaDataKeys[ObjectArtifactMetaDataKeyEnum.KEYWORDS.ToString()];
                if (objectArtifact?.MetaData == null || !objectArtifact.MetaData.ContainsKey(keywordsKey))
                {
                    _logger.LogWarning($"MetaData or Keywords not found for ObjectArtifactId: {objectArtifactId}");
                    return;
                }

                string keywordsString = objectArtifact.MetaData[keywordsKey].Value;

                // Valid JSON Check
                if (string.IsNullOrWhiteSpace(keywordsString))
                {
                    _logger.LogWarning($"Keywords string is null or whitespace for ObjectArtifactId: {objectArtifactId}");
                    return;
                }

                string[] keywords = JsonSerializer.Deserialize<string[]>(keywordsString);

                // Null Check for keywords
                if (keywords == null || keywords.Length == 0)
                {
                    _logger.LogWarning($"No valid keywords found for ObjectArtifactId: {objectArtifactId}");
                    return;
                }

                var organisationKeywords = await _repository.GetItemAsync<RiqsKeyword>(kw => kw.OrganizationId == objectArtifact.OrganizationId);
                if (organisationKeywords != null)
                {
                    organisationKeywords.Values = organisationKeywords.Values.Union(keywords).ToArray();
                    await _repository.UpdateAsync<RiqsKeyword>(kw => kw.ItemId == organisationKeywords.ItemId, organisationKeywords);
                }
                else
                {
                    await SaveKeyword(keywords, objectArtifact.OrganizationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while updating ObjectArtifact keywords for ObjectArtifactId: {objectArtifactId}. Exception Details: {ex}");
            }
        }

        public async Task UpdateKeywords(string[] keywords, string objectArtifactId)
        {
            ObjectArtifact objectArtifact = await GetObjectArtifactById(objectArtifactId);
            if (objectArtifact == null)
            {
                return;
            }
            var organisationKeywords = await _repository.GetItemAsync<RiqsKeyword>(kw => kw.OrganizationId == objectArtifact.OrganizationId);
            if (organisationKeywords != null)
            {
                organisationKeywords.Values = organisationKeywords.Values.Union(keywords).ToArray();
                await _repository.UpdateAsync<RiqsKeyword>(kw => kw.ItemId == organisationKeywords.ItemId, organisationKeywords);
            }
        }

        public async Task<string[]> GetKeywordValues(string organisationId)
        {
            var organisationKeywords = await _repository.GetItemAsync<RiqsKeyword>(kw => kw.OrganizationId == organisationId);
            if(organisationKeywords == null)
            {
                return new string[] { };
            }
            return organisationKeywords.Values;
        }

        private async Task SaveKeyword(string[] keywords, string organisationId)
        {
            var securityContext = _securityContextProvider.GetSecurityContext();
            var newKeyword = new RiqsKeyword
            {
                ItemId = Guid.NewGuid().ToString(),
                CreateDate = DateTime.UtcNow.ToLocalTime(),
                LastUpdateDate = DateTime.UtcNow.ToLocalTime(),
                CreatedBy = securityContext.UserId,
                TenantId = securityContext.TenantId,
                Language = securityContext.Language,
                OrganizationId = organisationId,
                Values = keywords,
                ModuleName = "Library",
                KeyName = "Keyword"
            };
            await _repository.SaveAsync<RiqsKeyword>(newKeyword);
        }

        private async Task<ObjectArtifact> GetObjectArtifactById(string id)
        {
            return await _repository.GetItemAsync<ObjectArtifact>(o => o.ItemId == id);
        }
    }
}
