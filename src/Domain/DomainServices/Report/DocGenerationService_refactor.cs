using System;
using System.IO;
using System.Linq;
using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Saving;
using Aspose.Words.Settings;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report
{
    public class XDocGenerationService : IDocGenerationService
    {
        private readonly ILogger<DocGenerationService> _logger;

        public XDocGenerationService(ILogger<DocGenerationService> logger)
        {
            _logger = logger;
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
                _logger.LogError("DocGeneration Lic Path {LicPath}, License Not Added!!!", licPath);
                _logger.LogError("Exception message: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        public Document GenerateTableOfContent(Document document, string fontName, string reportTypeKey)
        {
            _logger.LogInformation("Calling GenerateTableOfContent");
            try
            {
                using (MemoryStream docStream = new MemoryStream())
                {
                    OoxmlSaveOptions options = new OoxmlSaveOptions(SaveFormat.Docx)
                    {
                        Compliance = OoxmlCompliance.Iso29500_2008_Strict
                    };
                    document.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2016);
                    document.Save(docStream, options);
                    docStream.Position = 0;

                    document = new Document(docStream);
                    foreach (Section section in document.Sections.OfType<Section>())
                        section.PageSetup.PaperSize = PaperSize.A4;
                    DocumentBuilder builder = new DocumentBuilder(document);
                    builder.PageSetup.PaperSize = PaperSize.A4;

                    builder.MoveToBookmark("bottom");
                    builder.InsertBreak(BreakType.LineBreak);

                    Style toc1 = document.Styles[StyleIdentifier.Toc1];
                    toc1.Font.Bold = true;
                    toc1.Font.Size = 11;
                    toc1.Font.Name = fontName;

                    Style toc2 = document.Styles[StyleIdentifier.Toc2];
                    toc2.Font.Bold = false;
                    toc2.Font.Size = 11;
                    toc2.Font.Name = fontName;

                    Style toc3 = document.Styles[StyleIdentifier.Toc3];
                    toc3.Font.Bold = false;
                    toc3.Font.Size = 11;
                    toc3.Font.Name = fontName;

                    builder.InsertTableOfContents(reportTypeKey == "multidisciplinary-audit-report"
                        ? "\\o \"1-2\" \\h \\z \\u"
                        : "\\o \"1-3\" \\h \\z \\u"
                    );

                    document.UpdatePageLayout();
                    document.UpdateFields();

                    docStream.Close();
                    docStream.Dispose();
                }

                _logger.LogInformation("Finished GenerateTableOfContent");
            }
            catch (Exception ex)
            {
                _logger.LogError("Generate Table Of Content: {Exception}", ex);
            }

            return document;
        }

        public byte[] PrepareDocumentFromHtmlStream(Stream contentStream)
        {
            SetAsposeLicense();

            try
            {
                // Create document and DocumentBuilder.
                var loadOptions = new LoadOptions
                {
                    LoadFormat = LoadFormat.Html
                };

                _logger.LogInformation("Going to setup page and contentStream length is: {ContentStreamLength}.", contentStream.Length);

                var document = new Document(contentStream, loadOptions);

                _logger.LogInformation("Document setup completed and Page Count is: {PageCount}", document.PageCount);

                foreach (var section in document.Sections.Cast<Section>())
                    section.PageSetup.PaperSize = PaperSize.A4;

                foreach (var image in document.GetChildNodes(NodeType.Shape, true).Cast<Shape>().Where(node => node.HasImage))
                {
                    image.Width = Math.Min(image.Width, 72 * 6); //image max width 6in
                }

                _logger.LogInformation("Page Setup completed ");
                try
                {
                    using var dstStream = new MemoryStream();
                    var options = new OoxmlSaveOptions(SaveFormat.Docx)
                    {
                        Compliance = OoxmlCompliance.Iso29500_2008_Strict
                    };
                    document.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2019);

                    _logger.LogInformation("Going to Save doc ");

                    document.Save(dstStream, options);

                    _logger.LogInformation("Doc Save Completed ");
                    dstStream.Position = 0;

                    var dstStreamBytes = dstStream.ToArray();

                    dstStream.Close();
                    dstStream.Dispose();

                    _logger.LogInformation("Finished DocGeneration ");
                    return dstStreamBytes;
                }
                catch (Exception e)
                {
                    _logger.LogError("Doc Generation failed");
                    _logger.LogError("Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
                    return new byte[] { };
                }
                finally
                {
                    GC.Collect();
                }
            }
            catch (Exception)
            {
                return new byte[] { };
            }
        }

        public byte[] PrepareHtmlFromObjectArtifactDocumentStream(Stream contentStream)
        {
            SetAsposeLicense();

            try
            {
                var doc = new Document(contentStream);

                var saveOptions = new HtmlSaveOptions
                {
                    ExportPageMargins = true,
                    ExportImagesAsBase64 = true,
                    ExportHeadersFootersMode = ExportHeadersFootersMode.FirstPageHeaderFooterPerSection,
                    ExportFontsAsBase64 = true,
                    Encoding = System.Text.Encoding.UTF8,
                    TableWidthOutputMode = HtmlElementSizeOutputMode.All,
                    PrettyFormat = true,
                    ExportDocumentProperties = true,
                    ExportTocPageNumbers = true,
                    ExportPageSetup = true
                };

                using MemoryStream htmlStream = new MemoryStream();
                doc.Save(htmlStream, saveOptions);
                var htmlStreamBytes = htmlStream.ToArray();
                htmlStream.Close();
                htmlStream.Dispose();
                return htmlStreamBytes;
            }
            catch (Exception e)
            {
                _logger.LogError("PrepareHtmlFromObjectArtifactDocumentStream -> html Generation failed");
                _logger.LogError("Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
                return new byte[] { };
            }
            finally
            {
                GC.Collect();
            }
        }

        public byte[] PrepareObjectArtifactDocumentFromHtmlStream(Stream contentStream)
        {
            SetAsposeLicense();

            try
            {
                // Create document and DocumentBuilder.
                var loadOptions = new LoadOptions
                {
                    LoadFormat = LoadFormat.Html
                };

                _logger.LogInformation("Going to setup page and contentStream length is: {ContentStreamLength}.", contentStream.Length);

                var document = new Document(contentStream, loadOptions);

                _logger.LogInformation("Document setup completed and Page Count is: {PageCount}", document.PageCount);
                foreach (var section in document.Sections.Cast<Section>())
                {
                    section.PageSetup.PaperSize = PaperSize.A4;
                    section.PageSetup.LeftMargin = 0;
                    section.PageSetup.RightMargin = 0;
                    section.PageSetup.TopMargin = 0;
                    section.PageSetup.BottomMargin = 0;
                    section.PageSetup.DifferentFirstPageHeaderFooter = true;
                }

                _logger.LogInformation("Page Setup completed ");
                try
                {
                    using var dstStream = new MemoryStream();
                    var options = new OoxmlSaveOptions(SaveFormat.Docx)
                    {
                        Compliance = OoxmlCompliance.Iso29500_2008_Strict
                    };
                    document.CompatibilityOptions.OptimizeFor(MsWordVersion.Word2019);

                    _logger.LogInformation("Going to Save doc ");

                    document.Save(dstStream, options);

                    _logger.LogInformation("Doc Save Completed ");
                    dstStream.Position = 0;

                    var dstStreamBytes = dstStream.ToArray();

                    dstStream.Close();
                    dstStream.Dispose();

                    _logger.LogInformation("Finished DocGeneration ");
                    return dstStreamBytes;
                }
                catch (Exception e)
                {
                    _logger.LogError("PrepareObjectArtifactDocumentFromHtmlStream -> Doc Generation failed");
                    _logger.LogError("Error message: {Message}. Full stacktrace: {StackTrace}", e.Message, e.StackTrace);
                    return new byte[] { };
                }
                finally
                {
                    GC.Collect();
                }
            }
            catch (Exception)
            {
                return new byte[] { };
            }
        }
    }
}