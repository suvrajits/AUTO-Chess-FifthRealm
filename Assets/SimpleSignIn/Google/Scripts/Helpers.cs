using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Assets.SimpleSignIn.Google.Scripts.Utils;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public static class Helpers
    {
        /// <summary>
        /// http://stackoverflow.com/a/3978040
        /// </summary>
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            listener.Stop();

            return port;
        }

        public static NameValueCollection ParseQueryString(string url)
        {
            var result = new NameValueCollection();

            foreach (Match match in Regex.Matches(url, @"(?<key>\w+)=(?<value>[^&#]+)"))
            {
                result.Add(match.Groups["key"].Value, Uri.UnescapeDataString(match.Groups["value"].Value));
            }

            return result;
        }
		
		public static string CreateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));

            return Base64UrlEncoder.Encode(hash);
        }
    }
}