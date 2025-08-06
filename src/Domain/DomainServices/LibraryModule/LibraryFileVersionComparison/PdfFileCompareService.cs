using Aspose.Words;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using SharpCompress.Compressors.ADC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison
{
    public class PdfFileCompareService : ICompareFileVersionFromStreamService
    {
        private readonly ILogger<PdfFileCompareService> _logger;

        public PdfFileCompareService(ILogger<PdfFileCompareService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> CompareFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            return await GetLibraryVersionComparisonPdf(latestVersionFileStream, oldVersionFileStream);
        }
        private void SetAsposeLicense()
        {
            _logger.LogInformation("Going to SetAsposeLicense");

            var licPath = AppDomain.CurrentDomain.BaseDirectory + @"Lic/";
            try
            {
                var license = new License();
                license.SetLicense(licPath + "Aspose.Words.lic");
            }
            catch (Exception ex)
            {
                _logger.LogError($"DocGeneration Lic Path {licPath}, License Not Added!!!");
                _logger.LogError(
                    $"Exception message: {ex.Message} StackTrace: {ex.StackTrace}"
                );
            }
        }
        private async Task<byte[]> GetLibraryVersionComparisonPdf(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            return await Task.Run(() =>
            {
                SetAsposeLicense();
                using var outputStream = new MemoryStream();

                try
                {
                    var latestVersionPdf = new Aspose.Pdf.Document(latestVersionFileStream);
                    var oldVersionPdf = new Aspose.Pdf.Document(oldVersionFileStream);

                    string latestVersionDocxPath = "latestVersionPdf.docx";
                    string oldVersionDocxPath = "oldVersionPdf.docx";

                    latestVersionPdf.Save(latestVersionDocxPath, Aspose.Pdf.SaveFormat.DocX);
                    oldVersionPdf.Save(oldVersionDocxPath, Aspose.Pdf.SaveFormat.DocX);

                    var latestVersionDoc = new Document(latestVersionDocxPath);
                    var oldVersionDoc = new Document(oldVersionDocxPath);

                    CompareDocuments(latestVersionDoc, oldVersionDoc);

                    if (oldVersionDoc.Revisions.Count > 0)
                    {
                        HighlightRevisions(oldVersionDoc);
                        oldVersionDoc.AcceptAllRevisions();
                        oldVersionDoc.Save(outputStream, SaveFormat.Pdf);
                    }

                    return outputStream.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during PDF comparison processing.");
                    return null;
                }
            });
         
        }

    

        private void CompareDocuments(Document latestVersionDoc, Document oldVersionDoc)
        {
            var options = new CompareOptions
            {
                IgnoreFormatting = true,
                IgnoreFields = true,
                IgnoreComments = true,

            };

            oldVersionDoc.Compare(latestVersionDoc, "user", DateTime.Now, options);
        }

        private void HighlightRevisions(Document doc)
        {
            var comparisonDocument = doc;
            foreach (Revision revision in comparisonDocument.Revisions)
            {
                if (revision.RevisionType == RevisionType.Insertion)
                {
                    ApplyHighlightToRevision(revision, Color.Yellow);
                }

                else if (revision.RevisionType == RevisionType.Deletion)
                {

                    ApplyHighlightToRevision(revision, Color.Red);
                }

                else if (revision.RevisionType == RevisionType.FormatChange)
                {

                    ApplyHighlightToRevision(revision, Color.Green);
                }
            }
        }

        private void ApplyHighlightToRevision(Revision revision, Color highlightColor)
        {
            if (revision.ParentNode is Run runNode)
            {
                runNode.Font.HighlightColor = highlightColor;
            }

        }

        public Task<byte[]> CompareDeleteFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> CompareUpdateFileVersionFromStream(Stream latestVersionFileStream, Stream oldVersionFileStream)
        {
            throw new NotImplementedException();
        }
    }

}
