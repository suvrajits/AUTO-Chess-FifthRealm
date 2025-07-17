using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public static class Extensions
    {
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<AsyncOperation>();

            asyncOp.completed += operation => { tcs.SetResult(operation); };

            return ((Task) tcs.Task).GetAwaiter();
        }

        public static string GetError(this UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.Success) return null;

            var error = request.error;

            if (error == "Cannot resolve destination host" || error == "Cannot connect to destination host") return $"{error}: {request.uri.Host}";

            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                error = $"{error}: {request.downloadHandler.text}";
            }

            if (!error.EndsWith('.')) error += '.';

            return error;
        }
    }
}