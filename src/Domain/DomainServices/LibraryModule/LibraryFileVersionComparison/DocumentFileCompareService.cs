using Aspose.Pdf.Facades;
using Aspose.Words;
using Aspose.Words.Saving;
using Aspose.Words.Settings;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison
{

    public class DocumentFileCompareService : ICompareFileVersionFromStreamService
    {
        private readonly ILogger<DocumentFileCompareService> _logger;

        public DocumentFileCompareService(ILogger<DocumentFileCompareService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> CompareFileVersionFromStream(Stream newVersionFileStream, Stream parentVersionFileStream)
        {
            return await GetLibraryVersionComparisonDoc(newVersionFileStream, parentVersionFileStream, ComparisonType.Standard);
        }

        public async Task<byte[]> CompareDeleteFileVersionFromStream(Stream newVersionFileStream, Stream parentVersionFileStream)
        {
            return await GetDeletionHighlightedOldFile(parentVersionFileStream, newVersionFileStream);
        }

        public async Task<byte[]> CompareUpdateFileVersionFromStream(Stream newVersionFileStream, Stream parentVersionFileStream)
        {
            return await GetUpdateHighlightedNewFile(newVersionFileStream, parentVersionFileStream);
        }

        private void SetAsposeLicense()
        {
            _logger.LogInformation("Setting Aspose License...");
            var licPath = AppDomain.CurrentDomain.BaseDirectory + @"Lic/";
            try
            {
                var license = new License();
                license.SetLicense(licPath + "Aspose.Words.lic");
            }
            catch (Exception ex)
            {
                _logger.LogError($"License not set! Path: {licPath}. Error: {ex.Message}");
            }
        }

        private async Task<byte[]> GetLibraryVersionComparisonDoc(Stream baseFileStream, Stream comparisonFileStream, ComparisonType comparisonType)
        {
            return await Task.Run(() =>
            {
                SetAsposeLicense();
                var author = "User";

                using var outputStream = new MemoryStream();
                Document baseDoc = new Document(baseFileStream);
                Document comparisonDoc = new Document(comparisonFileStream);

                baseDoc.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2019);

                var compareOptions = new CompareOptions
                {
                    IgnoreFormatting = true,
                    IgnoreComments = true,
                    Target = ComparisonTargetType.New
                };

                baseDoc.Compare(comparisonDoc, author, DateTime.Now, compareOptions);
                ProcessRevisionsBasedOnComparisonType(baseDoc, comparisonType);

                baseDoc.AcceptAllRevisions();
                baseDoc.Save(outputStream, SaveFormat.Docx);
                return outputStream.ToArray();
            });
        }

        private void ProcessRevisionsBasedOnComparisonType(Document doc, ComparisonType comparisonType)
        {
            _logger.LogInformation("Processing {RevisionsCount} revisions for {ComparisonType}", doc.Revisions.Count, comparisonType);

            if (comparisonType == ComparisonType.Standard)
            {
                ProcessRevisionsWithCustomFormatting(doc);
            }
        }

        private void ProcessRevisionsWithCustomFormatting(Document doc)
        {
            foreach (Revision revision in doc.Revisions)
            {
                if (revision.ParentNode is Run run)
                {
                    switch (revision.RevisionType)
                    {
                        case RevisionType.Insertion:
                            run.Font.Color = Color.Green;
                            run.Font.HighlightColor = Color.LightGreen;
                            break;

                        case RevisionType.Deletion:
                            run.Font.Color = Color.Red;
                            run.Font.StrikeThrough = true;
                            run.Font.HighlightColor = Color.Pink;
                            break;

                        case RevisionType.FormatChange:
                            run.Font.Color = Color.Blue;
                            run.Font.HighlightColor = Color.LightBlue;
                            break;
                    }
                }
            }
        }

        private async Task<byte[]> GetDeletionHighlightedOldFile(Stream oldFileStream, Stream newFileStream)
        {
            return await Task.Run(() =>
            {
                SetAsposeLicense();
                var author = "User";

                using var outputStream = new MemoryStream();
                var oldDoc = new Document(oldFileStream);
                var newDoc = new Document(newFileStream);

                oldDoc.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2019);

                var compareOptions = new CompareOptions
                {
                    IgnoreFormatting = true,
                    IgnoreComments = true,
                    Target = ComparisonTargetType.New
                };

                oldDoc.Compare(newDoc, author, DateTime.Now, compareOptions);
                ApplyDeletionHighlightsToOriginal(oldDoc, newDoc);

                oldDoc.AcceptAllRevisions();
                oldDoc.Save(outputStream, SaveFormat.Docx);
                return outputStream.ToArray();
            });
        }

        private async Task<byte[]> GetUpdateHighlightedNewFile(Stream newFileStream, Stream oldFileStream)
        {
            return await Task.Run(() =>
            {
                SetAsposeLicense();
                var author = "User";

                using var outputStream = new MemoryStream();
                var newDoc = new Document(newFileStream);
                var oldDoc = new Document(oldFileStream);

                newDoc.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2019);

                var compareOptions = new CompareOptions
                {
                    IgnoreFormatting = true,
                    IgnoreComments = true,
                    Target = ComparisonTargetType.New
                };

                newDoc.Compare(oldDoc, author, DateTime.Now, compareOptions);
                ApplyUpdateHighlightsToNewDocument(newDoc);

                newDoc.AcceptAllRevisions();
                newDoc.Save(outputStream, SaveFormat.Docx);
                return outputStream.ToArray();
            });
        }

        private void ApplyDeletionHighlightsToOriginal(Document originalDoc, Document comparedDoc)
        {
            foreach (Revision revision in comparedDoc.Revisions)
            {
                if (revision.RevisionType == RevisionType.Deletion && revision.ParentNode is Run run)
                {
                    var paragraph = (Paragraph)run.GetAncestor(NodeType.Paragraph);
                    int index = GetParagraphIndex(comparedDoc, paragraph);

                    HighlightTextInOriginalDocument(originalDoc, run.Text, index);
                }
            }
        }

        private void ApplyUpdateHighlightsToNewDocument(Document newDoc)
        {
            foreach (Revision revision in newDoc.Revisions)
            {
                if (revision.RevisionType == RevisionType.Insertion || revision.RevisionType == RevisionType.FormatChange)
                {
                    if (revision.ParentNode is Run run)
                    {
                        run.Font.Color = Color.Blue;
                        run.Font.HighlightColor = Color.LightYellow;
                    }
                }
            }
        }

        private void HighlightTextInOriginalDocument(Document doc, string textToHighlight, int paragraphIndex)
        {
            NodeCollection paragraphs = doc.GetChildNodes(NodeType.Paragraph, true);
            if (paragraphIndex < 0 || paragraphIndex >= paragraphs.Count) return;

            Paragraph para = (Paragraph)paragraphs[paragraphIndex];
            foreach (Run run in para.GetChildNodes(NodeType.Run, true))
            {
                if (run.Text.Contains(textToHighlight.Trim()))
                {
                    run.Font.Color = Color.Red;
                    run.Font.StrikeThrough = true;
                    run.Font.HighlightColor = Color.Pink;
                    break;
                }
            }
        }

        private int GetParagraphIndex(Document document, Paragraph paragraph)
        {
            NodeCollection paragraphs = document.GetChildNodes(NodeType.Paragraph, true);
            for (int i = 0; i < paragraphs.Count; i++)
            {
                if (paragraphs[i] == paragraph) return i;
            }
            return -1;
        }

        private class DeletionInfo
        {
            public string Text { get; set; }
            public int ParagraphIndex { get; set; }
        }

        private class UpdateInfo
        {
            public string Text { get; set; }
            public int ParagraphIndex { get; set; }
            public RevisionType RevisionType { get; set; }
        }

        private enum ComparisonType
        {
            Standard
        }
    }


}
