using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using Nextended.Core.Extensions;

namespace Nextended.Imaging
{
    /// <summary>
    ///     Hilfklasse für Bildveraerbeitung
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        ///     Calculates the resize.
        /// </summary>
        /// <param name="imageSize">Size of the image.</param>
        /// <param name="boxSize">Size of the box.</param>
        /// <returns></returns>
        public static Size CalculateResize(Size imageSize, Size boxSize)
        {
            double widthScale = 0, heightScale = 0;
            if (imageSize.Width != 0)
                widthScale = boxSize.Width / (double) imageSize.Width;
            if (imageSize.Height != 0)
                heightScale = boxSize.Height / (double) imageSize.Height;

            var scale = Math.Min(widthScale, heightScale);

            return new Size((int) (imageSize.Width * scale),
                (int) (imageSize.Height * scale));
        }

        /// <summary>
        ///     Converts the image to a byte array.
        /// </summary>
        public static byte[] ConvertImageToByteArray(string fileName)
        {
            var bitMap = new Bitmap(fileName);
            var bmpFormat = bitMap.RawFormat;
            var imageToConvert = Image.FromFile(fileName);
            using (var ms = new MemoryStream())
            {
                imageToConvert.Save(ms, bmpFormat);
                return ms.ToArray();
            }
        }

        /// <summary>
        ///     Bild als ByteArray von einer Url
        /// </summary>
        public static Image GetImageFromUrl(string url)
        {
            var httpWebRequest = WebRequest.Create(url);
            var httpWebReponse = (HttpWebResponse) httpWebRequest.GetResponse();
            var stream = httpWebReponse.GetResponseStream();
            if (stream != null)
                return Image.FromStream(stream);
            return null;
        }


        /// <summary>
        ///     Erstellt ein Thumb bmp und gibt davon das byte array zurück (relationen werden bei behalten, und diese rückgabe
        ///     garantiert,
        ///     dass das bild nicht kleiner als size ist
        /// </summary>
        public static byte[] GetMinSizedImageThumbnailData(Image image, Size size,
            ResizeMode mode = ResizeMode.KeepScale)
        {
            var bmp = ResizeImage(image, size, mode);
            var result = bmp.ToByteArray(ImageFormat.Png);

            bmp.Dispose();
            image.Dispose();
            return result;
        }

        /// <summary>
        ///     Erstellt ein Thumb bmp und gibt davon das byte array zurück
        ///     (wenn useSizeAsHeight true wird die größe als höhe benutzt ansonsten als breite (relationen immer beibehalten)
        /// </summary>
        public static byte[] GetImageThumbnailData(Image image, int size, bool useSizeAsHeight = false)
        {
            var bmp = ResizeImage(image, size, useSizeAsHeight);
            var result = bmp.ToByteArray(ImageFormat.Png);

            bmp.Dispose();
            image.Dispose();
            return result;
        }


        /// <summary>
        ///     Größe eines bildes ändern
        /// </summary>
        /// <returns></returns>
        public static Bitmap ResizeImage(this Image image, Size size)
        {
            var resizedImage = new Bitmap(size.Width, size.Height);
            Graphics.FromImage(resizedImage).DrawImage(image, 0, 0, size.Width, size.Height);
            return resizedImage;
        }


        /// <summary>
        ///     Größe eines bildes ändern
        ///     Wenn keepScale auf true ist, wird die skalierung in jedem fall beibehalten, und die rückgabe garantiert dann nur,
        ///     dass sie nicht kleiner ist als size
        /// </summary>
        /// <returns></returns>
        public static Bitmap ResizeImage(this Image image, Size size, ResizeMode mode)
        {
            if (mode == ResizeMode.Stretch)
                return ResizeImage(image, size);
            var newSize = CalculateResize(image.Size, size);
            var i = 1;
            while (newSize.Height < size.Height || newSize.Width < size.Width)
            {
                newSize = CalculateResize(image.Size, new Size(size.Width + i, size.Height + i));
                i++;
            }

            var result = ResizeImage(image, newSize);
            if (mode == ResizeMode.KeepScale)
                return result;
            // Kepp Scale and Cut
            return CropBitmap(result, 0, 0, size.Width, size.Height);
        }

