using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace AvStreamPlugin
{
    /// <summary>
    /// Heler class for containing all of the hoops we need to jump through to make our web requests work on all of our various platforms.
    /// </summary>
    public static class Http
    {
        static Http()
        {
#if true
            // Don't allow SSL (POODLE). Enable the other TLS versions.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // We were hitting cert issues. This avoids them for now. Need to revisit this, though.
            ServicePointManager.ServerCertificateValidationCallback = delegate (
                object s,
                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
#endif
        }

        internal static JToken DownloadJson(string url, string headerKey = "", string headerValue = "")
        {
            return JToken.Parse(DownloadString(url, headerKey, headerValue));
        }

        internal static string DownloadString(string url, string headerKey = "", string headerValue = "")
        {
#if false
            // An attempt at disabling ipv6 and proxies to work around issues with VPN. Not what we
            // want to ship with, I think? But it resolves issues with the altspace VPN. Note that
            // this doesn't solve the FFMpeg issues.
            HttpWebRequest request = System.Net.WebRequest.Create(url) as HttpWebRequest;

            request.Proxy = null;
            request.ServicePoint.BindIPEndPointDelegate = (servicePount, remoteEndPoint, retryCount) =>
            {
                if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return new IPEndPoint(IPAddress.Any, 0);
                }
                throw new System.InvalidOperationException("No IPv4 address found.");
            };

            if (!string.IsNullOrWhiteSpace(headerKey) && !string.IsNullOrWhiteSpace(headerValue))
            {
                request.Headers[headerKey] = headerValue;
            }

            string result = "";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                }
            }
            return result;
#else
#if true
            // System.Net.Http.
            // UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url);
            // UnityEngine.Networking.UnityWebRequestAsyncOperation
            using (WebClient client = new WebClient())
            {
                if (!string.IsNullOrWhiteSpace(headerKey) && !string.IsNullOrWhiteSpace(headerValue))
                {
                    client.Headers[headerKey] = headerValue;
                }
                return client.DownloadString(url);
            }
#else
            // This doens't work. These calls must be made from the main thread. Dammit.
            UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url);
            if (!string.IsNullOrWhiteSpace(headerKey) && !string.IsNullOrWhiteSpace(headerValue))
            {
                request.SetRequestHeader(headerKey, headerValue);
            }
            UnityEngine.Networking.UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                System.Threading.Thread.Yield();
            }

            if (request.isHttpError || request.isNetworkError)
            {
                throw new WebException(request.error);
            }

            return request.downloadHandler.text;
#endif
#endif
        }
    }
}
