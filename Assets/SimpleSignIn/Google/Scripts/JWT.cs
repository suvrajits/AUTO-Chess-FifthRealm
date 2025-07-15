using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Assets.SimpleSignIn.Google.Scripts.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleSignIn.Google.Scripts
{
    /// <summary>
    /// JWT debugger: https://jwt.io/
    /// </summary>
    public class JWT
    {
        public readonly string Encoded;

        public string Header => Base64UrlEncoder.Decode(Encoded.Split('.')[0]);
        public string Payload => Base64UrlEncoder.Decode(Encoded.Split('.')[1]);
        public string SignedData => Encoded.Split('.')[0] + "." + Encoded.Split('.')[1];
        public string Signature => Encoded.Split('.')[2];

        private const string JwksUri = "https://www.googleapis.com/oauth2/v3/certs";

        private static Dictionary<string, Dictionary<string, string>> KnownPublicKeys
        {
            get => PlayerPrefs.HasKey(JwksUri) ? JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(PlayerPrefs.GetString(JwksUri)) : new Dictionary<string, Dictionary<string, string>>();
            set => PlayerPrefs.SetString(JwksUri, JsonConvert.SerializeObject(value));
        }

        public JWT(string encoded)
        {
            Encoded = encoded;
        }

        /// <summary>
        /// More info: https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken
        /// Signature validation makes sense on a backend only in most cases.
        /// </summary>
        public void ValidateSignature(string clientId, Action<bool, string> callback)
        {
            var header = JObject.Parse(Header);

            if ((string) header["typ"] != "JWT")
            {
                callback(false, "Unexpected header (typ).");
                return;
            }

            if ((string) header["alg"] != "RS256")
            {
                callback(false, "Unexpected header (alg).");
                return;
            }

            var payload = JObject.Parse(Payload);

            if ((string) payload["iss"] != "https://accounts.google.com")
            {
                callback(false, "Unexpected payload (iss).");
                return;
            }

            if ((string) payload["aud"] != clientId)
            {
                callback(false, "Unexpected payload (aud).");
                return;
            }

            var exp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int) payload["exp"]);

            if (exp < DateTime.UtcNow)
            {
                callback(false, "JWT expired.");
                return;
            }

            var kid = (string) header["kid"];
            var knownPublicKeys = KnownPublicKeys;

            if (knownPublicKeys.ContainsKey(kid))
            {
                var verified = ValidateSignature(knownPublicKeys[kid]["n"], knownPublicKeys[kid]["e"]);

                if (verified)
                {
                    callback(true, null);
                }
                else
                {
                    callback(false, "Invalid JWT signature.");
                }

                return;
            }

            var request = UnityWebRequest.Get(JwksUri);

            request.SendWebRequest().completed += obj =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var certs = JObject.Parse(request.downloadHandler.text);
                    var keys = certs["keys"].ToDictionary(i => i["kid"].Value<string>(), i => i.ToObject<Dictionary<string, string>>());

                    KnownPublicKeys = keys;

                    if (!keys.ContainsKey(kid))
                    {
                        callback(false, $"Public key not found (kid={kid}).");
                        return;
                    }

                    var verified = ValidateSignature(keys[kid]["n"], keys[kid]["e"]);

                    if (verified)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, "Invalid JWT signature.");
                    }
                }
                else
                {
                    Debug.LogError(request.GetError());
                    callback(false, request.GetError());
                }

                request.Dispose();
            };
        }

        private bool ValidateSignature(string modulus, string exponent)
        {
            var parameters = new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(modulus),
                Exponent = Base64UrlEncoder.DecodeBytes(exponent)
            };
            var provider = new RSACryptoServiceProvider();

            provider.ImportParameters(parameters);

            var signature = Base64UrlEncoder.DecodeBytes(Signature);
            var data = Encoding.UTF8.GetBytes(SignedData);
            var verified = provider.VerifyData(data, SHA256.Create(), signature);

            return verified;
        }
    }
}