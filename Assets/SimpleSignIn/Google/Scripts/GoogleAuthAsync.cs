using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public partial class GoogleAuth
    {
        /// <summary>
        /// Performs sign-in and returns UserInfo asynchronously.
        /// </summary>
        public async Task<UserInfo> SignInAsync()
        {
            var completed = false;
            string error = null;
            UserInfo userInfo = null;

            SignIn((success, e, result) =>
            {
                if (success)
                {
                    userInfo = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            }, caching: true);

            while (!completed)
            {
                await Task.Yield();
            }

            if (userInfo == null) throw new Exception(error);

            Log($"userInfo={JsonConvert.SerializeObject(userInfo)}");

            return userInfo;
        }

        /// <summary>
        /// Returns TokenResponse asynchronously.
        /// </summary>
        public async Task<TokenResponse> GetTokenResponseAsync()
        {
            var completed = false;
            string error = null;
            TokenResponse tokenResponse = null;

            GetTokenResponse((success, e, result) =>
            {
                if (success)
                {
                    tokenResponse = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            });

            while (!completed)
            {
                await Task.Yield();
            }

            if (tokenResponse == null) throw new Exception(error);

            Log($"TokenResponse={JsonConvert.SerializeObject(TokenResponse)}");

            return tokenResponse;
        }

        /// <summary>
        /// Returns AuthorizationCode asynchronously.
        /// </summary>
        public async Task<string> GetAuthorizationCodeAsync()
        {
            var completed = false;
            string error = null;
            string code = null;

            GetAuthorizationCode((success, e, result) =>
            {
                if (success)
                {
                    code = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            });

            while (!completed)
            {
                await Task.Yield();
            }

            if (code == null) throw new Exception(error);

            Log($"AuthorizationCode={code}");

            return code;
        }
    }
}