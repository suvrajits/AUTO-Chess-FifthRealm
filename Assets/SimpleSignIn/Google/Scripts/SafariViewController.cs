using System;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR

using System;
using System.Runtime.InteropServices;
using UnityEngine.iOS;

#endif

namespace Assets.SimpleSignIn.Google.Scripts
{
    /// <summary>
    /// https://developer.apple.com/documentation/safariservices/sfsafariviewcontrollerdelegate
    /// </summary>
    public class SafariViewController : MonoBehaviour
    {
        /// <summary>
        /// This method is invoked when SFSafariViewController completes the loading of the URL that you pass to its initializer.
        /// The method is not invoked for any subsequent page loads in the same SFSafariViewController instance.
        /// </summary>
        public static event Action<bool> DidCompleteInitialLoad;

        /// <summary>
        /// This method is invoked when the user is redirected from the initial URL.
        /// </summary>
        public static event Action<string> InitialLoadDidRedirectToURL;

        /// <summary>
        /// This method is invoked when the user dismissed the view.
        /// </summary>
        public static event Action DidFinish;

        #if UNITY_IOS && !UNITY_EDITOR

        [DllImport("__Internal")]
        static extern void openURL(string url);

        [DllImport("__Internal")]
        static extern void dismiss();

        private static SafariViewController _instance;

        public static void OpenURL(string url)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(SafariViewController)).AddComponent<SafariViewController>();
            }

            if (Version.Parse(Device.systemVersion).Major >= 9)
            {
                openURL(url);
            }
            else
            {
                Application.OpenURL(url);
            }
        }

        public static void Close()
        {
            if (Version.Parse(UnityEngine.iOS.Device.systemVersion).Major >= 9)
            {
                dismiss();
            }
        }

        #endif

        void didCompleteInitialLoad(string didLoadSuccessfully)
        {
            Debug.Log($"DidCompleteInitialLoad={didLoadSuccessfully}");
            DidCompleteInitialLoad?.Invoke(didLoadSuccessfully == "1");
        }

        void initialLoadDidRedirectToURL(string url)
        {
            Debug.Log($"InitialLoadDidRedirectToURL={url}");
            InitialLoadDidRedirectToURL?.Invoke(url);
        }

        void didFinish()
        {
            Debug.Log("DidFinish");
            DidFinish?.Invoke();
        }
    }
}