using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryFileVersionComparison;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryFileVersionComparison
{
    public class CompareLibraryFileVersionFactoryService : ICompareLibraryFileVersionFactoryService
    {
        private readonly ExcelFileCompareService _excelFileCompareService;
        private readonly DocumentFileCompareService _documentFileCompareService;
        private readonly PdfFileCompareService _pdfFileCompareService;

        public CompareLibraryFileVersionFactoryService(
            ExcelFileCompareService excelFileCompareService,
            DocumentFileCompareService documentFileCompareService,
            PdfFileCompareService pdfFileCompareService)
        {
            _excelFileCompareService = excelFileCompareService;
            _documentFileCompareService = documentFileCompareService;
            _pdfFileCompareService = pdfFileCompareService;
        }

        public ICompareFileVersionFromStreamService GetFileCompareService(LibraryFileTypeEnum fileType)
        {
            return fileType switch
            {
                LibraryFileTypeEnum.EXCELS => _excelFileCompareService,
                LibraryFileTypeEnum.DOCUMENT => _documentFileCompareService,
                LibraryFileTypeEnum.PDF => _pdfFileCompareService,
                _ => null 
            };
        }
    }


}
