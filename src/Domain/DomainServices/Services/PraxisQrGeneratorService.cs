using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using ZXing;
using ZXing.QrCode;
using SkiaSharp;
using System.Linq;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisQrGeneratorService : IPraxisQrGeneratorService
    {
        private readonly IStorageDataService storageDataService;
        private readonly ILogger<PraxisQrGeneratorService> logger;
        private readonly IRepository repository;
        private readonly INotificationService _notificationService;
        public PraxisQrGeneratorService(
            IStorageDataService storageDataService,
            ILogger<PraxisQrGeneratorService> log,
            IRepository repo,
            INotificationService notificationService
            )
        {
            this.storageDataService = storageDataService;
            this.logger = log;
            this.repository = repo;
            this._notificationService = notificationService;
        }

        public async Task<string> QRCodeGenerateAsync(PraxisEquipment equipmentForQrCode, string qrCodeContent, int height = 100, int width = 100, int margin = 0)
        {
            string fileId = string.Empty;
            try
            {
                Bitmap qrCodeImage = CreateQRCode(qrCodeContent, width, height, margin);
                var textToBeAdded = GetEquipmentAdditionalInfo(equipmentForQrCode);
                byte[] imageBytes;
                if (textToBeAdded != null && textToBeAdded.Count > 0)
                {
                    imageBytes = AddTextToQrCode(qrCodeImage, textToBeAdded);
                }
                else
                {
                    imageBytes = ImageToBytes(qrCodeImage);
                }
                fileId = await UploadQrCodeAsync(imageBytes);
                if (!string.IsNullOrEmpty(fileId))
                {
                    equipmentForQrCode.EquipmentQrFileId = fileId;
                    repository.Update<PraxisEquipment>(e => e.ItemId == equipmentForQrCode.ItemId, equipmentForQrCode);
                    await _notificationService.QrCodeGenerateNotification(true, equipmentForQrCode.ItemId, equipmentForQrCode.EquipmentQrFileId);
                    logger.LogInformation("Successfully generated QR code for the equipment with id: {ItemId}", equipmentForQrCode.ItemId);
                }
                else
                {
                    logger.LogError("Error occurred while generating equipment QR code for the equipment with id: {ItemId}.", equipmentForQrCode.ItemId);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "QRCodeGenerate exception. Message: {Message}", exception.Message);
            }
            return fileId;
        }

        private Bitmap CreateQRCode(string qrCodeContent, int width, int height, int margin)
        {
            var qrCodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions { Height = height, Width = width, Margin = margin }
            };

            var pixelData = qrCodeWriter.Write(qrCodeContent);

            // creating a bitmap from the raw pixel data; if only black and white colors are used it makes no difference
            // that the pixel data ist BGRA oriented and the bitmap is initialized with RGB
            var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            try
            {
                // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            return bitmap;
        }

        private byte[] AddTextToQrCode(Bitmap qrCodeImage, List<string> textLines)
        {
            try
            {
                var lineHeight = 40;
                var padding = 40;
                var borderPadding = 150;
                var textAreaHeight = (lineHeight * textLines.Count) + (padding * 2);

                var qrCodeWidth = qrCodeImage.Width;
                var qrCodeHeight = qrCodeImage.Height;
                
                var totalWidth = qrCodeWidth + (borderPadding * 2);
                var totalHeight = qrCodeHeight + textAreaHeight + (borderPadding * 2);

                byte[] qrCodeBytes = ImageToBytes(qrCodeImage);

                using var finalBitmap = new SKBitmap(totalWidth, totalHeight);
                using var canvas = new SKCanvas(finalBitmap);

                canvas.Clear(SKColors.White);

                using var borderPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsStroke = true,
                    StrokeWidth = 1
                };

                canvas.DrawRect(0, 0, totalWidth, totalHeight, borderPaint);

                using var qrData = SKData.CreateCopy(qrCodeBytes);
                using var qrImage = SKImage.FromEncodedData(qrData);

                canvas.DrawImage(qrImage, borderPadding, borderPadding);

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 14,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                };

                var xCenter = totalWidth / 2f;
                var yPosition = qrCodeHeight + padding + paint.TextSize + borderPadding;

                for (int i = 0; i < textLines.Count; i++)
                {
                    var text = textLines[i];
                    canvas.DrawText(text, xCenter, yPosition, paint);
                    yPosition += lineHeight;
                }

                using var image = SKImage.FromBitmap(finalBitmap);
                using var imageData = image.Encode(SKEncodedImageFormat.Png, 100);

                return imageData.ToArray();
            }
            catch (Exception exception)
            {
                logger.LogError("Error adding text to QR code. Message: {Message}. StackTrace: {StackTrace}.", exception.Message, exception.StackTrace);
                return null;
            }
        }

        private byte[] ImageToBytes(Image image)
        {
            try
            {
                var memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Png);
                byte[] byteImage = memoryStream.ToArray();
                memoryStream.Close();
                memoryStream.Dispose();
                return byteImage;
            }
            catch (ArgumentNullException exception)
            {
                logger.LogError("Error converting image to byte[] in QR code. Message: {Message}. StackTrace: {StackTrace}.", exception.Message, exception.StackTrace);
                return new byte[] {};
            }
        }

        private async Task<string> UploadQrCodeAsync(byte[] file)
        {
            string fileId = (Guid.NewGuid()).ToString();
            try
            {
                string fileName = fileId + ".png";
                string[] tags = new string[] { "File" };
                Dictionary<string, MetaValue> metaData = null;
                if (file != null && file.Length > 0)
                {
                    var isSuccess = await storageDataService.UploadFileAsync(fileId, fileName, file, tags, metaData);
                    return isSuccess ? fileId : "";
                }
                return "";
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error uploading QR code file. Message: {Message}", exception.Message);
                return "";
            }
        }

        private List<string> GetEquipmentAdditionalInfo(PraxisEquipment equipment)
        {
            var additionalInfo = new List<string>();
            if (equipment != null)
            {
                var serialNumberMetaData = equipment.MetaDataList?.FirstOrDefault(m => m.Key == "SerialNumber");
                if (serialNumberMetaData != null && !string.IsNullOrEmpty(serialNumberMetaData.MetaData.Value))
                {
                    additionalInfo.Add($"Serial number: {serialNumberMetaData.MetaData.Value}");
                }
                var internalMetaData = equipment.MetaDataList?.FirstOrDefault(m => m.Key == "InternalNumber");
                if (internalMetaData != null && !string.IsNullOrEmpty(internalMetaData.MetaData.Value))
                {
                    additionalInfo.Add($"Internal number: {internalMetaData.MetaData.Value}");
                }
                var InstallationNumberMetaData = equipment.MetaDataList?.FirstOrDefault(m => m.Key == "InstallationNumber");
                if (InstallationNumberMetaData != null && !string.IsNullOrEmpty(InstallationNumberMetaData.MetaData.Value))
                {
                    additionalInfo.Add($"Installation number: {InstallationNumberMetaData.MetaData.Value}");
                }
                var UDINumberMetaData = equipment.MetaDataList?.FirstOrDefault(m => m.Key == "UDINumber");
                if (UDINumberMetaData != null && !string.IsNullOrEmpty(UDINumberMetaData.MetaData.Value))
                {
                    additionalInfo.Add($"UDI number: {UDINumberMetaData.MetaData.Value}");
                }
            }
            return additionalInfo;
        }
    }
}
