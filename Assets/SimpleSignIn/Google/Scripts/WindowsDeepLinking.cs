#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32; // Requires .NET 4.x (Player Settings / Configuration).
using UnityEngine;

namespace Assets.SimpleSignIn.Google.Scripts
{
    /// <summary>
    /// Add deep linking support for Windows.
    /// </summary>
    public class WindowsDeepLinking : MonoBehaviour
    {
        public static event Action<string> DeepLinkActivated;

        private static WindowsDeepLinking _instance;

        private string _uriScheme;
        private Action<string> _callback;
        private const string RegistryValueName = "uri";

        public static void Initialize(string uriScheme, Action<string> callback = null)
        {
            RegisterCustomUriScheme(uriScheme);
            CreateCommandScript(uriScheme);

            if (_instance == null)
            {
                _instance = new GameObject(nameof(WindowsDeepLinking)).AddComponent<WindowsDeepLinking>();
            }

            _instance._uriScheme = uriScheme;
            _instance._callback = callback;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            // .NET Framework should be used instead of .NET Standard.
            using var key = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Classes\{_uriScheme}", writable: true);
            var value = key?.GetValue(RegistryValueName);

            if (value == null) return;

            key.DeleteValue(RegistryValueName);

            var uri = (string) value;

            if (!uri.StartsWith(_uriScheme) || !uri.Contains("state=") || !uri.Contains("code=")) return;

            Destroy(gameObject);
            _callback?.Invoke(uri);
            DeepLinkActivated?.Invoke(uri);
        }

        private static void RegisterCustomUriScheme(string uriScheme)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Classes\{uriScheme}");

            if (key == null) throw new Exception("Unable to create registry key.");
            
            var applicationLocation = Path.Combine(Environment.CurrentDirectory, Application.productName + ".cmd");

            key.SetValue("URL Protocol", "");

            using var commandKey = key.CreateSubKey(@"shell\open\command");

            if (commandKey == null) throw new Exception("Unable to create registry sub key.");

            commandKey.SetValue("", $"\"{applicationLocation}\" \"%1\"");
        }

        private static void CreateCommandScript(string uriScheme)
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, Application.productName + ".exe");
            var cmdPath = Path.Combine(Environment.CurrentDirectory, Application.productName + ".cmd");

            File.WriteAllLines(cmdPath, new List<string>
            {
                $"REG ADD \"HKEY_CLASSES_ROOT\\{uriScheme}\" /v \"{RegistryValueName}\" /t REG_SZ /d %1 /f",
                $"start \"\" \"{appPath}\" %1"
            });
        }
    }
}

#endif