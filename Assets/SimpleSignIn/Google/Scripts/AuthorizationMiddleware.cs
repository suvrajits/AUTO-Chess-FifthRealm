using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public class AuthorizationMiddleware : MonoBehaviour
    {
        public const string Endpoint = "https://hippogames.dev/api/oauth";

        private static string _state;
        private static Action<bool, string, string> _callback;
        private static AuthorizationMiddleware _instance;

        public static void Auth(string redirectUri, string state, Action<bool, string> callback)
        {
            _state = state;
            _callback = null;

            var request = UnityWebRequest.Post(Endpoint + "/init", new Dictionary<string, string> { { "state", _state }, { "redirectUri", redirectUri }, { "clientName", Application.productName } });

            Log($"Initializing auth middleware: {request.url}?state={_state}&redirectUri={redirectUri}&clientName={Application.productName}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    callback(true, null);
                }
                else
                {
                    Debug.LogError(request.GetError());
                    callback(false, request.GetError());
                }

                request.Dispose();
            };
        }

        public static void Auth(string redirectUri, string state, Action authorizationRequest, Action<bool, string, string> callback)
        {
            _state = state;
            _callback = callback;

            var request = UnityWebRequest.Post(Endpoint + "/init", new Dictionary<string, string> { { "state", _state }, { "redirectUri", redirectUri }, { "clientName", Application.productName } });

            Log($"Initializing auth middleware: {request.url}?state={_state}&redirectUri={redirectUri}&clientName={Application.productName}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log($"Authorization: {authorizationRequest}");

                    authorizationRequest();

                    if (_instance == null)
                    {
                        _instance = new GameObject(nameof(AuthorizationMiddleware)).AddComponent<AuthorizationMiddleware>();
                    }
                }
                else
                {
                    Debug.LogError(request.GetError());
                    _callback(false, request.GetError(), null);
                }

                request.Dispose();
            };
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            var request = UnityWebRequest.Post(Endpoint + "/getcode", new Dictionary<string, string> { { "state", _state } });

            Log($"Obtaining auth code: {request.url}");

            request.SendWebRequest().completed += obj =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var code = request.downloadHandler.text;

                    Log($"code={code}");
                    _callback(true, null, code);
                }
                else
                {
                    Debug.LogError(request.GetError());
                    _callback(false, request.GetError(), null);
                }

                request.Dispose();
            };

            Destroy(gameObject);
            _instance = null;
        }

        private static void Log(string message)
        {
            Debug.Log(message); // TODO: Remove in Release.
        }
    }
}