using PlaytestFeedback.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace PlaytestFeedback.Editor
{
    [CustomEditor(typeof(FeedbackSettings))]
    internal sealed class FeedbackSettingsEditor : UnityEditor.Editor
    {
        static bool s_ShowWebhook;

        string _testStatus;
        MessageType _testStatusType;
        bool _testRunning;

        public override void OnInspectorGUI()
        {
            var settings = (FeedbackSettings)target;
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Press the hotkey in Play mode to open the feedback panel (F8, or L3 + R3 on a gamepad, unless changed under Hotkey below).\n" +
                "This asset must stay at Resources/PlaytestFeedbackSettings to be found automatically.",
                MessageType.Info);

            DrawWebhookSection(settings);

            var property = serializedObject.GetIterator();
            property.NextVisible(true); // Skip the script field.
            while (property.NextVisible(false))
            {
                if (property.name == nameof(FeedbackSettings.webhookUrl)) continue;
                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawWebhookSection(FeedbackSettings settings)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Discord Webhook", EditorStyles.boldLabel);

            var webhook = serializedObject.FindProperty(nameof(FeedbackSettings.webhookUrl));
            using (new EditorGUILayout.HorizontalScope())
            {
                var label = new GUIContent("Webhook URL", webhook.tooltip);
                webhook.stringValue = s_ShowWebhook
                    ? EditorGUILayout.TextField(label, webhook.stringValue)
                    : EditorGUILayout.PasswordField(label, webhook.stringValue);
                s_ShowWebhook = GUILayout.Toggle(s_ShowWebhook, "Show", EditorStyles.miniButton, GUILayout.Width(50));
            }

            string url = webhook.stringValue?.Trim();
            bool valid = FeedbackSettings.IsValidWebhookUrl(url);
            if (string.IsNullOrEmpty(url))
            {
                EditorGUILayout.HelpBox("Paste a webhook URL from Discord: Channel settings > Integrations > Webhooks > New Webhook > Copy Webhook URL.", MessageType.Warning);
            }
            else if (!valid)
            {
                EditorGUILayout.HelpBox("This doesn't look like a Discord webhook URL (https://discord.com/api/webhooks/...).", MessageType.Error);
            }

            EditorGUILayout.HelpBox(
                "Anyone who has a build can extract this URL. Use a separate webhook per playtest round, and delete it in Discord when the round ends or if it leaks.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!valid || _testRunning))
            {
                if (GUILayout.Button(_testRunning ? "Sending…" : "Send Test Message"))
                {
                    serializedObject.ApplyModifiedProperties();
                    SendTestMessage(settings, url);
                }
            }

            if (!string.IsNullOrEmpty(_testStatus)) EditorGUILayout.HelpBox(_testStatus, _testStatusType);
            EditorGUILayout.Space();
        }

        void SendTestMessage(FeedbackSettings settings, string url)
        {
            var request = DiscordWebhook.BuildTest(settings);
            var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(request.Body) { contentType = request.ContentType },
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 20,
            };
            www.SetRequestHeader("Content-Type", request.ContentType);

            _testRunning = true;
            _testStatus = null;

            www.SendWebRequest().completed += _ =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    _testStatus = "Test message sent. Check your Discord channel.";
                    _testStatusType = MessageType.Info;
                }
                else
                {
                    string body = www.downloadHandler?.text;
                    _testStatus = $"Failed ({www.responseCode} {www.error}).\n{DiscordWebhook.Truncate(body ?? "", 400)}";
                    if (body != null && body.Contains("thread_name"))
                    {
                        _testStatus += "\nIf the webhook isn't in a Forum channel, untick 'Post To Forum Channel'.";
                    }
                    _testStatusType = MessageType.Error;
                }

                _testRunning = false;
                www.Dispose();
                if (this != null) Repaint();
            };
        }
    }
}
