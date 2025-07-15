using System;
using System.Text;
using UnityEngine;

namespace Assets.SimpleSignIn.Google.Scripts
{
    public static class LoopbackFlow
    {
        private static Action<string> _callback;

        public static void Initialize(string url, Action<string> callback)
        {
            _callback = callback;

            var httpListener = new System.Net.HttpListener();

            httpListener.Prefixes.Add(url);
            httpListener.Start();

            var context = System.Threading.SynchronizationContext.Current;
            var asyncResult = httpListener.BeginGetContext(result => context.Send(HandleHttpListenerCallback, result), httpListener);

            if (!Application.runInBackground)
            {
                Debug.LogWarning("HttpListener is blocking the main thread. To avoid this, set [Run In Background] from [Player Settings].");

                // Block the thread when background mode is not supported to serve HTTP response while the application is not in focus.
                if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    Debug.LogWarning("No response received.");
                }
            }
        }

        private static void HandleHttpListenerCallback(object state)
        {
            var result = (IAsyncResult) state;
            var httpListener = (System.Net.HttpListener) result.AsyncState;
            var context = httpListener.EndGetContext(result);

            // Send an HTTP response to the browser to notify the user to close the browser.
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes(Resources.Load<TextAsset>("StandaloneTemplate").text.Replace("{0}", Application.productName));

            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            var output = response.OutputStream;

            output.Write(buffer, 0, buffer.Length);
            output.Close();
            httpListener.Close();
            _callback?.Invoke(context.Request.Url.AbsoluteUri);
        }
    }
}