        /// <summary>
        ///     Bild schneiden
        /// </summary>
        public static Bitmap CropBitmap(this Bitmap bitmap, int cropX, int cropY, int cropWidth, int cropHeight)
        {
            var rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
            var cropped = bitmap.Clone(rect, bitmap.PixelFormat);
            return cropped;
        }

        /// <summary>
        ///     Größe eines bildes ändern
        ///     (wenn useSizeAsHeight true wird die größe als höhe benutzt ansonsten als breite (relationen immer beibehalten)
        /// </summary>
        /// <returns></returns>
        public static Bitmap ResizeImage(Image image, int size, bool useSizeAsHeight = false)
        {
            var srcWidth = image.Width;
            var srcHeight = image.Height;
            Bitmap bmp;
            int thumbHeight;
            var thumbWidth = size;

            if (!useSizeAsHeight) // Size as width
            {
                //
                thumbHeight = Convert.ToInt32(srcHeight / (double) srcWidth * size);
                bmp = new Bitmap(size, thumbHeight);
            }
            else
            {
                thumbHeight = size;
                thumbWidth = Convert.ToInt32(srcWidth / (double) srcHeight * size);
                bmp = new Bitmap(thumbWidth, thumbHeight);
            }

            var gr = Graphics.FromImage(bmp);
            gr.SmoothingMode = SmoothingMode.HighQuality;
            gr.CompositingQuality = CompositingQuality.HighQuality;
            gr.InterpolationMode = InterpolationMode.High;

            var rectDestination = new Rectangle(0, 0, thumbWidth, thumbHeight);
            gr.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);
            return bmp;
        }


        /// <summary>
        ///     Gibt den contenttype eines byte arrays zurück
        /// </summary>
        public static ImageFormat GetContentType(byte[] imageBytes)
        {
            var ms = new MemoryStream(imageBytes);

            using var br = new BinaryReader(ms);
            var maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

            var magicBytes = new byte[maxMagicBytesLength];

            for (var i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = br.ReadByte();

                foreach (var kvPair in imageFormatDecoders.Where(kvPair => magicBytes.StartsWith(kvPair.Key)))
                    return kvPair.Value;
            }

            throw new ArgumentException("Could not recognise image format", "binaryReader");
        }


        /// <summary>
        ///     Mimetype für Bitmap
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static string GetMimeType(this Image bitmap)
        {
            return bitmap.RawFormat.GetMimeType();
        }

        private const string errorMessage = "Could not recognise image format.";


        /// <summary>
        ///     Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static Size GetDimensions(string path)
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    return GetDimensions(binaryReader);
                }
                catch (ArgumentException e)
                {
                    if (e.Message.StartsWith(errorMessage)) throw new ArgumentException(errorMessage, "path", e);
                    throw;
                }
            }
        }


        /// <summary>
        ///     IsValid Image
        /// </summary>
        public static bool IsValidImage(byte[] bytes)
        {
            using (var binaryReader = new BinaryReader(new MemoryStream(bytes)))
            {
                try
                {
                    return IsValidImage(binaryReader);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static bool IsValidImage(string path)
        {
            if (!File.Exists(path))
                return false;
            using (var binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    return IsValidImage(binaryReader);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets the dimensions of an image.
        /// </summary>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static bool IsValidImage(BinaryReader binaryReader)
        {
            var maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

            var magicBytes = new byte[maxMagicBytesLength];

            for (var i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();

                if (imageFormatDecoders.Any(kvPair => magicBytes.StartsWith(kvPair.Key))) return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets the dimensions of an image.
        /// </summary>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static Size GetDimensions(BinaryReader binaryReader)
        {
            var maxMagicBytesLength = imageFormatDecodersFunc.Keys.OrderByDescending(x => x.Length).First().Length;

            var magicBytes = new byte[maxMagicBytesLength];

            for (var i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();

                foreach (var kvPair in imageFormatDecodersFunc)
                    if (magicBytes.StartsWith(kvPair.Key))
                        return kvPair.Value(binaryReader);
            }

            throw new ArgumentException(errorMessage, "binaryReader");
        }

        /// <summary>
        ///     Bild als ByteArray
        /// </summary>
        public static byte[] ToByteArray(this Image img, ImageFormat format = null)
        {
            if (format == null)
                format = img.RawFormat;
            if (format.Guid == ImageFormat.MemoryBmp.Guid)
                format = ImageFormat.Png;
            var mstream = new MemoryStream();
            img.Save(mstream, format);
            mstream.Flush();
            return mstream.ToArray();
        }

        /// <summary>
        ///     Bild als ByteArray
        /// </summary>
        public static Image FromByteArray(byte[] img)
        {
            using (var memStream = new MemoryStream(img))
            {
                return Image.FromStream(memStream);
            }
        }

        /// <summary>
        ///     Gibt z.B folgenden string zurück data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAANwAAA ......
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string ToHtmlImageString(this Image image)
        {
            var result = image.ToByteArray();
            var res = Convert.ToBase64String(result);
            res = string.Format("data:{0};{1},{2}", image.RawFormat.GetMimeType(), "base64", res);
            return res;
        }

        /// <summary>
        ///     Gibt dür einen HtmlImageString das echte Bild zurück
        /// </summary>
        public static Image FromHtmlImageString(string s)
        {
            var strings = s.Split(';');
            var base64 = strings[1].Split(',');
            //string contentType = strings[0];
            var encodedDataAsBytes = Convert.FromBase64String(base64[1]);
            var image = Image.FromStream(new MemoryStream(encodedDataAsBytes));
            return image;
        }

        /// <summary>
        ///     Mimetyp für ein Imageformat
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetMimeType(this ImageFormat format)
        {
            string result;

            if (format.Guid == ImageFormat.Bmp.Guid)
                result = "bmp";
            else if (format.Guid == ImageFormat.Emf.Guid)
                result = "emf";
            else if (format.Guid == ImageFormat.Exif.Guid)
                result = "exif";
            else if (format.Guid == ImageFormat.Gif.Guid)
                result = "gif";
            else if (format.Guid == ImageFormat.Icon.Guid)
                result = "icon";
            else if (format.Guid == ImageFormat.Jpeg.Guid)
                result = "jpeg";
            else if (format.Guid == ImageFormat.MemoryBmp.Guid)
                result = "membmp";
            else if (format.Guid == ImageFormat.Png.Guid)
                result = "png";
            else if (format.Guid == ImageFormat.Tiff.Guid)
                result = "tiff";
            else if (format.Guid == ImageFormat.Wmf.Guid)
                result = "wmf";
            else
                result = "unknown";
            return $"image/{result}";
        }


        /// <summary>
        ///     Farbe des Bildes ändern
        /// </summary>
        public static Bitmap ChangeColor(string fullFilePath, Color newColor)
        {
            if (IsValidImage(fullFilePath))
            {
                var cacheKey = $"ChangeColor{fullFilePath}_{newColor}";
                return MemoryCache.Default.AddOrGetExisting(cacheKey,
                    () => ChangeColor(new Bitmap(fullFilePath), newColor));
            }

            throw new FormatException($"Invalid format! {fullFilePath} is not a valid imagefile");
        }

        /// <summary>
        ///     Farbe des Bildes ändern
        /// </summary>
        public static Bitmap ReplaceColor(string fullFilePath, Color colorToReplace, Color newColor)
        {
            if (IsValidImage(fullFilePath))
            {
                var cacheKey = $"ReplaceColor{fullFilePath}_{colorToReplace}_{newColor}";
                return MemoryCache.Default.AddOrGetExisting(cacheKey,
                    () => ReplaceColor(new Bitmap(fullFilePath), colorToReplace, newColor));
            }

            throw new FormatException($"Invalid format! {fullFilePath} is not a valid imagefile");
        }

        /// <summary>
        ///     Farbe des Bildes ändern
        /// </summary>
        public static Bitmap ChangeColor(Bitmap sourceBitmap, Color newColor)
        {
            var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            for (var x = 0; x < sourceBitmap.Width; x++)
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var currentPixelColor = sourceBitmap.GetPixel(x, y);
                resultBitmap.SetPixel(x, y, Color.FromArgb(currentPixelColor.A, newColor));
            }

            return resultBitmap;
        }

        /// <summary>
        ///     Farbe des Bildes ändern
        /// </summary>
        public static Bitmap ReplaceColor(Bitmap sourceBitmap, Color colorToReplace, Color newColor)
        {
            var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            for (var x = 0; x < sourceBitmap.Width; x++)
            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                var currentPixelColor = sourceBitmap.GetPixel(x, y);
                resultBitmap.SetPixel(x, y, Color.FromArgb(currentPixelColor.A, newColor));

                if (currentPixelColor.R == colorToReplace.R && currentPixelColor.G == colorToReplace.G &&
                    currentPixelColor.B == colorToReplace.B)
                    resultBitmap.SetPixel(x, y, Color.FromArgb(currentPixelColor.A, newColor));
                else
                    resultBitmap.SetPixel(x, y, currentPixelColor);
            }

            return resultBitmap;
        }

        private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(short)];
            for (var i = 0; i < sizeof(short); i += 1) bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            return BitConverter.ToInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(int)];
            for (var i = 0; i < sizeof(int); i += 1) bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            var width = binaryReader.ReadInt32();
            var height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            var width = binaryReader.ReadLittleEndianInt32();
            var height = binaryReader.ReadLittleEndianInt32();
            return new Size(width, height);
        }

        private static Size DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                var marker = binaryReader.ReadByte();
                var chunkLength = binaryReader.ReadLittleEndianInt16();

                if (marker == 0xc0)
                {
                    binaryReader.ReadByte();

                    int height = binaryReader.ReadLittleEndianInt16();
                    int width = binaryReader.ReadLittleEndianInt16();
                    return new Size(width, height);
                }

                binaryReader.ReadBytes(chunkLength - 2);
            }

            throw new ArgumentException(errorMessage);
        }


        private static bool StartsWith(this byte[] thisBytes, byte[] thatBytes)
        {
            for (var i = 0; i < thatBytes.Length; i += 1)
                if (thisBytes[i] != thatBytes[i])
                    return false;
            return true;
        }

        private static readonly Dictionary<byte[], Func<BinaryReader, Size>> imageFormatDecodersFunc = new()
        {
            {new byte[] {0x42, 0x4D}, DecodeBitmap},
            {new byte[] {0x47, 0x49, 0x46, 0x38, 0x37, 0x61}, DecodeGif},
            {new byte[] {0x47, 0x49, 0x46, 0x38, 0x39, 0x61}, DecodeGif},
            {new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, DecodePng},
            {new byte[] {0xff, 0xd8}, DecodeJfif}
        };

        private static readonly Dictionary<byte[], ImageFormat> imageFormatDecoders = new()
        {
            {new byte[] {0x42, 0x4D}, ImageFormat.Bmp},
            {new byte[] {0x47, 0x49, 0x46, 0x38, 0x37, 0x61}, ImageFormat.Gif},
            {new byte[] {0x47, 0x49, 0x46, 0x38, 0x39, 0x61}, ImageFormat.Gif},
            {new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, ImageFormat.Png},
            {new byte[] {0xff, 0xd8}, ImageFormat.Jpeg}
        };
    }

    /// <summary>
    ///     ResizeMode
    /// </summary>
    public enum ResizeMode
    {
        /// <summary>
        ///     StretchMode
        /// </summary>
        Stretch, // Wird knallhart geändert egal ab skalierung stimm

        /// <summary>
        ///     KeepScale
        /// </summary>
        KeepScale, // wird die skalierung in jedem fall beibehalten, und die rückgabe garantiert dann nur, dass sie nicht kleiner ist als size

        /// <summary>
        ///     KeepScaleAndCut
        /// </summary>
        KeepScaleAndCut // Behält die skalierung, aber schneidet das Bild anschließend damit die größe exact size beträgt
    }
}