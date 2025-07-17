using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleSignIn.Google.Scripts;

namespace Assets.SimpleSignIn.Google
{
    public class Example : MonoBehaviour
    {
        public GoogleAuth GoogleAuth;
        public Text Log;
        public Text Output;

        public void Start()
        {
            Application.logMessageReceived += (condition, _, _) => Log.text += condition + '\n';
            GoogleAuth = new GoogleAuth();
            GoogleAuth.TryResume(OnSignIn, OnGetTokenResponse);
        }

        public void SignIn()
        {
            GoogleAuth.SignIn(OnSignIn, caching: true);
        }

        public void SignOut()
        {
            GoogleAuth.SignOut(revokeAccessToken: true);
            Output.text = "Not signed in";
        }

        public void GetAccessToken()
        {
            GoogleAuth.GetTokenResponse(OnGetTokenResponse);
        }

        private void OnSignIn(bool success, string error, UserInfo userInfo)
        {
            Output.text = success ? $"Hello, {userInfo.name}!" : error;
        }

        private void OnGetTokenResponse(bool success, string error, TokenResponse tokenResponse)
        {
            Output.text = success ? $"Access token: {tokenResponse.AccessToken}" : error;

            if (!success) return;

            var jwt = new JWT(tokenResponse.IdToken);

            Debug.Log($"JSON Web Token (JWT) Payload: {jwt.Payload}");

            jwt.ValidateSignature(GoogleAuth.ClientId, OnValidateSignature);
        }

        private void OnValidateSignature(bool success, string error)
        {
            Output.text += Environment.NewLine;
            Output.text += success ? "JWT signature validated" : error;
        }

        public void Navigate(string url)
        {
            Application.OpenURL(url);
        }

        #region Research

        /// <summary>
        /// https://docs.unity.com/ugs/en-us/manual/authentication/manual/platform-signin-google
        /// Note: this will work ONLY if [Services > Google Identity Provider > Client ID] is exactly the same as was used for obtaining ID Token.
        /// In fact, we use different credentials (with different Client ID) for different platforms, but Unity accepts the only Client ID.
        /// As a result, it MAY work for 1 credentials only (for example, for universal iOS type that is used for Android/iOS/UWP).
        /// </summary>
        public async void UnityAuthenticationWithIdToken()
        {
            //try
            //{
            //    var idToken = (await GoogleAuth.GetTokenResponseAsync()).IdToken;

            //    await Unity.Services.Core.UnityServices.InitializeAsync();

            //    var authService = Unity.Services.Authentication.AuthenticationService.Instance;

            //    await authService.SignInWithGoogleAsync(idToken);

            //    Debug.Log($"IsAuthorized={authService.IsSignedIn}");
            //    Debug.Log(Social.Active.localUser.userName);
            //}
            //catch (Exception e)
            //{
            //    Debug.LogException(e);
            //}
        }

        /// <summary>
        /// https://docs.unity.com/ugs/en-us/manual/authentication/manual/platform-signin-google-play-games
        /// Note: does NOT work, waiting for Unity Support response.
        /// </summary>
        public async void UnityAuthenticationWithAuthCode()
        {
            //try
            //{
            //    var authCode = await GoogleAuth.GetAuthorizationCodeAsync();

            //    await Unity.Services.Core.UnityServices.InitializeAsync();

            //    var authService = Unity.Services.Authentication.AuthenticationService.Instance;

            //    await authService.SignInWithGooglePlayGamesAsync(authCode);

            //    Debug.Log($"IsAuthorized={authService.IsSignedIn}");
            //}
            //catch (Exception e)
            //{
            //    Debug.LogException(e);
            //}
        }

        #endregion
    }
}