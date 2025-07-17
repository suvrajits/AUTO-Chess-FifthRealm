using UnityEditor;
using UnityEngine;

namespace Assets.SimpleSignIn.Google.Scripts.Editor
{
    [CustomEditor(typeof(GoogleAuthSettings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (GoogleAuthSettings)target;
            var warning = settings.Validate();

            if (warning != null)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Google Cloud / Credentials"))
            {
                Application.OpenURL("https://console.cloud.google.com/apis/credentials");
            }

            if (GUILayout.Button("Wiki"))
            {
                Application.OpenURL("https://github.com/hippogamesunity/SimpleSignIn/wiki/Google");
            }
        }
    }
}