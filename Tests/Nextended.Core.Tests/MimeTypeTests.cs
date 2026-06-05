using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class MimeTypeTests
    {
        [TestMethod]
        public void ContainesArchives()
        {
            var types = MimeType.ArchiveTypes.ToList();
            Assert.IsTrue(types.Contains("application/zip"), "ArchiveTypes should contain application/zip");
        }

        [TestMethod]
        public void ArchiveTypes_Contain_CommonArchiveTypes()
        {
            var types = MimeType.ArchiveTypes;

            CollectionAssert.Contains(types, "application/x-compressed");
            CollectionAssert.Contains(types, "application/x-zip-compressed");
            CollectionAssert.Contains(types, MimeType.All["zip"]); // application/zip
            CollectionAssert.Contains(types, MimeType.All["rar"]); // application/x-rar-compressed
            CollectionAssert.Contains(types, MimeType.All["7z"]);  // application/x-7z-compressed
            CollectionAssert.Contains(types, MimeType.All["tar"]); // application/x-tar
            CollectionAssert.Contains(types, MimeType.All["tgz"]); // application/tar+gzip
        }

        [TestMethod]
        public void IsArchive_ReturnsTrue_ForKnownArchives()
        {
            Assert.IsTrue(MimeType.IsArchive("application/zip"));
            Assert.IsTrue(MimeType.IsArchive("application/x-rar-compressed"));
            Assert.IsTrue(MimeType.IsArchive("application/x-7z-compressed"));
            Assert.IsTrue(MimeType.IsArchive("application/x-tar"));
            Assert.IsTrue(MimeType.IsArchive("application/tar+gzip"));
            Assert.IsTrue(MimeType.IsArchive("application/x-compressed"));
            Assert.IsTrue(MimeType.IsArchive("application/x-zip-compressed"));
        }

        [TestMethod]
        public void IsArchive_ReturnsFalse_ForNonArchives_AndNullOrEmpty()
        {
            Assert.IsFalse(MimeType.IsArchive("application/pdf"));
            Assert.IsFalse(MimeType.IsArchive("text/plain"));
            Assert.IsFalse(MimeType.IsArchive("image/png"));
            Assert.IsFalse(MimeType.IsArchive(null));
            Assert.IsFalse(MimeType.IsArchive(""));
            Assert.IsFalse(MimeType.IsArchive(" "));
        }

        [TestMethod]
        public void IsZip_WorksForZipMimeTypes()
        {
            Assert.IsTrue(MimeType.IsZip("application/zip"));
            Assert.IsTrue(MimeType.IsZip("application/x-zip-compressed"));
            Assert.IsTrue(MimeType.IsZip("application/x-zip"));

            Assert.IsFalse(MimeType.IsZip("application/x-rar-compressed"));
            Assert.IsFalse(MimeType.IsZip("application/pdf"));
            Assert.IsFalse(MimeType.IsZip(null));
        }

        [TestMethod]
        public void IsRar_WorksForRarMimeTypes_AndCompressed()
        {
            Assert.IsTrue(MimeType.IsRar("application/x-compressed"));
            Assert.IsTrue(MimeType.IsRar("application/x-rar-compressed"));

            Assert.IsFalse(MimeType.IsRar("application/zip"));
            Assert.IsFalse(MimeType.IsRar("application/pdf"));
            Assert.IsFalse(MimeType.IsRar(null));
        }

        [TestMethod]
        public void IsTar_WorksForTarMimeTypes()
        {
            Assert.IsTrue(MimeType.IsTar("application/x-tar"));
            Assert.IsTrue(MimeType.IsTar("application/tar+gzip"));
            Assert.IsTrue(MimeType.IsTar("application/tar+gzip")); // tar.gz mapping

            Assert.IsFalse(MimeType.IsTar("application/x-rar-compressed"));
            Assert.IsFalse(MimeType.IsTar("application/zip"));
            Assert.IsFalse(MimeType.IsTar(null));
        }

        [TestMethod]
        public void Is7Zip_WorksFor7zMimeType()
        {
            var sevenZip = MimeType.All["7z"]; // application/x-7z-compressed

            Assert.IsTrue(MimeType.Is7Zip(sevenZip));
            Assert.IsFalse(MimeType.Is7Zip("application/zip"));
            Assert.IsFalse(MimeType.Is7Zip(null));
        }

        [TestMethod]
        public void IsAudio_ReturnsTrueForAudioTypes_AndFalseOtherwise()
        {
            Assert.IsTrue(MimeType.IsAudio("audio/mpeg"));
            Assert.IsTrue(MimeType.IsAudio("audio/mp4"));
            Assert.IsTrue(MimeType.IsAudio("audio/ogg"));

            Assert.IsFalse(MimeType.IsAudio("video/webm"));
            Assert.IsFalse(MimeType.IsAudio("application/pdf"));
            Assert.IsFalse(MimeType.IsAudio(null));
        }

        [TestMethod]
        public void IsVideo_ReturnsTrueForVideoTypes_AndFalseOtherwise()
        {
            Assert.IsTrue(MimeType.IsVideo("video/mp4"));
            Assert.IsTrue(MimeType.IsVideo("video/webm"));
            Assert.IsTrue(MimeType.IsVideo("video/x-msvideo"));

            Assert.IsFalse(MimeType.IsVideo("audio/mpeg"));
            Assert.IsFalse(MimeType.IsVideo("application/pdf"));
            Assert.IsFalse(MimeType.IsVideo(null));
        }

        [TestMethod]
        public void IsImage_ReturnsTrueForImageTypes_AndFalseOtherwise()
        {
            Assert.IsTrue(MimeType.IsImage("image/png"));
            Assert.IsTrue(MimeType.IsImage("image/jpeg"));
            Assert.IsTrue(MimeType.IsImage("image/gif"));

            Assert.IsFalse(MimeType.IsImage("application/pdf"));
            Assert.IsFalse(MimeType.IsImage("audio/mpeg"));
            Assert.IsFalse(MimeType.IsImage(null));
        }

        [TestMethod]
        public void IsPdf_ReturnsTrueOnlyForPdf()
        {
            Assert.IsTrue(MimeType.IsPdf("application/pdf"));

            Assert.IsFalse(MimeType.IsPdf("application/msword"));
            Assert.IsFalse(MimeType.IsPdf("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            Assert.IsFalse(MimeType.IsPdf(null));
        }

        [TestMethod]
        public void IsExcel_ReturnsTrueForXlsAndOpenXml()
        {
            Assert.IsTrue(MimeType.IsExcel(MimeType.Xls));
            Assert.IsTrue(MimeType.IsExcel(MimeType.OpenXml));

            Assert.IsFalse(MimeType.IsExcel("application/pdf"));
            Assert.IsFalse(MimeType.IsExcel("text/csv"));
            Assert.IsFalse(MimeType.IsExcel(null));
        }

        [TestMethod]
        public void IsWord_ReturnsTrueForWordMimeTypes()
        {
            Assert.IsTrue(MimeType.IsWord("application/msword"));
            Assert.IsTrue(MimeType.IsWord("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            Assert.IsTrue(MimeType.IsWord("application/vnd.openxmlformats-officedocument.wordprocessingml.template"));

            Assert.IsFalse(MimeType.IsWord("application/pdf"));
            Assert.IsFalse(MimeType.IsWord("text/plain"));
            Assert.IsFalse(MimeType.IsWord(null));
        }

        [TestMethod]
        public void Matches_WorksWithExactAndRegexPatterns()
        {
            var pdf = "application/pdf";

            // exact
            Assert.IsTrue(MimeType.Matches(pdf, "application/pdf"));

            // regex pattern
            Assert.IsTrue(MimeType.Matches(pdf, "application/.*"));
            Assert.IsTrue(MimeType.Matches("image/png", "image/.*"));

            // no match
            Assert.IsFalse(MimeType.Matches(pdf, "image/.*"));
            Assert.IsFalse(MimeType.Matches(pdf, Array.Empty<string>()));
            Assert.IsFalse(MimeType.Matches(null, "application/pdf"));
        }

        [TestMethod]
        public void All_Returns_NonEmpty_Dictionary_With_KnownEntries()
        {
            var all = MimeType.All;

            Assert.IsTrue(all.Count > 0);
            Assert.AreEqual("application/pdf", all["pdf"]);
            Assert.AreEqual("text/plain", all["txt"]);
        }

        [TestMethod]
        public void ImageTypes_ContainOnlyImageMimeTypes()
        {
            var images = MimeType.ImageTypes;

            Assert.IsTrue(images.Length > 0);
            Assert.IsTrue(images.All(x => x.StartsWith("image/", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DocumentTypes_ContainPdfAndOfficeTypes()
        {
            var docs = MimeType.DocumentTypes;

            CollectionAssert.Contains(docs, "application/pdf");
            CollectionAssert.Contains(docs, "application/msword");
            CollectionAssert.Contains(docs, "application/vnd.ms-excel");
            CollectionAssert.Contains(docs, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [TestMethod]
        public void GetExtension_ReturnsCorrectExtension_ForKnownMime()
        {
            var extPdf = MimeType.GetExtension("application/pdf");
            var extPng = MimeType.GetExtension("image/png");

            Assert.AreEqual("pdf", extPdf);
            Assert.AreEqual("png", extPng);
        }

        [TestMethod]
        public void GetExtension_ReturnsDefault_ForUnknownMime()
        {
            var ext = MimeType.GetExtension("application/this-does-not-exist");

            Assert.AreEqual("bin", ext);
        }

        [TestMethod]
        public void GetMimeType_ReturnsCorrectMime_ForKnownExtension()
        {
            Assert.AreEqual("application/pdf", MimeType.GetMimeType("test.pdf"));
            Assert.AreEqual("application/pdf", MimeType.GetMimeType("test.PDF"));
            Assert.AreEqual("text/plain", MimeType.GetMimeType("readme.txt"));
        }

        [TestMethod]
        public void GetMimeType_ReturnsDefault_ForUnknownExtension()
        {
            Assert.AreEqual("application/octet-stream", MimeType.GetMimeType("file.unknownext"));
            Assert.AreEqual("application/octet-stream", MimeType.GetMimeType("file"));
        }

        [TestMethod]
        public void GetMimeType_UsesFullString_WhenNoDot_AndExtensionExists()
        {
            // "pdf" as filename -> ext is "pdf" which is known
            Assert.AreEqual("application/pdf", MimeType.GetMimeType("pdf"));
        }

        [TestMethod]
        public void AddOrUpdate_AddsNewExtension_AndCanBeResolved()
        {
            const string customMime = "application/x-custom";
            const string extension = "cstm";

            MimeType.AddOrUpdate(customMime, extension);

            Assert.AreEqual(customMime, MimeType.All[extension]);
            Assert.AreEqual(customMime, MimeType.GetMimeType("file.cstm"));
        }

        [TestMethod]
        public void AddOrUpdate_UpdatesExistingExtension()
        {
            const string extension = "cstm-update";
            const string originalMime = "application/x-original";
            const string updatedMime = "application/x-updated";

            MimeType.AddOrUpdate(originalMime, extension);
            Assert.AreEqual(originalMime, MimeType.GetMimeType("file.cstm-update"));

            MimeType.AddOrUpdate(updatedMime, extension);
            Assert.AreEqual(updatedMime, MimeType.GetMimeType("file.cstm-update"));
        }

        [TestMethod]
        public async Task ReadMimeTypeFromUrlAsync_ReturnsMappedType_ForInvalidUrl_TreatedAsFileName()
        {
            // invalid URL -> IsValidUrl = false -> GetMimeType wird verwendet
            var mime = await MimeType.ReadMimeTypeFromUrlAsync("file.pdf");

            Assert.AreEqual("application/pdf", mime);
        }

        [TestMethod]
        public async Task ReadMimeTypeFromUrlAsync_UsesHttpClient_AndReturnsContentType()
        {
            var handler = new FakeSuccessHandler("application/json");
            using var client = new HttpClient(handler);

            var mime = await MimeType.ReadMimeTypeFromUrlAsync("https://example.com/file.json", client);

            Assert.AreEqual("application/json", mime);
        }

        [TestMethod]
        public async Task ReadMimeTypeFromUrlAsync_ReturnsDefault_OnHttpError()
        {
            var handler = new FakeThrowingHandler();
            using var client = new HttpClient(handler);

            var mime = await MimeType.ReadMimeTypeFromUrlAsync("https://example.com/file", client);

            Assert.AreEqual("application/octet-stream", mime);
        }

        [TestMethod]
        public async Task ReadMimeTypeFromUrlAsync_Overload_UsesDefaultHttpClient_ForInvalidUrl()
        {
            // invalid URL -> kein HTTP Request, fällt auf GetMimeType zurück
            var mime = await MimeType.ReadMimeTypeFromUrlAsync("another.pdf");

            Assert.AreEqual("application/pdf", mime);
        }

        // --- Hilfs-Handler für HttpClient-Tests ---

        private sealed class FakeSuccessHandler : HttpMessageHandler
        {
            private readonly string _mediaType;

            public FakeSuccessHandler(string mediaType)
            {
                _mediaType = mediaType;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test")
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(_mediaType);
                return Task.FromResult(response);
            }
        }

        private sealed class FakeThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("Simulated failure");
            }
        }
    }
}
