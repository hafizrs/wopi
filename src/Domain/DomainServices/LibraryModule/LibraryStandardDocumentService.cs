using System;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;

public class LibraryStandardDocumentService : ILibraryStandardDocumentService
{
    private readonly ILogger<LibraryStandardDocumentService> _logger;
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;

    public LibraryStandardDocumentService(ILogger<LibraryStandardDocumentService> logger, IRepository repository, ISecurityContextProvider securityContextProvider)
    {
        _logger = logger;
        _repository = repository;
        _securityContextProvider = securityContextProvider;
    }
    public async Task UpdateDocumentEditRecordForChildStandardFile()
    {
        throw new System.NotImplementedException();
    }

    public async Task<bool> UpdateDocumentEditRecordHistory(DocumentEditMappingRecord documentEditMappingRecord)
    {
        try
        {
            var securityContext = _securityContextProvider.GetSecurityContext();

            if (documentEditMappingRecord == null)
            {
                _logger.LogInformation("Document Edit Mapping Record is null");
                return false;
            }

            var editHistory = documentEditMappingRecord.EditHistory.Find(x => x.EditorUserId == securityContext.UserId);
            if (editHistory != null)
            {
                editHistory.EditDate = DateTime.UtcNow;
                await _repository.UpdateAsync(d => d.ItemId == documentEditMappingRecord.ItemId, documentEditMappingRecord);
            }
            else
            {
                var editHistoryData = new DocumentEditRecordHistory()
                {
                    EditorUserId = securityContext.UserId,
                    EditorDisplayName = securityContext.DisplayName,
                    EditDate = DateTime.UtcNow
                };

                if (documentEditMappingRecord.EditHistory == null)
                {
                    documentEditMappingRecord.EditHistory = new List<DocumentEditRecordHistory>()
                    {
                        editHistoryData
                    };
                }
                else
                {
                    documentEditMappingRecord.EditHistory.Add(editHistoryData);
                }

                await _repository.UpdateAsync(d => d.ItemId == documentEditMappingRecord.ItemId, documentEditMappingRecord);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred in {HandlerName} -> {MethodName}", nameof(LibraryStandardDocumentService), nameof(UpdateDocumentEditRecordHistory));
            _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            return false;
        }
    }
}