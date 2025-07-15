using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Nextended.Web.Extensions
{

    public static class ControllerExtensions
    {
        /// <summary>
        /// Serves data as a asynchronous Stream to the client after setting the required Headers
        /// </summary>
        /// <param name="controller">The Controller used to serve the data</param>
        /// <param name="dataContent">The Data in the body</param>
        /// <param name="mimeType">The MIME-Type of the downloaded Data</param>
        /// <param name="fileName">The desired Filename</param>
        /// <param name="inlineFile">Should the browser try to display the file, rather than use a dialog</param>
        /// <param name="httpStatusCode">The Http-Statuscode</param>
        /// <param name="additionalHeaders">Additional headers that should be set</param>
        /// <returns></returns>
        public static async Task DownloadDataAsync(this ControllerBase controller, Stream dataContent, string mimeType, string fileName, bool inlineFile, int httpStatusCode, params (string key, StringValues value)[] additionalHeaders)
        {
            SetFileDownloadHeaders(controller, mimeType, fileName, inlineFile, httpStatusCode, additionalHeaders);
            await DownloadDataAsync(controller, dataContent);
        }

        /// <summary>
        /// Serves data as a asynchronous Stream to the client after setting the required Headers
        /// </summary>
        /// <param name="controller">The Controller used to serve the data</param>
        /// <param name="writeResponseDataAction">An async Function that can be used to write complex data directly to the output</param>
        /// <param name="mimeType">The MIME-Type of the downloaded Data</param>
        /// <param name="fileName">The desired Filename</param>
        /// <param name="inlineFile">Should the browser try to display the file, rather than use a dialog</param>
        /// <param name="httpStatusCode">The Http-Statuscode</param>
        /// <param name="additionalHeaders">Additional headers that should be set</param>
        /// <returns></returns>
        public static async Task DownloadDataAsync(this ControllerBase controller, Func<Stream, Task> writeResponseDataAction, string mimeType, string fileName, bool inlineFile, int httpStatusCode, params (string key, StringValues value)[] additionalHeaders)
        {
            SetFileDownloadHeaders(controller, mimeType, fileName, inlineFile, httpStatusCode, additionalHeaders);
            await DownloadDataAsync(controller, writeResponseDataAction);
        }

        public static async Task DownloadDataAsync(this ControllerBase controller, Func<Stream, Task> writeResponseDataAction)
        {
            await writeResponseDataAction(controller.Response.Body);
        }

        public static async Task DownloadDataAsync(this ControllerBase controller, Stream dataContent)
        {
            await dataContent.CopyToAsync(controller.Response.Body);
        }

        private static void SetFileDownloadHeaders(ControllerBase controller, string mimeType, string fileName, bool inlineFile, int statusCode, (string key, StringValues value)[] additionalHeaders)
        {
            controller.Response.ContentType = string.IsNullOrEmpty(mimeType) ? "application/octet-stream" : mimeType;

            if (!string.IsNullOrEmpty(fileName))
            {
                var disposition = new ContentDisposition()
                {
                    Inline = inlineFile,
                };

                if (!inlineFile)
                {
                    // Filename is only allowed when file is 'attachment'.
                    // URL encode the file name if any non-extended ASCII (> 255) characters are detected
                    var encodedFileName = fileName.Any(c => c > 255) ?
                        WebUtility.UrlEncode(fileName) :
                        fileName;
                    disposition.FileName = encodedFileName;
                }

                controller.Response.Headers.Add(HeaderNames.ContentDisposition, disposition.ToString());
            }

            if (statusCode > 0)
            {
                controller.Response.StatusCode = statusCode;
            }

            if (additionalHeaders != null && additionalHeaders.Length > 0)
            {
                foreach (var additionalHeader in additionalHeaders)
                {
                    controller.Response.Headers.Add(additionalHeader.key, additionalHeader.value);
                }
            }
        }
    }
}