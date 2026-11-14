using System.IO;
using PlaytestFeedback.Internal;
using UnityEditor;
using UnityEngine;

namespace PlaytestFeedback.Editor
{
    internal static class FeedbackMenu
    {
        const string SettingsFolder = "Assets/Resources";
        const string SettingsAssetPath = SettingsFolder + "/" + FeedbackSettings.ResourcesPath + ".asset";

        [MenuItem("Tools/Playtest Feedback/Create or Select Settings", priority = 0)]
        static void CreateOrSelectSettings()
        {
            var settings = FeedbackSettings.Load();
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(SettingsFolder)) AssetDatabase.CreateFolder("Assets", "Resources");
                settings = ScriptableObject.CreateInstance<FeedbackSettings>();
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PlaytestFeedback] Created settings at {SettingsAssetPath}. Paste your Discord webhook URL into it.", settings);
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Tools/Playtest Feedback/Open Outbox Folder", priority = 20)]
        static void OpenOutboxFolder()
        {
            Directory.CreateDirectory(FeedbackOutbox.FolderPath);
            EditorUtility.RevealInFinder(FeedbackOutbox.FolderPath);
        }
    }
}
