using System;
using System.IO;
using System.Net.Http;
using Altinn.AccessManagement.Tests.Controllers;

namespace Altinn.AccessManagement.Tests.Utils
{
    /// <summary>
    /// Utility class for usefull common operations for setup for unittests
    /// </summary>
    public static class SetupUtils
    {
        /// <summary>
        /// Deletes a app blob stored locally
        /// </summary>
        /// <param name="org">Org</param>
        /// <param name="app">App</param>
        public static void DeleteAppBlobData(string org, string app)
        {
            string blobPath = Path.Combine(GetDataBlobPath(), $"{org}/{app}");

            if (Directory.Exists(blobPath))
            {
                Directory.Delete(blobPath, true);
            }
        }

        /// <summary>
        /// Adds an auth cookie to the request message
        /// </summary>
        /// <param name="requestMessage">the request message</param>
        /// <param name="token">the tijen to be added in the cookie</param>
        /// <param name="cookieName">the name of the cookie</param>
        /// <param name="xsrfToken">the xsrf token</param>
        public static void AddAuthCookie(HttpRequestMessage requestMessage, string token, string cookieName, string xsrfToken = null)
        {
            requestMessage.Headers.Add("Cookie", cookieName + "=" + token);
            if (xsrfToken != null)
            {
                requestMessage.Headers.Add("X-XSRF-TOKEN", xsrfToken);
            }
        }

        private static string GetDataBlobPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(DelegationsControllerTest).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "data", "blobs");
        }
    }
}
