using Assets.SimpleSignIn.Google.Scripts.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public class SavedAuth
    {
        public string ClientId;
        public TokenResponse TokenResponse;
        public UserInfo UserInfo;

        [Preserve]
        private SavedAuth()
        {
        }

        public SavedAuth(string clientId, TokenResponse tokenResponse)
        {
            ClientId = clientId;
            TokenResponse = tokenResponse;
        }

        public static SavedAuth GetInstance(string clientId)
        {
            var key = GetKey(clientId);

            if (!PlayerPrefs.HasKey(key)) return null;

            try
            {
                var encrypted = PlayerPrefs.GetString(key);
                var json = AES.Decrypt(encrypted, SystemInfo.deviceUniqueIdentifier);

                return JsonConvert.DeserializeObject<SavedAuth>(json);
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            var key = GetKey(ClientId);
            var json = JsonConvert.SerializeObject(this);
            var encrypted = AES.Encrypt(json, SystemInfo.deviceUniqueIdentifier);

            PlayerPrefs.SetString(key, encrypted);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            var key = GetKey(ClientId);

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static string GetKey(string clientId)
        {
            return Md5.ComputeHash(nameof(SavedAuth) + ':' + clientId);
        }
    }
}